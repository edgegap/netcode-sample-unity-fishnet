# FishNet on Edgegap, with an optional Edgegap Matchmaker (WebGL and desktop)

The SpaceEdge FishNet sample on Edgegap dedicated servers. **Start Game** has two modes, selected on the `MatchmakingSystem` object in the `MainMenu` scene:

- **Direct connection (default).** Connects to a server address and port you set. Test locally first, then against a deployment you start from the Edgegap dashboard. No matchmaker is needed.
- **Edgegap Matchmaker (opt-in).** Tick **Use Edgegap Matchmaker**. The [Edgegap Matchmaker](https://docs.edgegap.com/learn/matchmaking) groups players, Edgegap starts a dedicated server near them, and every client connects to it automatically.

In both modes the game client holds no Edgegap API token; matchmaker mode only uses the matchmaker's Auth Token, which is safe to ship in clients. The server stops its own Edgegap deployment once the match is empty.

Tested with Unity `2021.3.45f2`, FishNet `4.1.6R`, the Bayou WebSocket transport and Edgegap Unity SDK `3.5.5` with the POST fix that shipped in `3.5.6` (see [Edgegap SDK version](#edgegap-sdk-version)).

## How it works

| Piece | File | Role |
|---|---|---|
| Menu bridge | `Assets/SpaceEdge/Scripts/Systems/MatchmakingSystem.cs` | Connects **Start Game** to a direct connection (default) or to the matchmaking client. With the matchmaker off, it disables the matchmaking client so the game never contacts a matchmaker. |
| Matchmaking client | `Assets/Edgegap/EdgegapMatchmakingClient.cs` | Matchmaker mode only. Creates a ticket (SDK Group Up flow, a group of one), polls it until `HOST_ASSIGNED`, then connects with `wss://{fqdn}:{external port}`. |
| Server lifecycle | `Assets/Edgegap/EdgegapServerLifecycle.cs` | In server builds, starts the FishNet server on `0.0.0.0:7770`. On Edgegap it reads the injected variables and stops the deployment when no one joins within 120 s or once everyone has left for 30 s. |
| Persistence | `Assets/Edgegap/EdgegapPersistence.cs` | Keeps the `Edgegap` object alive across the MainMenu → BattleScene → MainMenu scene changes. |
| Edgegap SDK | `Packages/manifest.json` | Git package from [edgegap-unity-sdk](https://github.com/edgegap/edgegap-unity-sdk), not a copy, so SDK updates reach the sample (see below). |
| Build helpers | `Assets/Edgegap/Editor/` | Command-line Linux server and client builds, plus scene setup. |

The server no longer relies on FishNet's **Start On Headless**: the lifecycle component starts it, so the same scene works for client and server builds without toggling anything.

### Edgegap SDK version

The SDK comes from its git repository, so the sample follows SDK updates instead of carrying a frozen copy.

The manifest tracks the SDK's `main` branch:

```json
"com.edgegap.unity-sdk": "https://github.com/edgegap/edgegap-unity-sdk.git#main"
```

This sample needs SDK `3.5.6` or later. Before Unity 2022.3, SDK 3.5.5 and earlier send POST bodies through `UnityWebRequest.Post(url, string)`, which form-encodes the JSON, and the matchmaker answers every ticket request with HTTP 400. [edgegap/edgegap-unity-sdk#68](https://github.com/edgegap/edgegap-unity-sdk/pull/68) fixed this by sending the JSON raw (`Runtime/SafeHttpRequest.cs`), and the fix shipped in 3.5.6. If you pin a tag instead of `main`, use `#3.5.6` or newer.

Unity records the resolved commit in `Packages/packages-lock.json` the first time it opens the project. To pull a newer SDK into a project you already opened, delete the `com.edgegap.unity-sdk` entry from `packages-lock.json` (or remove and re-add the package) and let Unity resolve it again.

## 1. Play locally (direct connection)

1. Open this folder with Unity `2021.3.45f2` and install the **Linux Build Support (Mono)**, **Linux Dedicated Server Build Support** and **WebGL Build Support** modules.
2. Build the server with **Tools → Edgegap → Build Linux Dedicated Server**. The output goes to `Builds/EdgegapServer/`.
3. Run it in Docker:
   ```bash
   docker build --platform linux/amd64 -t fishnet-server:local .
   docker run --rm -p 7770:7770 fishnet-server:local
   ```
   The log shows `Edgegap Server | Starting server on 0.0.0.0:7770.` and `Not running on Edgegap, self-stop disabled.`
4. Build the client from **File → Build Settings**, or from the command line:
   ```bash
   Unity -batchmode -quit -projectPath . -executeMethod EdgegapServerBuild.BuildClient -edgegapClientTarget webgl
   ```
   Serve `Builds/Client/webgl` (for example `python -m http.server 8080`), open `http://localhost:8080` in two tabs and click **Start Game**. The defaults (`localhost`, `7770`, **Direct Use Wss** off) connect to the local server. Desktop builds (`-edgegapClientTarget win64`) work too, since Bayou supports native clients.

## 2. Play on an Edgegap server (direct connection)

1. Push the image to your Edgegap registry. Your registry project, username and token are on the dashboard's **Container Registry** page.
   ```bash
   docker login registry.edgegap.com
   docker build --platform linux/amd64 -t registry.edgegap.com/<project>/fishnet-server:v1 .
   docker push registry.edgegap.com/<project>/fishnet-server:v1
   ```
2. In the [dashboard](https://app.edgegap.com), create an application and a version for that image:
   - Port: name `gameport`, port `7770`, protocol **WS**, **TLS Upgrade** on. Edgegap terminates TLS, so browsers connect with `wss://` while the server speaks plain `ws`.
   - Container registry username and token: set them, or deployments fail to pull the image.
3. Deploy the version from the dashboard. On `MatchmakingSystem`, set **Direct Address** to the deployment's host (`<id>.pr.edgegap.net`), **Direct Port** to its external port, and tick **Direct Use Wss**. Rebuild the client and play.

The server stops its deployment 120 s after it starts if nobody joins, and 30 s after the last player leaves.

## 3. Turn on the Edgegap Matchmaker (optional)

1. In `matchmaker-config.json`, set `application.name` / `version` to your app and version, and list every origin that serves the WebGL build in `allowed_cors_origins`.
2. Dashboard → **Matchmaker** → **Create Matchmaker** → upload the file → **Create and Start**.
3. In `MainMenu`, copy the **API URL** and **Auth Token** into the `Edgegap` object's **Edgegap Matchmaking Client** (`Base Url`, `Auth Token`), and tick **Use Edgegap Matchmaker** on `MatchmakingSystem`.
4. Rebuild the client, serve it from an origin listed in `allowed_cors_origins` (e.g. `http://localhost:8080`), open two tabs and click **Start Game** in both. The `casual` profile matches 2 to 4 players. It also has a 30 s expansion down to 1 player, but in our tests (matchmaker 3.3.2, free tier) a lone player was only given a server when the ticket neared its 5-minute expiration, so test with two clients.

### Shipping a new build

Build the server → push a new tag → create a new app version → in the matchmaker, **Stop → Edit** `application.version` **→ Start**.

## Notes

- Free tier: the matchmaker restarts every 3 hours (URL and token stay the same), and only one deployment runs at a time.
- WebGL can't ping latency beacons, so the profile has no `latencies` rule and Edgegap places the server from player IPs.
- Next steps for a real game: check the client's ticket ID on the server (`MM_TICKET_IDS`), backfill, and a production matchmaker tier.

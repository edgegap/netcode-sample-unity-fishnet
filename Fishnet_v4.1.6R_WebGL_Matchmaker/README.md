# FishNet + Edgegap Matchmaker (WebGL and desktop)

The SpaceEdge FishNet sample, wired to the [Edgegap Matchmaker](https://docs.edgegap.com/learn/matchmaking). A player clicks **Start Game**. The matchmaker groups players, Edgegap starts a dedicated server near them, and every client connects to it automatically. When the match is empty, the server stops its own deployment.

The game client holds no Edgegap API token. It only uses the matchmaker's Auth Token, which is safe to ship in clients.

Tested with Unity `2021.3.45f2`, FishNet `4.1.6R`, the Bayou WebSocket transport and Edgegap Unity SDK `3.5.5`.

## How it works

| Piece | File | Role |
|---|---|---|
| Matchmaking client | `Assets/Edgegap/EdgegapMatchmakingClient.cs` | Creates a ticket (SDK Group Up flow, a group of one), polls it until `HOST_ASSIGNED`, then connects with `wss://{fqdn}:{external port}`. |
| Server lifecycle | `Assets/Edgegap/EdgegapServerLifecycle.cs` | In server builds, starts the FishNet server on `0.0.0.0:7770`, reads the matchmaker's injected variables, and self-stops the deployment when no one joins within 120 s or once everyone has left for 30 s. |
| Persistence | `Assets/Edgegap/EdgegapPersistence.cs` | Keeps the `Edgegap` object alive across the MainMenu → BattleScene → MainMenu scene changes. |
| Menu bridge | `Assets/SpaceEdge/Scripts/Systems/MatchmakingSystem.cs` | Connects the main menu's **Start Game** button and status text to the matchmaking client. |
| Edgegap SDK | `Packages/com.edgegap.unity-sdk` | SDK 3.5.5, embedded with one fix (see below). |
| Build helpers | `Assets/Edgegap/Editor/` | Command-line Linux server and client builds, plus scene setup. |

The `Edgegap` GameObject in `MainMenu` holds the components. The server no longer relies on FishNet's **Start On Headless**: the lifecycle component starts it, so the same scene works for client and server builds without toggling anything.

### Embedded SDK fix

Before Unity 2022.3, SDK 3.5.5 sends POST bodies through `UnityWebRequest.Post(url, string)`, which form-encodes the JSON. The matchmaker answers every ticket request with HTTP 400. The embedded copy in `Packages/com.edgegap.unity-sdk` sends the JSON raw (`Runtime/SafeHttpRequest.cs`). On Unity 2022.3 or newer you can use the git package instead: `"com.edgegap.unity-sdk": "https://github.com/edgegap/edgegap-unity-sdk.git#3.5.5"`.

## Setup

### 1. Server build and image

1. Open this folder with Unity `2021.3.45f2` and install the **Linux Build Support (Mono)** and **Linux Dedicated Server Build Support** modules.
2. Build the server with **Tools → Edgegap → Build Linux Dedicated Server**. The output goes to `Builds/EdgegapServer/`.
3. Build and push the image. Your registry project, username and token are on the dashboard's **Container Registry** page.
   ```bash
   docker login registry.edgegap.com
   docker build --platform linux/amd64 -t registry.edgegap.com/<project>/fishnet-webgl-matchmaker:v1 .
   docker push registry.edgegap.com/<project>/fishnet-webgl-matchmaker:v1
   ```

### 2. App version

Create an application and a version for that image in the [dashboard](https://app.edgegap.com):
- Port: name `gameport`, port `7770`, protocol **WS**, **TLS Upgrade** on. Edgegap terminates TLS, so browsers can connect with `wss://` while the server speaks plain `ws`.
- Container registry username and token: set them, or deployments fail to pull the image.

### 3. Matchmaker

1. In `matchmaker-config.json`, set `application.name` / `version` to your app and version, and list every origin that serves the WebGL build in `allowed_cors_origins`.
2. Dashboard → **Matchmaker** → **Create Matchmaker** → upload the file → **Create and Start**.
3. Copy the **API URL** and **Auth Token** into the `Edgegap` object's **Edgegap Matchmaking Client** in `MainMenu`: `Base Url` and `Auth Token`.

The `casual` profile matches 2 to 4 players. After 30 s a lone player gets a server alone, so one person can test.

### 4. Client

Build WebGL from **File → Build Settings**, or from the command line:
```bash
Unity -batchmode -quit -projectPath . -executeMethod EdgegapServerBuild.BuildClient -edgegapClientTarget webgl
```
Serve `Builds/Client/webgl` from an origin listed in `allowed_cors_origins`, for example `python -m http.server 8080` → `http://localhost:8080`. Open two tabs and click **Start Game** in both. Desktop builds (`-edgegapClientTarget win64`) work too, since Bayou supports native clients.

## Shipping a new build

Build the server → push a new tag → create a new app version → in the matchmaker, **Stop → Edit** `application.version` **→ Start**.

## Notes

- Free tier: the matchmaker restarts every 3 hours (URL and token stay the same), and only one deployment runs at a time.
- WebGL can't ping latency beacons, so the profile has no `latencies` rule and Edgegap places the server from player IPs.
- Next steps for a real game: check the client's ticket ID on the server (`MM_TICKET_IDS`), backfill, and a production matchmaker tier.

# Netcode for Game Objects on Arbitrium

This guide will help you create a headless server on Edgegap for a Unity project using [Fishnet](https://github.com/FirstGearGames/FishNet) as its networking solution along with Edgegap's Matchmaker.

This sample was tested using the Managed Matchmaker, using the following JSON configuration:

```json
{
    "auth": {
      "type": "NoAuth"
    },
    "profiles": [
        {
            "profile_id": "GameExample",
            "name": "Configuration Example",
            "app": "APP_NAME",
            "version": "APP_VERSION",
            "game_port": "7770",
            "delay_to_start": 2,
            "refresh": 5,
            "match_player_count": 2,
            "selectors": [
                {
                    "key": "mode",
                    "name": "Game Mode",
                    "default": "casual",
                    "required": true,
                    "inject_env": true,
                    "items": [
                            "casual",
                            "ranked",
                            "private"
                        ]
                }
            ],
            "filters": [
                {
                    "key": "score",
                    "name": "ELO Score",
                    "required": false,
                    "maximum": 2000.0,
                    "minimum": 0.0,
                    "difference": {
                        "negative": 50.0,
                        "positive": 50.0
                    }
                }
            ]
        }
    ]
}
```

## Tutorial

To test the sample project: 

- Create and enable a matchmaker release using the configuration above. Make sure to replace `"APP_NAME"` and `"APP_VERSION"` with your own values;
- Open the `Fishnet_v4.1.6R_EdgegapMatchmaker` folder with the Unity Hub;
- Open the `MatchmakerManager` script under `Assets/SpaceEdge/Scripts/EdgegapMatchmaker`, and update the `MATCHMAKER_URL` variable with your release's URL;
- Open the `MainMenu` scene under `Assets/SpaceEdge/Scenes`;
- Enable the `Start On Headless` option in the `NetworkManager` gameObject;
- Make sure that both `Assets/SpaceEdge/Scenes/MainMenu` and `Assets/SpaceEdge/Scenes/BattleScene` are included in the build settings;
- Create an app version using the Edgegap Plugin in the `Tools/Edgegap Hosting` menu tab. Use the same application name and version as in the Matchmaker configuration, with the port value as `7770` and protocol as `UDP`.
- Disable the `Start On Headless` option, then create a client build of the game;
- In your app version on the Edgegap dashboard, rename the port from `Game Port` to `7770`.
- Lauch 2 instances of the build, select the same mode in both and enter an ELO score value in each instance's input field so as to respect the filter condition (ex: 200 and 240). Click on `Create Ticket` and wait a few moments for the game to begin.

## Troubleshooting

If your clients get a `ConnectionFailed` debug message and/or your deployment has an abnormally high CPU usage (e.g. close to 100%), make sure that:
- your server build was made with the `Start On Headless` option enabled;
- you renamed your app version's port to `7770` on the Edgegap dashboard.
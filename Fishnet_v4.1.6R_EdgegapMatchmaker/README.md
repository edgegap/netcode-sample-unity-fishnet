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
            "app": "Edgegap-App-ExplainerVideo",
            "version": "demo-app-2a4ed4",
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
                        "negative": 20.0,
                        "positive": 40.0
                    }
                }
            ]
        }
    ]
}
```

## Tutorial

To test the sample project: 

- Create a matchmaker release using the configuration above;
- Open the `Fishnet_v4.1.6R_EdgegapMatchmaker` folder with the Unity Hub;
- Open the `MatchmakerManager` script under `Assets/SpaceEdge/Scripts/EdgegapMatchmaker`, and update the `MATCHMAKER_URL` variable with your release's URL;
- Open the `MainMenu` scene under `Assets/SpaceEdge/Scenes`;
- Enable the `Start On Headless` option in the `NetworkManager` gameObject, and make sure that both `Assets/SpaceEdge/Scenes/MainMenu` and `Assets/SpaceEdge/Scenes/BattleScene` are included in the build settings;
- Create an app version using the Edgegap Plugin in the `Edgegap` menu toolbar tab. Use the same application name and version as in the Matchmaker configuration.
- Disable the `Start On Headless` option, then create a client build of the game;
- Lauch 2 instances of the build, select the same mode in both and enter an ELO score value in each instance's input field so as to respect the filter condition (ex: 200 and 240). Click on `Create Ticket` and wait a few moments for the game to begin.
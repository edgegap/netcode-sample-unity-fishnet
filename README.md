# FishNet on Edgegap

This guide will help you create a headless server on Edgegap for a Unity project using [Fishnet](https://github.com/FirstGearGames/FishNet) as its networking solution.

The core of this sample is the “MatchmakingSystem” script. This script communicates with the Edgegap API to find live deployments of the available server for the game and if no available servers are found then it requests the Edgegap API to deploy a new instance of the game’s server for the client based on their location. This script explains the core concepts of communicating with the Edgegap API using the Unity HTTP Requests system and utilizing the data sent by the Edgegap API for making decisions for the game’s matchmaking.
This game auto connects to the first instance found on the list of all the live server deployments for the game, this is to make testing easier and due to the fact that Edgegap free tier allows only 1 live server deployment for the game at a given time. But if a user is not using free tier then this matchmaking system can be really easily extended to give players an option to choose from all the available servers to connect to.

Note: This project uses a few free assets from the Unity Asset Store. These assets can be found at the end of the documentation page. This Project is tested on Unity Version 2021 LTS.

## Tutorial

You can see the [full documentation here](https://docs.edgegap.com/docs/sample-projects/fishnet-on-edgegap)

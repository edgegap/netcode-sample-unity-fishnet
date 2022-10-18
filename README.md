# Netcode for Game Objects on Arbitrium

This guide will help you create a headless server on Edgegap for a Unity project using [Fishnet](https://github.com/FirstGearGames/FishNet) as its networking solution.

The core of this sample is the “MatchmakingSystem” script. This script communicates with the EdgeGap API to find live deployments of the available server for the game and if no available servers are found then it requests the EdgeGap API to deploy a new instance of the game’s server for the client based on their location. This script explains the core concepts of communicating with the EdgeGap API using the Unity HTTP Requests system and utilizing the data sent by the EdgeGap API for making decisions for the game’s matchmaking.
This game auto connects to the first instance found on the list of all the live server deployments for the game, this is to make testing easier and due to the fact that EdgeGap free tier allows only 1 live server deployment for the game at a given time. But if a user is not using free tier then this matchmaking system can be really easily extended to give players an option to choose from all the available servers to connect to.

Note: This project uses a few free assets from the Unity Asset Store. At the end of this document there are links to all these assets. 
This Project is tested on Unity Version 2021 LTS.

## Tutorial

You can see the [full documentation here](https://docs.edgegap.com/)
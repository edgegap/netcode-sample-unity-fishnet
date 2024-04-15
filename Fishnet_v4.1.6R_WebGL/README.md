# Netcode for Game Objects on Arbitrium

This guide will help you use [Fishnet](https://github.com/FirstGearGames/FishNet)'s Websocket Transport, [Bayou](https://fish-networking.gitbook.io/docs/manual/components/transports/bayou), and create a headless server on Edgegap for a Unity project. This guide should work with any version of Fishnet, so long as you use the appropriate version of Bayou for it.

The gist of this project is to have the Bayou transport replace the Tugboat transport on the `NetworkManager` gameObject. The server build needs the `Start On Headless` option enabled, while the client build needs the `Client Use Wss` option enabled.

This project is tested on Unity version `2021.3.16f1` and Fishnet version `4.1.6R`.

## Tutorial

You can see the [full documentation here](https://docs.edgegap.com/docs/sample-projects/fishnet-webgl)

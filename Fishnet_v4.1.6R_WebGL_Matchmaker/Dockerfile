FROM ubuntu:22.04

ARG DEBIAN_FRONTEND=noninteractive
ARG SERVER_BUILD_PATH=Builds/EdgegapServer

# ca-certificates: required for HTTPS calls from the server (self-stop, matchmaker backfill).
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    apt-get clean && rm -rf /var/lib/apt/lists/* && \
    update-ca-certificates

# Linux Dedicated Server build folder (ServerBuild, ServerBuild_Data, UnityPlayer.so).
COPY ${SERVER_BUILD_PATH} /root/build/
WORKDIR /root/
RUN chmod +x /root/build/ServerBuild

# Documentation only: Edgegap exposes the ports declared on the app version.
EXPOSE 7770/tcp

# Shell form so $UNITY_COMMANDLINE_ARGS (set as an app version / deployment env var) expands.
# Unlike the plugin's Dockerfile this does not run `env;` first, which would print the
# hidden self-stop token into the container logs.
CMD ["/bin/bash", "-c", "/root/build/ServerBuild -batchmode -nographics $UNITY_COMMANDLINE_ARGS"]

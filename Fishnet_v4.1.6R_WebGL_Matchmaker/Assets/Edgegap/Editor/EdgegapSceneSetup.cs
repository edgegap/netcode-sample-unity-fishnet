// Adds (or updates) an "Edgegap" GameObject with EdgegapServerLifecycle + EdgegapMatchmakingClient
// in a scene, so agents don't have to hand-edit scene YAML. Place under any Assets/**/Editor/ folder.
// Usage (project closed in the Editor):
//   <Unity> -batchmode -quit -projectPath <project> -executeMethod EdgegapSceneSetup.Setup
//     -edgegapScene Assets/Scenes/Game.unity   (default: first enabled scene in Build Settings)
//     -edgegapListenPort 7777                  -edgegapAutoStart true|false
//     -edgegapProfile casual                   -edgegapPortName gameport
//     -edgegapBaseUrl <matchmaker API URL>     -edgegapAuthToken <matchmaker auth token, client-safe>
//     -edgegapBeacons false                    -edgegapSecureWebSocket true
// Omitted options keep the component's current value.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EdgegapSceneSetup
{
    [MenuItem("Tools/Edgegap/Add Edgegap Components To Open Scene")]
    public static void AddToOpenScene()
    {
        Apply(EditorSceneManager.GetActiveScene().path);
    }

    public static void Setup()
    {
        string scenePath = Arg("-edgegapScene")
            ?? EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path;
        if (string.IsNullOrEmpty(scenePath))
        {
            Fail("No scene given (-edgegapScene) and no enabled scene in Build Settings.");
            return;
        }
        Apply(scenePath);
    }

    private static void Apply(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Edgegap") ?? new GameObject("Edgegap");
        var server = go.GetComponent<EdgegapServerLifecycle>() ?? go.AddComponent<EdgegapServerLifecycle>();
        var client = go.GetComponent<EdgegapMatchmakingClient>() ?? go.AddComponent<EdgegapMatchmakingClient>();
        if (go.GetComponent<EdgegapPersistence>() == null) go.AddComponent<EdgegapPersistence>();

        if (ushort.TryParse(Arg("-edgegapListenPort"), out ushort port)) server.ListenPort = port;
        if (bool.TryParse(Arg("-edgegapAutoStart"), out bool autoStart)) server.AutoStartServer = autoStart;
        if (Arg("-edgegapProfile") is string profile) client.Profile = profile;
        if (Arg("-edgegapPortName") is string portName) client.PortName = portName;
        if (Arg("-edgegapBaseUrl") is string baseUrl) client.BaseUrl = baseUrl;
        if (Arg("-edgegapAuthToken") is string token) client.AuthToken = token;
        if (bool.TryParse(Arg("-edgegapBeacons"), out bool beacons)) client.UseLatencyBeacons = beacons;
        if (bool.TryParse(Arg("-edgegapSecureWebSocket"), out bool wss)) client.UseSecureWebSocket = wss;

        EditorUtility.SetDirty(server);
        EditorUtility.SetDirty(client);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            Fail($"Couldn't save {scenePath}.");
            return;
        }

        Debug.Log($"EdgegapSceneSetup | {scenePath}: ListenPort={server.ListenPort} AutoStartServer={server.AutoStartServer} "
            + $"Profile={client.Profile} PortName={client.PortName} BaseUrl={(string.IsNullOrEmpty(client.BaseUrl) ? "(empty)" : client.BaseUrl)} "
            + $"AuthToken={(string.IsNullOrEmpty(client.AuthToken) ? "(empty)" : "(set)")} Beacons={client.UseLatencyBeacons}");
    }

    private static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static void Fail(string message)
    {
        Debug.LogError($"EdgegapSceneSetup | {message}");
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}

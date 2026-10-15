// Command-line Linux dedicated server build for Edgegap. Place under any Assets/**/Editor/ folder.
// Usage (the project must NOT be open in another Editor instance):
//   <Unity> -batchmode -quit -projectPath <project> -executeMethod EdgegapServerBuild.Build -logFile -
// Output: Builds/EdgegapServer/ServerBuild (+ ServerBuild_Data, UnityPlayer.so), matching the Dockerfile.
// Requires Unity Hub modules: Linux Build Support (Mono or IL2CPP) + Linux Dedicated Server Build Support.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EdgegapServerBuild
{
    public const string OutputPath = "Builds/EdgegapServer/ServerBuild";

    [MenuItem("Tools/Edgegap/Build Linux Dedicated Server")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
        {
            Fail("Linux Build Support is not installed. Unity Hub > Installs > this version > Add Modules > "
                + "Linux Build Support + Linux Dedicated Server Build Support, then restart Unity.");
            return;
        }

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Fail("No enabled scenes in Build Settings. Add the game scene(s) (including the one with the network manager).");
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server, // Dedicated Server: defines UNITY_SERVER, strips rendering
            locationPathName = OutputPath,
            options = BuildOptions.None,
        };

        Debug.Log($"EdgegapServerBuild | Building {scenes.Length} scene(s): {string.Join(", ", scenes)}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Fail($"Build {report.summary.result} with {report.summary.totalErrors} error(s). See the log above.");
            return;
        }

        Debug.Log($"EdgegapServerBuild | Succeeded: {OutputPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
    }

    /// <summary>
    /// Client build for end-to-end tests: -executeMethod EdgegapServerBuild.BuildClient -edgegapClientTarget webgl|win64|mac|linux
    /// Output: Builds/Client/&lt;target&gt;/. Switches the active build target (slow the first time).
    /// </summary>
    public static void BuildClient()
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-edgegapClientTarget");
        string name = i >= 0 && i + 1 < args.Length ? args[i + 1].ToLowerInvariant() : "webgl";

        (BuildTarget target, string path) = name switch
        {
            "win64" => (BuildTarget.StandaloneWindows64, "Builds/Client/win64/Game.exe"),
            "mac" => (BuildTarget.StandaloneOSX, "Builds/Client/mac/Game.app"),
            "linux" => (BuildTarget.StandaloneLinux64, "Builds/Client/linux/Game.x86_64"),
            _ => (BuildTarget.WebGL, "Builds/Client/webgl"),
        };

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Fail("No enabled scenes in Build Settings.");
            return;
        }

        if (target != BuildTarget.WebGL)
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            target = target,
            subtarget = target == BuildTarget.WebGL ? 0 : (int)StandaloneBuildSubtarget.Player,
            locationPathName = path,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            Fail($"Client build {report.summary.result} with {report.summary.totalErrors} error(s).");
            return;
        }
        Debug.Log($"EdgegapServerBuild | Client build succeeded: {path}");
    }

    private static void Fail(string message)
    {
        Debug.LogError($"EdgegapServerBuild | {message}");
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}

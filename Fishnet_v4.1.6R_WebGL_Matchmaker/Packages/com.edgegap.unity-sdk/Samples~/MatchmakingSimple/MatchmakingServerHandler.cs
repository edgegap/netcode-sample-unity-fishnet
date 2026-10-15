using System;
using System.Collections;
using Edgegap;
using Edgegap.Matchmaking;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using L = Edgegap.Logger;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;

public class MatchmakingServerHandler : MonoBehaviour
{
    private bool MockEnv = false;
    public DeploymentEnvironmentDTO DeploymentEnv { get; private set; }
    public MatchEnvironmentDTO<MyTicketsAttributes> MatchEnv;

    public static MatchmakingServerHandler Instance { get; private set; }
    private SafeHttpRequest Request;

    public void Awake()
    {
        // if there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        Request = new SafeHttpRequest(this);
        IDictionary env = Environment.GetEnvironmentVariables();

#if UNITY_EDITOR
        MockEnv = true;
#endif

        MockEnv = MockEnv || !string.IsNullOrEmpty(env["ARBITRIUM_MOCK_ENV"]?.ToString());

        if (!MockEnv && !Application.isBatchMode)
        {
            L.Log("MM ServerHandler | Destroying self in client environment.");
            Destroy(this.gameObject);
        }

        #region mock data
        if (MockEnv)
        {
            // define mock env variables here
            env["ARBITRIUM_REQUEST_ID"] = "Editor";
            env["ARBITRIUM_PUBLIC_IP"] = "172.236.117.196";
            env["ARBITRIUM_DEPLOYMENT_TAGS"] = "tag1,tag2";
            env["ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY"] = "2000";
            env["ARBITRIUM_DEPLOYMENT_VCPU_UNITS"] = "1536";
            env["ARBITRIUM_DEPLOYMENT_MEMORY_MB"] = "3072";
            env["ARBITRIUM_DEPLOYMENT_LOCATION"] =
                "{\"city\":\"Chicago\",\"country\":\"United States of America\",\"continent\":\"North America\",\"administrative_division\":\"Illinois\",\"timezone\":\"Central Time\"}";
            env["ARBITRIUM_PORTS_MAPPING"] =
                "{\"ports\":{\"gameport\":{\"name\":\"GamePort\",\"internal\":7777,\"external\":31504,\"protocol\":\"UDP\"}}}";

            env["MM_MATCH_PROFILE"] = "advanced-example";
            env["MM_TICKET_IDS"] = "[\"cusfn10msflc73beiik0\",\"cusfn18msflc73beiil0\"]";
            env["MM_TICKET_cusfn10msflc73beiik0"] =
                "{\"id\":\"cusfn10msflc73beiik0\",\"created_at\":\"2025-02-21T22:17:42.388697Z\",\"player_ip\":\"174.93.233.25\",\"group_id\":\"b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1\",\"team_id\":\"cusfn1gmsflc73beiim0\",\"attributes\":{\"beacons\":{\"Chicago\":12.3,\"Los Angeles\":145.6,\"Tokyo\":233.2}}}";
            env["MM_TICKET_cusfn18msflc73beiil0"] =
                "{\"id\":\"cusfn18msflc73beiil0\",\"created_at\":\"2025-02-21T22:17:42.254839Z\",\"player_ip\":\"174.93.233.23\",\"group_id\":\"015d4dc8-6c79-4b5c-bbc6-f309b9787c8f\",\"team_id\":\"cusfn1gmsflc73beiim0\",\"attributes\":{\"beacons\":{\"Chicago\":87.3,\"LosAngeles\":32.4,\"Tokyo\":253.2}}}";
            env["MM_GROUPS"] =
                "{\"b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1\":[\"cusfn10msflc73beiik0\"],\"015d4dc8-6c79-4b5c-bbc6-f309b9787c8f\":[\"cusfn18msflc73beiil0\"]}";
            env["MM_TEAMS"] =
                "{\"cusfn1gmsflc73beiim0\":[\"b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1\",\"015d4dc8-6c79-4b5c-bbc6-f309b9787c8f\"]}";
            env["MM_MATCH_ID"] = "advanced-example_initial-2025-02-21T22:17:43.3886970Z";
            env["MM_EQUALITY"] = "{\"selected_game_mode\":\"quickplay\"}";
            env["MM_INTERSECTION"] =
                "{\"selected_map\":[\"Airport\"],\"backfill_group_size\":[\"new\",\"1\"]}";
        }
        #endregion

        DeploymentEnv = new DeploymentEnvironmentDTO(env);
        MatchEnv = new MatchEnvironmentDTO<MyTicketsAttributes>(env);

        L.Log(
            $"MM ServerHandler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
        );
    }

    public void SelfStopDeployment()
    {
        if (MockEnv)
        {
            L.Log("MM ServerHandler | Invoking Application.Quit() in mock environment.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        if (
            string.IsNullOrEmpty(DeploymentEnv.SelfStopURL)
            || string.IsNullOrEmpty(DeploymentEnv.SelfStopToken)
        )
        {
            L.Error("MM ServerHandler | Self-Stop URL or Token not set, unable to self-stop.");
            return;
        }

        Request.Delete(
            DeploymentEnv.SelfStopURL,
            DeploymentEnv.SelfStopToken,
            (string response, UnityWebRequest request) =>
            {
                L.Log($"MM ServerHandler | Successfully called Self-Stop API.\n{response}");
            },
            (string error, UnityWebRequest request) =>
            {
                L.Error($"MM ServerHandler | Couldn't reach Self-Stop API.\n{error}");
            },
            new RetryParameters { MaxAttempts = 10 }
        );
    }
}

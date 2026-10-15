using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class GroupClient<G, A>
        where G : GroupUpRequestDTO<A>
    {
        private Api MatchmakingApi;
        private Edgegap.Ping Ping;

        public MonoBehaviour Handler;

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public int RequestTimeoutSeconds;
        public float PollingBackoffSeconds;
        public int MaxConsecutivePollingErrors;
        public float RemoveGroupSeconds;

        public bool LogGroupUpdates;
        public bool LogPollingUpdates;

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO>() { };
        public Observable<GroupUpResponseDTO> Group { get; private set; } =
            new Observable<GroupUpResponseDTO>() { };
        public bool Owner { get; private set; } = false;
        private protected bool Polling = false;

        public GroupClient(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            int requestTimeoutSeconds = 3,
            float pollingBackoffSeconds = 1f,
            int maxConsecutivePollingErrors = 10,
            float removeGroupSeconds = 30f,
            bool logGroupUpdates = true,
            bool logPollingUpdates = false
        )
        {
            if (handler == null)
            {
                throw new Exception("MatchmakingClient Handler not assigned.");
            }

            Handler = handler;

            BaseUrl = baseUrl;
            AuthToken = authToken;

            RequestTimeoutSeconds = requestTimeoutSeconds;
            PollingBackoffSeconds = pollingBackoffSeconds;
            MaxConsecutivePollingErrors = maxConsecutivePollingErrors;

            RemoveGroupSeconds = removeGroupSeconds;

            LogGroupUpdates = logGroupUpdates;
            LogPollingUpdates = logPollingUpdates;
        }

        #region Client API

        public void Status()
        {
            MatchmakingApi.GetMonitor(
                (MonitorResponseDTO monitor, UnityWebRequest request) =>
                {
                    if (monitor.Status.ToLower() == "healthy")
                    {
                        Monitor._Update(monitor, "healthy");
                    }
                    else
                    {
                        Monitor._Update(monitor, "unhealthy");
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    Monitor._Error($"get monitor failed (unexpected error)\n{error}", null);
                }
            );
        }

        public void Beacons(
            Action<BeaconsResponseDTO> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            MatchmakingApi.GetBeacons(
                (BeaconsResponseDTO beacons, UnityWebRequest request) =>
                {
                    onSuccessDelegate(beacons);
                },
                (string error, UnityWebRequest request) =>
                {
                    Monitor._Error($"get beacons failed\n{error}");
                    onErrorDelegate(error, request);
                }
            );
        }

        public void MeasureBeaconsRoundTripTime(
            BeaconDTO[] beacons,
            Action<Dictionary<string, float>> onCompleteDelegate,
            int requests = 3
        )
        {
            Handler.StartCoroutine(GetLatencies(beacons, onCompleteDelegate, requests));
        }

        public void ResumeMatchmaking(GroupUpResponseDTO group, bool abandon = false)
        {
            if (Group.Current is not null && !abandon)
            {
                Group._Error("conflict, abandon and restart");
                return;
            }

            StopMatchmaking(() =>
            {
                Group._Update(group, "resumed");
                Polling = true;
                StartPollingGroup();
            });
        }

        public void CreateGroup(G group, bool abandon = false)
        {
            if (Group.Current is not null && !abandon)
            {
                Group._Error("conflict, abandon and restart");
                return;
            }

            StopMatchmaking(() =>
            {
                MatchmakingApi.CreateGroup<G, A>(
                    group,
                    (GroupUpResponseDTO group, UnityWebRequest request) =>
                    {
                        Group._Update(group, "created");
                        Owner = true;
                        Polling = true;
                        Handler.StartCoroutine(DelayPollingGroup());
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        Group._Error($"group create failed\n{error}");
                    }
                );
            });
        }

        public void JoinGroup(G member, string groupId, bool abandon = false)
        {
            if (Group.Current is not null && !abandon)
            {
                Group._Error("conflict, abandon and restart");
                return;
            }

            StopMatchmaking(() =>
            {
                MatchmakingApi.CreateGroupMember<G, A>(
                    member,
                    groupId,
                    (GroupUpResponseDTO group, UnityWebRequest request) =>
                    {
                        Group._Update(group, "joined");
                        Owner = false;
                        Polling = true;
                        Handler.StartCoroutine(DelayPollingGroup());
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        if (request.responseCode == 404)
                        {
                            Group._Error($"group not found\n{error}");
                        }
                        else
                        {
                            Group._Error($"group join failed\n{error}");
                        }
                    }
                );
            });
        }

        public void SetReady(bool isReady)
        {
            if (Group.Current is null)
            {
                Group._Error("group not found");
                return;
            }

            MatchmakingApi.UpdateGroupMember(
                Group.Current.GroupID,
                Group.Current.MemberID,
                isReady,
                (GroupUpResponseDTO group, UnityWebRequest request) =>
                {
                    Group._Update(group, $"member updated [{isReady}]");
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        Group._Error($"group not found\n{error}");
                    }
                    else if (request.responseCode == 409)
                    {
                        Group._Notify("member update failed", ObservableActionType.Warn);
                    }
                    else
                    {
                        Group._Error($"group update failed\n{error}");
                    }
                }
            );
        }

        public void StopMatchmaking(Action onCompletedDelegate = null)
        {
            Polling = false;

            if (Group.Current?.GroupID is null)
            {
                if (onCompletedDelegate is not null)
                {
                    onCompletedDelegate();
                }
                return;
            }

            if (Owner)
            {
                MatchmakingApi.DeleteGroup(
                    Group.Current.GroupID,
                    (UnityWebRequest request) =>
                    {
                        Group._Update(null, "abandoned");

                        if (onCompletedDelegate is not null)
                        {
                            onCompletedDelegate();
                        }
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        OnStopMatchmakingError(error, request, onCompletedDelegate);
                    }
                );
            }
            else
            {
                MatchmakingApi.DeleteGroupMember(
                    Group.Current.GroupID,
                    Group.Current.MemberID,
                    (UnityWebRequest request) =>
                    {
                        Group._Update(null, "abandoned");

                        if (onCompletedDelegate is not null)
                        {
                            onCompletedDelegate();
                        }
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        OnStopMatchmakingError(error, request, onCompletedDelegate);
                    }
                );
            }
        }
        #endregion

        #region Initialization
        public void Initialize(
            UnityAction<
                Observable<MonitorResponseDTO>,
                ObservableActionType,
                string
            > onMonitorUpdate,
            UnityAction<
                Observable<GroupUpResponseDTO>,
                ObservableActionType,
                string
            > onGroupUpUpdate
        )
        {
            if (string.IsNullOrEmpty(BaseUrl.Trim()))
            {
                throw new Exception("BaseUrl not declared.");
            }

            if (string.IsNullOrEmpty(AuthToken.Trim()))
            {
                throw new Exception("AuthToken not declared.");
            }

            MatchmakingApi = new Api(Handler, AuthToken, BaseUrl);
            Ping = new Edgegap.Ping(Handler);

            L.SubscribeLogger(Monitor, "MM", "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            L.SubscribeLogger(Group, "MM", "Group", LogGroupUpdates);
            Group.Subscribe(onGroupUpUpdate);

            Status();
        }
        #endregion

        #region Internals
        internal void StartPollingGroup(int consecutiveErrors = 0)
        {
            if (!Polling)
            {
                if (LogPollingUpdates)
                {
                    Group._Notify("polling stopped");
                }
                return;
            }

            if (LogPollingUpdates)
            {
                Group._Notify($"polling [{consecutiveErrors + 1}/{MaxConsecutivePollingErrors}]");
            }

            MatchmakingApi.GetGroupMember(
                Group.Current.GroupID,
                Group.Current.MemberID,
                (GroupUpResponseDTO group, UnityWebRequest request) =>
                {
                    if (Group.Current is not null && group.Status != Group.Current.Status)
                    {
                        Group._Update(group, $"group updated [{group.Status}]");

                        if (
                            Group.Current.Status == "HOST_ASSIGNED"
                            || Group.Current.Status == "CANCELLED"
                        )
                        {
                            Handler.StartCoroutine(ExpireGroup());
                        }
                        else
                        {
                            Handler.StartCoroutine(DelayPollingGroup());
                        }
                    }
                    else
                    {
                        Handler.StartCoroutine(DelayPollingGroup());
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    if (consecutiveErrors + 1 > MaxConsecutivePollingErrors)
                    {
                        Group._Error($"polling failed, reached maximum retries\n{error}");
                        StopMatchmaking();
                    }
                    else
                    {
                        if (request.responseCode == 429 || request.responseCode >= 500)
                        {
                            Handler.StartCoroutine(DelayPollingGroup(consecutiveErrors + 1));
                        }
                        else
                        {
                            Group._Error($"polling failed\n{error}");
                            StopMatchmaking();
                        }
                    }
                }
            );
        }

        internal IEnumerator DelayPollingGroup(int consecutiveErrors = 0)
        {
            yield return new WaitForSeconds(PollingBackoffSeconds + (0.1f * Random.value));
            StartPollingGroup(consecutiveErrors);
        }

        internal IEnumerator ExpireGroup()
        {
            Polling = false;
            yield return new WaitForSeconds(RemoveGroupSeconds);
            Group._Update(null, "removed");
        }

        internal IEnumerator GetLatencies(
            BeaconDTO[] beacons,
            Action<Dictionary<string, float>> onCompleteDelegate,
            int requests
        )
        {
            Dictionary<string, float> results = new Dictionary<string, float>();
            foreach (BeaconDTO beacon in beacons)
            {
                Handler.StartCoroutine(
                    Ping.GetAverageRoundTripTime(
                        beacon.PublicIP,
                        (double ping) => results.Add(beacon.Location.City, (float)ping),
                        requests
                    )
                );
            }

            yield return new WaitUntil(() => results.Keys.Count == beacons.Count());
            onCompleteDelegate(results);
        }

        internal void OnStopMatchmakingError(
            string error,
            UnityWebRequest request,
            Action onCompletedDelegate = null
        )
        {
            if (request.responseCode == 409)
            {
                Group._Notify("abandon failed (match found)", ObservableActionType.Warn);
            }
            else if (request.responseCode == 404)
            {
                Group._Update(null, "abandon failed (not found)");
            }
            else
            {
                Group._Error($"abandon failed\n{error}", null);
            }

            if (onCompletedDelegate is not null)
            {
                onCompletedDelegate();
            }
        }
        #endregion
    }
}

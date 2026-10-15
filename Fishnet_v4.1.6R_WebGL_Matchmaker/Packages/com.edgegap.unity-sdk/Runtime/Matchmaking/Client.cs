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

    public class Client<T, A>
        where T : TicketsRequestDTO<A>, new()
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
        public float RemoveAssignmentSeconds;

        public bool LogAssignmentUpdates;
        public bool LogPollingUpdates;

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO>() { };

        [Obsolete("Managing tickets in clients is deprecated, see the Group Up flow instead")]
        public Observable<TicketResponseDTO> Assignment { get; private set; } =
            new Observable<TicketResponseDTO>() { };
        private protected bool Polling = false;

        public Client(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            int requestTimeoutSeconds = 3,
            float pollingBackoffSeconds = 1f,
            int maxConsecutivePollingErrors = 10,
            float removeAssignmentSeconds = 30f,
            bool logAssignmentUpdates = true,
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

            RemoveAssignmentSeconds = removeAssignmentSeconds;

            LogAssignmentUpdates = logAssignmentUpdates;
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

        [Obsolete("Managing tickets in clients is deprecated, see the Group Up flow instead")]
        public void StartMatchmaking(T ticket, bool abandon = false)
        {
            if (Assignment.Current is not null && !abandon)
            {
                Assignment._Error("conflict, abandon and restart");
                return;
            }

            StopMatchmaking(() =>
            {
                MatchmakingApi.CreateTicketAsync<T, A>(
                    ticket,
                    (TicketResponseDTO assignment, UnityWebRequest request) =>
                    {
                        Assignment._Update(assignment, "received");
                        Polling = true;
                        Handler.StartCoroutine(DelayPollingAssignment());
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        Assignment._Error($"ticket create failed\n{error}");
                        StopMatchmaking();
                    }
                );
            });
        }

        [Obsolete("Managing tickets in clients is deprecated, see the Group Up flow instead")]
        public void ResumeMatchmaking(TicketResponseDTO assignment, bool abandon = false)
        {
            if (Assignment.Current is not null && !abandon)
            {
                Assignment._Error("conflict, abandon and restart");
                return;
            }

            StopMatchmaking(() =>
            {
                Assignment._Update(assignment, "resumed");
                Polling = true;
                StartPollingAssignment();
            });
        }

        [Obsolete(
            "Managing group tickets in clients is deprecated, please use Group Up flow instead."
        )]
        public void StartGroupMatchmaking(
            T hostTicket,
            List<T> memberTickets,
            Action<List<TicketResponseDTO>, UnityWebRequest> onSuccessDelegate,
            bool abandon = false
        )
        {
            if (Assignment.Current is not null && !abandon)
            {
                Assignment._Error("conflict, abandon and restart");
                return;
            }

            GroupTicketsRequestDTO<A> groupTicket = new GroupTicketsRequestDTO<A>(
                memberTickets.Append(hostTicket).ToArray()
            );

            StopMatchmaking(() =>
            {
                MatchmakingApi.CreateGroupTicketAsync<GroupTicketsRequestDTO<A>, A>(
                    groupTicket,
                    (GroupTicketsResponseDTO assignment, UnityWebRequest request) =>
                    {
                        Assignment._Update(assignment.Tickets.Last(), "received");
                        onSuccessDelegate(assignment.Tickets.SkipLast(1).ToList(), request);
                        Polling = true;
                        Handler.StartCoroutine(DelayPollingAssignment());
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        Assignment._Error($"ticket create failed\n{error}");
                        StopMatchmaking();
                    }
                );
            });
        }

        [Obsolete(
            "Managing group tickets in clients is deprecated, please use Group Up flow instead."
        )]
        public void JoinGroupMatchmaking(TicketResponseDTO assignment, bool abandon = false)
        {
            ResumeMatchmaking(assignment, abandon);
        }

        [Obsolete("Managing tickets in clients is deprecated, see the Group Up flow instead")]
        public void StopMatchmaking(Action onCompletedDelegate = null)
        {
            Polling = false;

            if (Assignment.Current is null)
            {
                if (onCompletedDelegate is not null)
                {
                    onCompletedDelegate();
                }
                return;
            }

            MatchmakingApi.DeleteTicketAsync(
                Assignment.Current.ID,
                (UnityWebRequest request) =>
                {
                    Assignment._Update(null, "abandoned");
                    if (onCompletedDelegate is not null)
                    {
                        onCompletedDelegate();
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 409)
                    {
                        Assignment._Notify(
                            "abandon failed (already matched)",
                            ObservableActionType.Warn
                        );
                    }
                    else if (request.responseCode == 404)
                    {
                        Assignment._Update(null, "abandon failed (not found)");
                    }
                    else
                    {
                        Assignment._Error($"abandon failed\n{error}", null);
                    }

                    if (onCompletedDelegate is not null)
                    {
                        onCompletedDelegate();
                    }
                }
            );
        }
        #endregion

        #region Initialization
        [Obsolete("Managing tickets in clients is deprecated, see the Group Up flow instead")]
        public void Initialize(
            UnityAction<
                Observable<MonitorResponseDTO>,
                ObservableActionType,
                string
            > onMonitorUpdate,
            UnityAction<
                Observable<TicketResponseDTO>,
                ObservableActionType,
                string
            > onAssignmentUpdate,
            UnityAction<Observable<T>, ObservableActionType, string> onTicketUpdate = null
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

            L.SubscribeLogger(Assignment, "MM", "Assignment", LogAssignmentUpdates);
            Assignment.Subscribe(onAssignmentUpdate);

            Status();
        }
        #endregion

        #region Internals
        internal void StartPollingAssignment(int consecutiveErrors = 0)
        {
            if (!Polling)
            {
                if (LogPollingUpdates)
                {
                    Assignment._Notify("polling stopped");
                }
                return;
            }

            if (LogPollingUpdates)
            {
                Assignment._Notify(
                    $"polling [{consecutiveErrors + 1}/{MaxConsecutivePollingErrors}]"
                );
            }

            MatchmakingApi.GetTicketAsync(
                Assignment.Current.ID,
                (TicketResponseDTO assignment, UnityWebRequest request) =>
                {
                    if (
                        Assignment.Current is not null
                        && assignment.Status != Assignment.Current.Status
                    )
                    {
                        Assignment._Update(assignment, $"updated [{assignment.Status}]");
                        if (
                            Assignment.Current.Status == "HOST_ASSIGNED"
                            || Assignment.Current.Status == "CANCELLED"
                        )
                        {
                            Handler.StartCoroutine(ExpireAssignment());
                        }
                        else
                        {
                            Handler.StartCoroutine(DelayPollingAssignment());
                        }
                    }
                    else
                    {
                        Handler.StartCoroutine(DelayPollingAssignment());
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    if (consecutiveErrors + 1 > MaxConsecutivePollingErrors)
                    {
                        Assignment._Error($"polling failed, reached maximum retries\n{error}");
                        StopMatchmaking();
                    }
                    else
                    {
                        if (request.responseCode == 429 || request.responseCode >= 500)
                        {
                            Handler.StartCoroutine(DelayPollingAssignment(consecutiveErrors + 1));
                        }
                        else
                        {
                            Assignment._Error($"polling failed\n{error}");
                            StopMatchmaking();
                        }
                    }
                }
            );
        }

        internal IEnumerator DelayPollingAssignment(int consecutiveErrors = 0)
        {
            yield return new WaitForSeconds(PollingBackoffSeconds + (0.1f * Random.value));
            StartPollingAssignment(consecutiveErrors);
        }

        internal IEnumerator ExpireAssignment()
        {
            Polling = false;
            yield return new WaitForSeconds(RemoveAssignmentSeconds);
            Assignment._Update(null, "removed");
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
        #endregion
    }
}

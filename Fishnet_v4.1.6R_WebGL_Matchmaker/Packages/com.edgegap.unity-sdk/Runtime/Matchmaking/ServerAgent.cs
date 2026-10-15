using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class ServerAgent<A>
    {
        private Api MatchmakingApi;

        public MonoBehaviour Handler;
        public string Profile;
        public BackfillAttributes BackfillAttributes;
        public int TargetTeamSize;

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public int RequestTimeoutSeconds;
        public float PollingBackoffSeconds;
        public int MaxConsecutivePollingErrors;
        public float ConnectionGracePeriodSeconds;

        public bool LogBackfillUpdates;
        public bool LogPollingUpdates;

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO>() { };

        public Observable<Dictionary<string, BackfillResponseDTO<A>>> Backfills
        {
            get;
            private set;
        } = new Observable<Dictionary<string, BackfillResponseDTO<A>>>() { };

        public Dictionary<string, BackfillAssignedTicket<A>> Assignments { get; private set; } =
            new Dictionary<string, BackfillAssignedTicket<A>>() { };

        private protected bool Polling = false;
        private protected bool BackfillRunning = false;
        private protected bool BackfillPending = false;

        public ServerAgent(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            string profile,
            BackfillAttributes attributes,
            int targetTeamSize = -1,
            int requestTimeoutSeconds = 3,
            float pollingBackoffSeconds = 1f,
            int maxConsecutivePollingErrors = 10,
            float connectionGracePeriodSeconds = 60f,
            bool logBackfillUpdates = true,
            bool logPollingUpdates = false
        )
        {
            if (handler == null)
            {
                throw new Exception("MatchmakingServer Handler not assigned.");
            }

            Handler = handler;

            BaseUrl = baseUrl;
            AuthToken = authToken;
            Profile = profile;
            BackfillAttributes = attributes;
            TargetTeamSize = targetTeamSize;

            RequestTimeoutSeconds = requestTimeoutSeconds;
            PollingBackoffSeconds = pollingBackoffSeconds;
            MaxConsecutivePollingErrors = maxConsecutivePollingErrors;
            ConnectionGracePeriodSeconds = connectionGracePeriodSeconds;

            LogBackfillUpdates = logBackfillUpdates;
            LogPollingUpdates = logPollingUpdates;
        }

        public bool AbandonPlayer(string ticketID, bool expiredGrace = false)
        {
            if (Assignments.Remove(ticketID))
            {
                L.Log(
                    $"MM | Backfill - ticket abandoned{(expiredGrace ? " (connection grace period expired)" : "")} [{ticketID}]"
                );
                AddBackfills();
                return true;
            }

            L.Warn($"MM | Backfill - ticket abandon failed [{ticketID}]");
            return false;
        }

        public BackfillAssignedTicket<A> PlayerConnected(string ticketID)
        {
            if (Assignments.ContainsKey(ticketID))
            {
                if (Assignments[ticketID].ConnectedAt is null)
                {
                    Assignments[ticketID].ConnectedAt = DateTime.Now;
                }

                return Assignments[ticketID];
            }

            return null;
        }

        #region Server API

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

        public void AddBackfills()
        {
            if (BackfillRunning)
            {
                BackfillPending = true;
                return;
            }

            BackfillRunning = true;

            try
            {
                do
                {
                    BackfillPending = false;
                    int plannedBackfills =
                        TargetTeamSize - (Assignments.Count + Backfills.Current.Count);

                    for (int i = 0; i < plannedBackfills; ++i)
                    {
                        StartNewBackfill();
                    }
                } while (BackfillPending);
            }
            finally
            {
                BackfillRunning = false;
                BackfillPending = false;
            }
        }

        public void AbandonBackfill(string backfillID, Action onCompletedDelegate = null)
        {
            if (!Backfills.Current.ContainsKey(backfillID))
            {
                Backfills._Notify(
                    $"abandon failed (not found) [{backfillID}]",
                    ObservableActionType.Warn
                );
                return;
            }

            MatchmakingApi.DeleteBackfill(
                backfillID,
                (UnityWebRequest request) =>
                {
                    Backfills._Notify($"abandoned [{backfillID}]");

                    RemoveBackfill(Backfills.Current, backfillID, onCompletedDelegate);
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        Backfills._Notify(
                            $"abandon failed (not found) [{backfillID}]",
                            ObservableActionType.Warn
                        );
                    }
                    else
                    {
                        Backfills._Notify(
                            $"abandon failed [{backfillID}]\n{error}",
                            ObservableActionType.Warn
                        );
                    }

                    RemoveBackfill(Backfills.Current, backfillID, onCompletedDelegate);
                }
            );
        }

        public void AbandonAllBackfills(Action onCompletedDelegate = null)
        {
            Polling = false;

            ConcurrentQueue<string> requestsFinished = new ConcurrentQueue<string>();
            int requestsPending = Backfills.Current.Count;

            foreach (KeyValuePair<string, BackfillResponseDTO<A>> b in Backfills.Current)
            {
                AbandonBackfill(
                    b.Key,
                    () =>
                    {
                        requestsFinished.Enqueue(b.Key);
                    }
                );
            }

            if (onCompletedDelegate is not null)
            {
                Handler.StartCoroutine(
                    WaitForRequests(requestsFinished, requestsPending, onCompletedDelegate)
                );
            }
        }
        #endregion

        #region Initialization
        public void Initialize(
            Dictionary<string, InjectedTicketDTO<A>> tickets,
            UnityAction<
                Observable<MonitorResponseDTO>,
                ObservableActionType,
                string
            > onMonitorUpdate,
            UnityAction<
                Observable<Dictionary<string, BackfillResponseDTO<A>>>,
                ObservableActionType,
                string
            > onBackfillUpdate
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

            L.SubscribeLogger(Monitor, "MM", "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            L.SubscribeLogger(Backfills, "MM", "Backfill", LogBackfillUpdates);
            Backfills.Subscribe(onBackfillUpdate);

            foreach (InjectedTicketDTO<A> ticket in tickets.Values)
            {
                Assignments[ticket.ID] = new BackfillAssignedTicket<A>()
                {
                    ID = ticket.ID,
                    CreatedAt = ticket.CreatedAt,
                    PlayerIP = ticket.PlayerIP,
                    GroupID = ticket.GroupID,
                    Attributes = ticket.Attributes,
                };

                Handler.StartCoroutine(CheckTicketConnection(ticket.ID));
            }

            Status();
        }
        #endregion

        #region Internals

        internal void RemoveBackfill(
            Dictionary<string, BackfillResponseDTO<A>> backfills,
            string backfillID,
            Action onCompletedDelegate = null
        )
        {
            Dictionary<string, BackfillResponseDTO<A>> newBackfills = new Dictionary<
                string,
                BackfillResponseDTO<A>
            >(backfills);

            if (newBackfills.Remove(backfillID))
            {
                Backfills._Update(newBackfills, $"removed [{backfillID}]");
            }
            else
            {
                Backfills._Notify(
                    $"remove failed (not found) [{backfillID}]",
                    ObservableActionType.Warn
                );
            }

            if (onCompletedDelegate is not null)
            {
                onCompletedDelegate();
            }
        }

        internal void StartNewBackfill()
        {
            if (Assignments.Count + Backfills.Current.Count >= TargetTeamSize)
            {
                Backfills._Error("maximum capacity currently reached");
                return;
            }

            BackfillRequestDTO<A> backfill = new BackfillRequestDTO<A>(
                Profile,
                BackfillAttributes,
                Assignments
            );

            MatchmakingApi.CreateBackfill(
                backfill,
                (BackfillResponseDTO<A> backfillRes, UnityWebRequest request) =>
                {
                    backfillRes.CreatedAt = DateTime.Now;

                    Dictionary<string, BackfillResponseDTO<A>> newBackfills = new Dictionary<
                        string,
                        BackfillResponseDTO<A>
                    >(Backfills.Current);

                    newBackfills[backfillRes.ID] = backfillRes;
                    Backfills._Update(newBackfills, $"created [{backfillRes.ID}]");

                    if (!Polling && Backfills.Current.Count > 0)
                    {
                        Polling = true;
                        Handler.StartCoroutine(DelayMethod(() => StartPollingBackfills()));
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    Backfills._Error($"create failed\n{error}");
                }
            );
        }

        internal void StartPollingBackfills(int consecutiveErrors = 0)
        {
            if (!Polling)
            {
                Backfills._Notify($"polling stopped");
                return;
            }

            if (consecutiveErrors > MaxConsecutivePollingErrors)
            {
                Polling = false;
                Backfills._Error($"polling failed (reached maximum retries)");
                AbandonAllBackfills();
                return;
            }

            if (LogPollingUpdates)
            {
                Backfills._Notify(
                    $"polling [{consecutiveErrors + 1}/{MaxConsecutivePollingErrors}]"
                );
            }

            ConcurrentQueue<bool> requestsFinished = new ConcurrentQueue<bool>();
            int requestsPending = Backfills.Current.Count;

            foreach (BackfillResponseDTO<A> b in Backfills.Current.Values)
            {
                MatchmakingApi.GetBackfill<A>(
                    b.ID,
                    (BackfillResponseDTO<A> backfill, UnityWebRequest request) =>
                    {
                        requestsFinished.Enqueue(true);

                        if (backfill.Status == "ASSIGNED")
                        {
                            InjectedTicketDTO<A> ticket = backfill.AssignedTicket;

                            Assignments[ticket.ID] = new BackfillAssignedTicket<A>()
                            {
                                ID = ticket.ID,
                                CreatedAt = ticket.CreatedAt,
                                PlayerIP = ticket.PlayerIP,
                                GroupID = ticket.GroupID,
                                Attributes = ticket.Attributes,
                            };

                            Backfills._Update(Backfills.Current, $"assigned [{backfill.ID}]");
                            RemoveBackfill(Backfills.Current, backfill.ID);
                            Handler.StartCoroutine(CheckTicketConnection(ticket.ID));
                        }
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        requestsFinished.Enqueue(false);

                        if (request.responseCode == 404)
                        {
                            Backfills._Notify(
                                $"polling failed (not found) [{b.ID}]",
                                ObservableActionType.Warn
                            );

                            RemoveBackfill(Backfills.Current, b.ID, StartNewBackfill);
                        }
                        else if (request.responseCode != 429 && request.responseCode < 500)
                        {
                            Backfills._Error($"polling failed [{b.ID}]\n{error}");
                            AbandonBackfill(b.ID, StartNewBackfill);
                        }
                    }
                );
            }

            Handler.StartCoroutine(
                WaitForRequests(
                    requestsFinished,
                    requestsPending,
                    () =>
                    {
                        if (requestsFinished.Contains(false))
                        {
                            consecutiveErrors += 1;
                        }
                        else
                        {
                            consecutiveErrors = 0;
                        }

                        Handler.StartCoroutine(
                            DelayMethod(() => StartPollingBackfills(consecutiveErrors))
                        );
                    }
                )
            );
        }

        internal IEnumerator CheckTicketConnection(string ticketID)
        {
            yield return new WaitForSecondsRealtime(ConnectionGracePeriodSeconds);

            if (Assignments.ContainsKey(ticketID) && Assignments[ticketID].ConnectedAt is null)
            {
                AbandonPlayer(ticketID, true);
            }
        }

        internal IEnumerator DelayMethod(Action onDelayFinished, float? delaySeconds = null)
        {
            delaySeconds ??= PollingBackoffSeconds + (0.1f * Random.value);

            yield return new WaitForSeconds((float)delaySeconds);
            onDelayFinished();
        }

        internal IEnumerator WaitForRequests<T>(
            ConcurrentQueue<T> requests,
            int expectedCount,
            Action onComplete
        )
        {
            yield return new WaitUntil(() => requests.Count == expectedCount);
            onComplete.Invoke();
        }
        #endregion
    }
}

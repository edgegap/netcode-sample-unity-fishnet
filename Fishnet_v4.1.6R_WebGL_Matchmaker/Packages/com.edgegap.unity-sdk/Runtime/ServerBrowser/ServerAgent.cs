using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class ServerAgent<ServerInstanceMetadata, SlotMetadata>
        where ServerInstanceMetadata : MetadataDTO, new()
        where SlotMetadata : MetadataDTO, new()
    {
        private Api<ServerInstanceMetadata, SlotMetadata> Api;

        public MonoBehaviour Handler { get; private set; }

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public bool AcceptExpiredReservations = false;
        public UpdateMode UpdateMode = UpdateMode.Heartbeat;
        public int RequestTimeoutSeconds;
        public float HeartbeatIntervalSeconds;
        public int HeartbeatMaxConsecutiveErrors;

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO> { };
        public Observable<InstanceDTO<ServerInstanceMetadata, SlotMetadata>> Instance
        {
            get;
            private set;
        } = new Observable<InstanceDTO<ServerInstanceMetadata, SlotMetadata>>() { };
        public Observable<ConfirmReservationsResponseDTO> Confirmations { get; private set; } =
            new Observable<ConfirmReservationsResponseDTO>() { };

        public ConcurrentDictionary<string, byte> PendingConfirmations { get; private set; } =
            new ConcurrentDictionary<string, byte>();
        public ConcurrentQueue<SlotUpdateDTO<SlotMetadata>> PendingSlotUpdates
        {
            get;
            private set;
        } = new ConcurrentQueue<SlotUpdateDTO<SlotMetadata>>();
        public ConcurrentQueue<ServerInstanceMetadata> PendingInstanceUpdates
        {
            get;
            private set;
        } = new ConcurrentQueue<ServerInstanceMetadata>();

        private bool FlushingUpdates = false;

        public ServerAgent(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            bool acceptExpiredReservations = false,
            UpdateMode updateMode = UpdateMode.Heartbeat,
            int requestTimeoutSeconds = 10,
            float heartbeatIntervalSeconds = 10f,
            int heartbeatMaxConsecutiveErrors = 10
        )
        {
            if (handler == null)
            {
                throw new Exception("ServerAgent Handler not assigned.");
            }

            Handler = handler;

            BaseUrl = baseUrl;
            AuthToken = authToken;

            AcceptExpiredReservations = acceptExpiredReservations;
            UpdateMode = updateMode;
            RequestTimeoutSeconds = requestTimeoutSeconds;
            HeartbeatIntervalSeconds = heartbeatIntervalSeconds;
            HeartbeatMaxConsecutiveErrors = heartbeatMaxConsecutiveErrors;
        }

        public void Initialize(
            UnityAction<
                Observable<MonitorResponseDTO>,
                ObservableActionType,
                string
            > onMonitorUpdate,
            UnityAction<
                Observable<InstanceDTO<ServerInstanceMetadata, SlotMetadata>>,
                ObservableActionType,
                string
            > onInstanceUpdate,
            UnityAction<
                Observable<ConfirmReservationsResponseDTO>,
                ObservableActionType,
                string
            > onConfirmationsUpdate = null
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

            Api = new Api<ServerInstanceMetadata, SlotMetadata>(
                Handler,
                AuthToken,
                BaseUrl,
                RequestTimeoutSeconds
            );

            L.SubscribeLogger(Monitor, "SB", "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            L.SubscribeLogger(Instance, "SB", "Instance");
            Instance.Subscribe(onInstanceUpdate);

            L.SubscribeLogger(Confirmations, "SB", "Confirmations");
            if (onConfirmationsUpdate is not null)
            {
                Confirmations.Subscribe(onConfirmationsUpdate);
            }

            if (HeartbeatIntervalSeconds < RequestTimeoutSeconds)
            {
                RequestTimeoutSeconds = (int)HeartbeatIntervalSeconds;
                Monitor._Notify(
                    $"request timeout clamped to heartbeat [{RequestTimeoutSeconds}]",
                    ObservableActionType.Warn
                );
            }

            Status();
        }

        #region Agent API
        public void Status()
        {
            Api.GetMonitor(
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
                    Monitor._Error($"get monitor failed\n{error}", null);
                }
            );
        }

        public void DiscoverInstance(InstanceDTO<ServerInstanceMetadata, SlotMetadata> instance)
        {
            Api.CreateServerInstance(
                instance,
                (
                    InstanceDTO<ServerInstanceMetadata, SlotMetadata> response,
                    UnityWebRequest request
                ) =>
                {
                    Instance._Update(response, "discovered");
                    Handler.StartCoroutine(DelayedHeartbeat());
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 409)
                    {
                        Instance._Error("discovery duplicate");
                    }
                    else
                    {
                        Instance._Error($"discovery failed\n{error}");
                    }
                }
            );
        }

        public void DeleteInstance()
        {
            if (Instance.Current is null)
            {
                Instance._Update(null, "instance deleted");
                return;
            }

            Api.DeleteServerInstance(
                Instance.Current.RequestID,
                (UnityWebRequest request) =>
                {
                    Instance._Update(null, "instance deleted");
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        Instance._Update(null, "instance delete failed (not found)");
                    }
                    else
                    {
                        Instance._Error($"instance delete failed\n{error}", null);
                    }
                }
            );
        }

        public void ConfirmReservation(string pending)
        {
            if (!PendingConfirmations.TryAdd(pending, 1))
            {
                Confirmations._Notify($"duplicate [{pending}]", ObservableActionType.Warn);
                return;
            }

            Confirmations._Notify($"enqueued [{pending}]");

            if (UpdateMode == UpdateMode.Greedy)
            {
                FlushConfirmations();
            }
        }

        public void UpdateSlot(SlotUpdateDTO<SlotMetadata> update)
        {
            SlotDTO<SlotMetadata> slot = Instance.Current.Slots.Find(s => s.Name == update.Name);

            if (slot is null)
            {
                Instance._Error($"slot update failed (not found) [{update.Name}]");
                return;
            }

            if (slot.AvailableSeats + update.AvailableSeats < 0)
            {
                Instance._Error($"slot update failed (not enough seats) [{update.Name}]");
                return;
            }

            PendingSlotUpdates.Enqueue(update);
            Instance._Notify($"slot update enqueued [{update.Name}]");

            if (UpdateMode == UpdateMode.Greedy)
            {
                FlushConfirmations();
            }
        }

        public void UpdateInstance(ServerInstanceMetadata metadata)
        {
            PendingInstanceUpdates.Enqueue(metadata);
            Instance._Notify($"instance update enqueued");

            if (UpdateMode == UpdateMode.Greedy)
            {
                FlushConfirmations();
            }
        }
        #endregion

        #region Internals

        internal void Heartbeat(int consecutiveErrors = 0)
        {
            if (consecutiveErrors > HeartbeatMaxConsecutiveErrors)
            {
                DeleteInstance();
                return;
            }
            else if (Instance.Current is null)
            {
                return;
            }

            Api.KeepAliveServerInstance(
                Instance.Current.RequestID,
                (KeepAliveResponseDTO keepalive, UnityWebRequest request) =>
                {
                    Instance._Notify($"heartbeat ok");
                    Handler.StartCoroutine(DelayedHeartbeat());
                    if (!FlushingUpdates)
                    {
                        FlushConfirmations();
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        DiscoverInstance(Instance.Current);
                    }
                    else
                    {
                        Instance._Notify(
                            $"heartbeat failed [{consecutiveErrors}/{HeartbeatMaxConsecutiveErrors}]\n{error}",
                            ObservableActionType.Warn
                        );
                        Handler.StartCoroutine(DelayedHeartbeat(consecutiveErrors + 1));
                    }
                }
            );
        }

        internal IEnumerator DelayedHeartbeat(int consecutiveErrors = 0)
        {
            yield return new WaitForSeconds(HeartbeatIntervalSeconds + (0.1f * Random.value));
            Heartbeat(consecutiveErrors);
        }

        internal void FlushConfirmations()
        {
            if (PendingConfirmations.IsEmpty)
            {
                if (!PendingSlotUpdates.IsEmpty)
                {
                    FlushSlotUpdates();
                }
                else if (!PendingInstanceUpdates.IsEmpty)
                {
                    FlushInstanceUpdates();
                }
                return;
            }

            List<string> staged = new List<string>();
            // simulate concurrent HashSet by removing from concurrent dictionary
            foreach (var (key, _) in PendingConfirmations)
            {
                if (PendingConfirmations.TryRemove(key, out _))
                {
                    staged.Add(key);
                }
            }

            Api.ConfirmReservations(
                Instance.Current.RequestID,
                new ConfirmReservationsDTO { UserIDs = staged },
                (ConfirmReservationsResponseDTO response, UnityWebRequest request) =>
                {
                    Confirmations._Update(response, "confirmed");
                    response.Slots.ForEach(slot =>
                    {
                        int allocatedSeats = (
                            AcceptExpiredReservations
                                ? slot.AcceptedUserIDs.Count + slot.ExpiredUserIDs.Count
                                : slot.AcceptedUserIDs.Count
                        );
                        if (allocatedSeats > 0)
                        {
                            PendingSlotUpdates.Enqueue(
                                new SlotUpdateDTO<SlotMetadata>(slot.Name, allocatedSeats * -1)
                            );
                            Instance._Notify($"slot update enqueued [{slot.Name}]");
                        }
                    });
                    FlushSlotUpdates();
                },
                (string error, UnityWebRequest request) =>
                {
                    foreach (var pending in staged)
                    {
                        PendingConfirmations.TryAdd(pending, 1);
                    }
                    Confirmations._Error($"failed\n{error}");
                }
            );
        }

        internal void FlushSlotUpdates()
        {
            if (FlushingUpdates)
            {
                Instance._Notify(
                    "agent throttled concurrent slot update",
                    ObservableActionType.Warn
                );
                return;
            }

            FlushingUpdates = true;
            Dictionary<string, SlotUpdateDTO<SlotMetadata>> mergedUpdates =
                new Dictionary<string, SlotUpdateDTO<SlotMetadata>>();

            while (PendingSlotUpdates.TryDequeue(out SlotUpdateDTO<SlotMetadata> update))
            {
                if (!mergedUpdates.ContainsKey(update.Name))
                {
                    SlotDTO<SlotMetadata> slot = Instance.Current.Slots.Find(s =>
                        s.Name == update.Name
                    );

                    mergedUpdates[update.Name] = new SlotUpdateDTO<SlotMetadata>(
                        slot.Name,
                        (int)slot.AvailableSeats,
                        slot.Metadata
                    );
                }
                mergedUpdates[update.Name] = new SlotUpdateDTO<SlotMetadata>(
                    update.Name,
                    mergedUpdates[update.Name].AvailableSeats + update.AvailableSeats,
                    mergedUpdates[update.Name]?.Metadata?.Merge(update.Metadata)
                );
            }

            ConcurrentQueue<string> updatesFinished = new ConcurrentQueue<string>();
            foreach (var update in mergedUpdates.Values)
            {
                Api.UpdateSlot(
                    Instance.Current.RequestID,
                    update,
                    (SlotDTO<SlotMetadata> slot, UnityWebRequest request) =>
                    {
                        Instance.Current.Slots[
                            Instance.Current.Slots.FindIndex(slot => slot.Name == update.Name)
                        ] = slot;
                        Instance._Update(Instance.Current, $"slot updated [{update.Name}]");
                        updatesFinished.Enqueue(update.Name);
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        Instance._Error(
                            $"slot update failed, enqueuing for retry [{update.Name}]\n{error}"
                        );
                        PendingSlotUpdates.Enqueue(update);
                        updatesFinished.Enqueue(update.Name);
                    }
                );
            }
            Handler.StartCoroutine(
                WaitForUpdates(updatesFinished, mergedUpdates.Count, FlushInstanceUpdates)
            );
        }

        internal IEnumerator WaitForUpdates(
            ConcurrentQueue<string> updates,
            int expectedCount,
            Action onComplete
        )
        {
            yield return new WaitUntil(() => updates.Count == expectedCount);
            onComplete.Invoke();
        }

        internal void FlushInstanceUpdates()
        {
            if (!FlushingUpdates)
            {
                FlushingUpdates = true;

                if (PendingSlotUpdates.IsEmpty)
                {
                    Api.GetServerInstance(
                        Instance.Current.RequestID,
                        (
                            InstanceDTO<ServerInstanceMetadata, SlotMetadata> instance,
                            UnityWebRequest request
                        ) =>
                        {
                            Instance._Update(instance, "instance updated");
                            FlushingUpdates = false;
                        },
                        (string error, UnityWebRequest request) =>
                        {
                            Instance._Error(
                                $"instance update failed, enqueuing for retry\n{error}"
                            );
                            if (PendingInstanceUpdates.IsEmpty)
                            {
                                PendingInstanceUpdates.Enqueue(Instance.Current.Metadata);
                            }
                            FlushingUpdates = false;
                        }
                    );
                    return;
                }
            }

            ServerInstanceMetadata mergedUpdate = Instance.Current.Metadata;
            while (PendingInstanceUpdates.TryDequeue(out ServerInstanceMetadata update))
            {
                mergedUpdate = mergedUpdate.Merge(update);
            }

            Api.UpdateServerInstance(
                Instance.Current.RequestID,
                new InstanceUpdateDTO<ServerInstanceMetadata>() { Metadata = mergedUpdate },
                (
                    InstanceDTO<ServerInstanceMetadata, SlotMetadata> response,
                    UnityWebRequest request
                ) =>
                {
                    response.Slots = Instance.Current.Slots; // patch doesn't return slots in SB 1.0.0
                    Instance._Update(response, "instance updated");
                    FlushingUpdates = false;
                },
                (string error, UnityWebRequest request) =>
                {
                    Instance._Error($"instance update failed, enqueuing for retry\n{error}");
                    PendingInstanceUpdates.Enqueue(mergedUpdate);
                    FlushingUpdates = false;
                }
            );
        }
        #endregion
    }

    public enum UpdateMode
    {
        Heartbeat,
        Greedy,
    }
}

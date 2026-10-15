using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class ClientAgent<ServerInstanceMetadata, SlotMetadata>
        where ServerInstanceMetadata : MetadataDTO, new()
        where SlotMetadata : MetadataDTO, new()
    {
        private Api<ServerInstanceMetadata, SlotMetadata> Api;

        public MonoBehaviour Handler { get; private set; }

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }
        public int RequestTimeoutSeconds;

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO> { };
        public Observable<InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>> Instances
        {
            get;
            private set;
        } = new Observable<InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>> { };

        private FilterCompiler ListFilter;
        private string ListOrder = "";
        private uint ListLimit = 20;

        public ClientAgent(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            int requestTimeoutSeconds = 3
        )
        {
            if (handler == null)
            {
                throw new Exception("ServerAgent Handler not assigned.");
            }

            Handler = handler;

            BaseUrl = baseUrl;
            AuthToken = authToken;

            RequestTimeoutSeconds = requestTimeoutSeconds;
        }

        public void Initialize(
            UnityAction<
                Observable<MonitorResponseDTO>,
                ObservableActionType,
                string
            > onMonitorUpdate,
            UnityAction<
                Observable<InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>>,
                ObservableActionType,
                string
            > onInstancesUpdate
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

            Api = new Api<ServerInstanceMetadata, SlotMetadata>(Handler, AuthToken, BaseUrl);

            L.SubscribeLogger(Monitor, "SB", "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            L.SubscribeLogger(Instances, "SB", "Instances");
            Instances.Subscribe(onInstancesUpdate);

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
                    Monitor._Error($"get monitor failed (unexpected error)\n{error}", null);
                }
            );
        }

        public void ListInstances(
            FilterCompiler filter = null,
            SortCompiler order = null,
            uint limit = 20,
            string cursor = null
        )
        {
            ListInstances(filter, order?.ToString(), limit, cursor);
        }

        public void ListInstances(
            FilterCompiler filter = null,
            string order = "",
            uint limit = 20,
            string cursor = null
        )
        {
            ListFilter = filter;
            ListOrder = order;
            ListLimit = limit;

            Api.ListServerInstances(
                (
                    InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata> response,
                    UnityWebRequest request
                ) =>
                {
                    Instances._Update(response, "instance list retrieved");
                },
                (string error, UnityWebRequest request) =>
                {
                    Instances._Error($"instance list retrieval failed\n{error}", null);
                },
                cursor,
                filter,
                order,
                limit
            );
        }

        public void GetNextPage()
        {
            if (Instances.Current is null || Instances.Current.Pagination.NextCursor is null)
            {
                Instances._Error("instance list last page reached");
            }

            Api.ListServerInstances(
                (
                    InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata> response,
                    UnityWebRequest request
                ) =>
                {
                    response.ServerInstances.InsertRange(0, Instances.Current.ServerInstances);
                    Instances._Update(response, "instance list next page retrieved");
                },
                (string error, UnityWebRequest request) =>
                {
                    Instances._Error($"instance list next page retrieval failed\n{error}", null);
                },
                Instances.Current.Pagination.NextCursor,
                ListFilter,
                ListOrder,
                ListLimit
            );
        }

        public void RefreshList(string cursor = null)
        {
            Instances._Update(null, "cache deleted");
            ListInstances(ListFilter, ListOrder, ListLimit, cursor);
        }

        public void ReserveSeats(string policyName, List<string> userIDs)
        {
            Api.ReserveSeats(
                new ReservationsDTO()
                {
                    PolicyName = policyName,
                    Users = userIDs
                        .Select(userID => new ReservationsUserDTO() { UserID = userID })
                        .ToList(),
                },
                (AutoAssignReservationsResponseDTO response, UnityWebRequest request) =>
                {
                    Instances._Update(
                        new InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>()
                        {
                            ServerInstances = new List<
                                InstanceDTO<ServerInstanceMetadata, SlotMetadata>
                            >()
                            {
                                new InstanceDTO<ServerInstanceMetadata, SlotMetadata>()
                                {
                                    RequestID = response.RequestID,
                                    Server = response.Server,
                                    Slots = new List<SlotDTO<SlotMetadata>>()
                                    {
                                        new SlotDTO<SlotMetadata>() { Name = response.Slot.Name },
                                    },
                                },
                            },
                        },
                        "instance details retrieved"
                    );
                    Instances._Notify("seats reserved");
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        Instances._Error($"seats reservation failed (not found)");
                    }
                    else if (request.responseCode == 409)
                    {
                        Instances._Error($"seats reservation failed (reached capacity)");
                    }
                    else
                    {
                        Instances._Error($"seats reservation failed\n{error}");
                    }
                }
            );
        }

        public void ReserveSeats(string requestID, string slotName, List<string> userIDs)
        {
            Api.ReserveSeats(
                requestID,
                slotName,
                new ReservationsDTO()
                {
                    Users = userIDs
                        .Select(userID => new ReservationsUserDTO() { UserID = userID })
                        .ToList(),
                },
                (ReservationsDTO response, UnityWebRequest request) =>
                {
                    Instances._Notify("seats reserved");
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        Instances._Error($"seats reservation failed (not found)");
                    }
                    else if (request.responseCode == 409)
                    {
                        Instances._Error($"seats reservation failed (reached capacity)");
                    }
                    else
                    {
                        Instances._Error($"seats reservation failed\n{error}");
                    }
                }
            );
        }

        public void GetInstanceDetails(string requestID)
        {
            Api.GetServerInstance(
                requestID,
                (
                    InstanceDTO<ServerInstanceMetadata, SlotMetadata> response,
                    UnityWebRequest request
                ) =>
                {
                    int index = Instances.Current.ServerInstances.FindIndex(instance =>
                        instance.RequestID == requestID
                    );

                    if (index == -1)
                    {
                        Instances.Current.ServerInstances.Insert(0, response);
                        Instances._Update(Instances.Current, "instance not cached, prepending");
                    }
                    else
                    {
                        Instances.Current.ServerInstances[index] = response;
                        Instances._Update(Instances.Current, "instance details retrieved");
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    Instances._Error($"instance details retrieval failed\n{error}");
                }
            );
        }

        #endregion
    }
}

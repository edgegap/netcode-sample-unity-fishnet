using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class Api<ServerInstanceMetadata, SlotMetadata>
        where ServerInstanceMetadata : MetadataDTO, new()
        where SlotMetadata : MetadataDTO, new()
    {
        internal SafeHttpRequest Request;
        internal string AuthToken;
        internal string BaseUrl;

        internal string PATH_MONITOR = "monitor";
        internal string PATH_BEACONS = "locations/beacons";
        internal string PATH_SERVER_INSTANCES = "server-instances";
        internal string PATH_KEEP_ALIVE = "keep-alive";
        internal string PATH_SLOTS = "slots";
        internal string PATH_RESERVATIONS = "reservations";
        internal string PATH_CONFIRM = "confirm";

        public Api(
            MonoBehaviour parent,
            string authToken,
            string baseUrl,
            int requestTimeoutSeconds = 0
        )
        {
            Request = new SafeHttpRequest(parent, requestTimeoutSeconds);
            AuthToken = authToken;
            BaseUrl = baseUrl;
        }

        public void GetMonitor(
            Action<MonitorResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_MONITOR}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        MonitorResponseDTO monitor =
                            JsonConvert.DeserializeObject<MonitorResponseDTO>(response);
                        onSuccessDelegate(monitor, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse monitor, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void GetBeacons(
            Action<BeaconsResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_BEACONS}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        BeaconsResponseDTO beacons =
                            JsonConvert.DeserializeObject<BeaconsResponseDTO>(response);
                        onSuccessDelegate(beacons, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse beacons, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void CreateServerInstance(
            InstanceDTO<ServerInstanceMetadata, SlotMetadata> serverInstance,
            Action<
                InstanceDTO<ServerInstanceMetadata, SlotMetadata>,
                UnityWebRequest
            > onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}",
                AuthToken,
                serverInstance.ToString(),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        InstanceDTO<ServerInstanceMetadata, SlotMetadata> instance =
                            JsonConvert.DeserializeObject<
                                InstanceDTO<ServerInstanceMetadata, SlotMetadata>
                            >(response);
                        onSuccessDelegate(instance, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse server instance, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void ListServerInstances(
            Action<
                InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>,
                UnityWebRequest
            > onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            string cursor = null,
            FilterCompiler filter = null,
            SortCompiler order = null,
            uint limit = 20
        )
        {
            ListServerInstances(
                onSuccessDelegate,
                onErrorDelegate,
                cursor,
                filter,
                order?.ToString(),
                limit
            );
        }

        public void ListServerInstances(
            Action<
                InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>,
                UnityWebRequest
            > onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            string cursor = null,
            FilterCompiler filter = null,
            string order = "",
            uint limit = 20
        )
        {
            List<string> queryParams = new List<string>()
            {
                { $"$filter={filter?.ToString() ?? ""}" },
                { $"$order={order}" },
                { $"limit={limit}" },
            };
            if (cursor != null)
            {
                queryParams.Add($"cursor={cursor}");
            }

            Request.Get(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}?{string.Join("&", queryParams)}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata> instanceList =
                            JsonConvert.DeserializeObject<
                                InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>
                            >(response);
                        onSuccessDelegate(instanceList, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse server instance list, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void GetServerInstance(
            string requestID,
            Action<
                InstanceDTO<ServerInstanceMetadata, SlotMetadata>,
                UnityWebRequest
            > onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        InstanceDTO<ServerInstanceMetadata, SlotMetadata> instance =
                            JsonConvert.DeserializeObject<
                                InstanceDTO<ServerInstanceMetadata, SlotMetadata>
                            >(response);
                        onSuccessDelegate(instance, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse server instance, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void UpdateServerInstance(
            string requestID,
            InstanceUpdateDTO<ServerInstanceMetadata> update,
            Action<
                InstanceDTO<ServerInstanceMetadata, SlotMetadata>,
                UnityWebRequest
            > onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Patch(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}",
                AuthToken,
                update.ToString(),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        InstanceDTO<ServerInstanceMetadata, SlotMetadata> instance =
                            JsonConvert.DeserializeObject<
                                InstanceDTO<ServerInstanceMetadata, SlotMetadata>
                            >(response);
                        onSuccessDelegate(instance, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse server instance update response, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void UpdateSlot(
            string requestID,
            SlotUpdateDTO<SlotMetadata> update,
            Action<SlotDTO<SlotMetadata>, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Patch(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}/{PATH_SLOTS}/{update.Name}",
                AuthToken,
                update.ToString(),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        SlotDTO<SlotMetadata> slot = JsonConvert.DeserializeObject<
                            SlotDTO<SlotMetadata>
                        >(response);
                        onSuccessDelegate(slot, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse slot update response, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void DeleteServerInstance(
            string requestID,
            Action<UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Delete(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate
            );
        }

        public void KeepAliveServerInstance(
            string requestID,
            Action<KeepAliveResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}/{PATH_KEEP_ALIVE}",
                AuthToken,
                "",
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        KeepAliveResponseDTO keepAlive =
                            JsonConvert.DeserializeObject<KeepAliveResponseDTO>(response);
                        onSuccessDelegate(keepAlive, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse keepalive, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void ReserveSeats(
            ReservationsDTO reservations,
            Action<AutoAssignReservationsResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{PATH_RESERVATIONS}",
                AuthToken,
                reservations.ToString(),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        AutoAssignReservationsResponseDTO reservations =
                            JsonConvert.DeserializeObject<AutoAssignReservationsResponseDTO>(
                                response
                            );
                        onSuccessDelegate(reservations, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse seat reservations, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void ReserveSeats(
            string requestID,
            string slotName,
            ReservationsDTO reservations,
            Action<ReservationsDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}/{PATH_SLOTS}/{slotName}/{PATH_RESERVATIONS}",
                AuthToken,
                reservations.ToString(),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        ReservationsDTO reservations =
                            JsonConvert.DeserializeObject<ReservationsDTO>(response);
                        onSuccessDelegate(reservations, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse seat reservations, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void ConfirmReservations(
            string requestID,
            ConfirmReservationsDTO userIDs,
            Action<ConfirmReservationsResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}/{PATH_RESERVATIONS}/{PATH_CONFIRM}",
                AuthToken,
                userIDs.ToString(),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        ConfirmReservationsResponseDTO confirmation =
                            JsonConvert.DeserializeObject<ConfirmReservationsResponseDTO>(response);
                        onSuccessDelegate(confirmation, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Server Browser | Couldn't parse reservation confirmations, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }
    }
}

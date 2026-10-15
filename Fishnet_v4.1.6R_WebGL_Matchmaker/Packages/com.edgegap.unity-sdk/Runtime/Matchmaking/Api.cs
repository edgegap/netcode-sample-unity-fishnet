using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Edgegap.Matchmaking
{
    using BeaconsResponseDTO = Edgegap.BeaconsResponseDTO;
    using L = Logger;

    public class Api
    {
        internal SafeHttpRequest Request;
        internal string AuthToken;
        internal string BaseUrl;

        internal string PATH_MONITOR = "monitor";
        internal string PATH_BEACONS = "locations/beacons";
        internal string PATH_TICKETS = "tickets";
        internal string PATH_GROUP_TICKETS = "group-tickets";
        internal string PATH_GROUP_UP = "groups";
        internal string PATH_BACKFILL = "backfills";

        public Api(MonoBehaviour parent, string authToken, string baseUrl)
        {
            Request = new SafeHttpRequest(parent);
            AuthToken = authToken;
            BaseUrl = baseUrl;
        }

        #region Utility

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
        #endregion

        #region Server-to-Server Tickets
        public void CreateTicketAsync<T, A>(
            T ticket,
            Action<TicketResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
            where T : TicketsRequestDTO<A>
        {
            Request.Post(
                $"{BaseUrl}/{PATH_TICKETS}",
                AuthToken,
                JsonConvert.SerializeObject(ticket),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        TicketResponseDTO assignment =
                            JsonConvert.DeserializeObject<TicketResponseDTO>(response);
                        onSuccessDelegate(assignment, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse assignment, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void CreateGroupTicketAsync<T, A>(
            T groupTicket,
            Action<GroupTicketsResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
            where T : GroupTicketsRequestDTO<A>
        {
            Request.Post(
                $"{BaseUrl}/{PATH_GROUP_TICKETS}",
                AuthToken,
                JsonConvert.SerializeObject(groupTicket),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        GroupTicketsResponseDTO assignment =
                            JsonConvert.DeserializeObject<GroupTicketsResponseDTO>(response);
                        onSuccessDelegate(assignment, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse assignment, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void GetTicketAsync(
            string ticketID,
            Action<TicketResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_TICKETS}/{ticketID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        TicketResponseDTO assignment =
                            JsonConvert.DeserializeObject<TicketResponseDTO>(response);
                        onSuccessDelegate(assignment, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse assignment, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void DeleteTicketAsync(
            string ticketID,
            Action<UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Delete(
                $"{BaseUrl}/{PATH_TICKETS}/{ticketID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate
            );
        }
        #endregion

        #region Client Group-Up
        public void CreateGroup<G, A>(
            G group,
            Action<GroupUpResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
            where G : GroupUpRequestDTO<A>
        {
            Request.Post(
                $"{BaseUrl}/{PATH_GROUP_UP}",
                AuthToken,
                JsonConvert.SerializeObject(group),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        GroupUpResponseDTO group =
                            JsonConvert.DeserializeObject<GroupUpResponseDTO>(response);
                        onSuccessDelegate(group, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Couldn't parse group, consider updating Matchmaking SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void GetGroup(
            string groupID,
            Action<GroupDetailResponse, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_GROUP_UP}/{groupID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        GroupDetailResponse details =
                            JsonConvert.DeserializeObject<GroupDetailResponse>(response);
                        onSuccessDelegate(details, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Couldn't parse details, consider updating Matchmaking SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void DeleteGroup(
            string groupID,
            Action<UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Delete(
                $"{BaseUrl}/{PATH_GROUP_UP}/{groupID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate
            );
        }

        public void CreateGroupMember<G, A>(
            G member,
            string groupID,
            Action<GroupUpResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
            where G : GroupUpRequestDTO<A>
        {
            Request.Post(
                $"{BaseUrl}/{PATH_GROUP_UP}/{groupID}/members",
                AuthToken,
                JsonConvert.SerializeObject(member),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        GroupUpResponseDTO member =
                            JsonConvert.DeserializeObject<GroupUpResponseDTO>(response);
                        onSuccessDelegate(member, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Couldn't parse member, consider updating Matchmaking SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void GetGroupMember(
            string groupID,
            string memberID,
            Action<GroupUpResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_GROUP_UP}/{groupID}/members/{memberID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        GroupUpResponseDTO details =
                            JsonConvert.DeserializeObject<GroupUpResponseDTO>(response);
                        onSuccessDelegate(details, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Couldn't parse details, consider updating Matchmaking SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void UpdateGroupMember(
            string groupID,
            string memberID,
            bool isReady,
            Action<GroupUpResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Patch(
                $"{BaseUrl}/{PATH_GROUP_UP}/{groupID}/members/{memberID}",
                AuthToken,
                JsonConvert.SerializeObject(
                    new Dictionary<string, bool> { { "is_ready", isReady } }
                ),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        GroupUpResponseDTO member =
                            JsonConvert.DeserializeObject<GroupUpResponseDTO>(response);
                        onSuccessDelegate(member, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Couldn't parse member, consider updating Matchmaking SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void DeleteGroupMember(
            string groupID,
            string memberID,
            Action<UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Delete(
                $"{BaseUrl}/{PATH_GROUP_UP}/{groupID}/members/{memberID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate
            );
        }
        #endregion

        #region Server Backfill
        public void CreateBackfill<A>(
            BackfillRequestDTO<A> backfill,
            Action<BackfillResponseDTO<A>, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_BACKFILL}",
                AuthToken,
                JsonConvert.SerializeObject(backfill),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        BackfillResponseDTO<A> backfillRes = JsonConvert.DeserializeObject<
                            BackfillResponseDTO<A>
                        >(response);
                        onSuccessDelegate(backfillRes, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Couldn't parse backfill, consider updating Matchmaking SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void GetBackfill<A>(
            string backfillID,
            Action<BackfillResponseDTO<A>, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_BACKFILL}/{backfillID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        BackfillResponseDTO<A> backfillRes = JsonConvert.DeserializeObject<
                            BackfillResponseDTO<A>
                        >(response);
                        onSuccessDelegate(backfillRes, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Couldn't parse backfill, consider updating Matchmaking SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void DeleteBackfill(
            string backfillID,
            Action<UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Delete(
                $"{BaseUrl}/{PATH_BACKFILL}/{backfillID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate
            );
        }
        #endregion
    }
}

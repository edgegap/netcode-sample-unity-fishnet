using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public class GroupUpResponseDTO
    {
        [JsonProperty("member_id")]
        public string MemberID;

        [JsonProperty("group_id")]
        public string GroupID;

        [JsonProperty("is_ready")]
        public bool IsReady;

        [JsonProperty("status")]
        public string Status;

#nullable enable
        [JsonProperty("ticket_id")]
        public string? TicketID;

        [JsonProperty("assignment")]
        public DeploymentDTO? Assignment;

        [JsonProperty("team_id")]
        public string? TeamID;
#nullable disable

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class GroupDetailResponse
    {
        [JsonProperty("status")]
        public string Status;

#nullable enable
        [JsonProperty("assignment")]
        public DeploymentDTO? Assignment;

        [JsonProperty("team_id")]
        public string? TeamID;
#nullable disable

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}


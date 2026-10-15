using System;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public class TicketResponseDTO
    {
        [JsonProperty("id")]
        public string ID;

        [JsonProperty("profile")]
        public string Profile;

        [JsonProperty("created_at")]
        public DateTime CreatedAt;

        [JsonProperty("status")]
        public string Status;

#nullable enable
        [JsonProperty("player_ip")]
        public string? PlayerIP;

        [JsonProperty("group_id")]
        public string? GroupID;

        [JsonProperty("team_id")]
        public string? TeamID;

        [JsonProperty("match_id")]
        public string? MatchID;

        [JsonProperty("assignment")]
        public DeploymentDTO? Assignment;

#nullable disable

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

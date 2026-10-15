using System;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public class InjectedTicketDTO<A>
    {
        [JsonProperty("id")]
        public string ID;

        [JsonProperty("created_at")]
        public DateTime CreatedAt;

        [JsonProperty("player_ip")]
        public string PlayerIP;

        [JsonProperty("group_id")]
        public string GroupID;

#nullable enable
        [JsonProperty("team_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? TeamID;

#nullable disable

        [JsonProperty("attributes")]
        public A Attributes;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class BackfillAssignedTicket<A> : InjectedTicketDTO<A>
    {
#nullable enable
        [JsonIgnore]
        public DateTime? ConnectedAt;
#nullable disable
    }
}

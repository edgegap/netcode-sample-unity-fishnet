using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public class BackfillResponseDTO<A>
    {
        [JsonProperty("id")]
        public string ID;

        [JsonProperty("profile")]
        public string Profile;

        [JsonProperty("tickets")]
        public Dictionary<string, InjectedTicketDTO<A>> Tickets;

        [JsonProperty("status")]
        public string Status;

#nullable enable
        [JsonProperty("assigned_ticket")]
        public InjectedTicketDTO<A>? AssignedTicket;

        [JsonIgnore]
        public DateTime? CreatedAt;

#nullable disable

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

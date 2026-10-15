using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public abstract class TicketsRequestDTO<A>
    {
        [JsonProperty("profile")]
        public string Profile;

        [JsonProperty("attributes")]
        public A Attributes;

#nullable enable
        [JsonProperty("player_ip")]
        public string? PlayerIP;

#nullable disable

        public TicketsRequestDTO(string profile)
        {
            Profile = profile;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

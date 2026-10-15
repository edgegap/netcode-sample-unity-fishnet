using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public class BackfillRequestDTO<A>
    {
        [JsonProperty("profile")]
        public string Profile;

        [JsonProperty("attributes")]
        public BackfillAttributes Attributes;

        [JsonProperty("tickets")]
        public Dictionary<string, BackfillAssignedTicket<A>> Tickets;

        public BackfillRequestDTO(
            string profile,
            BackfillAttributes attributes,
            Dictionary<string, BackfillAssignedTicket<A>> tickets
        )
        {
            Profile = profile;
            Attributes = attributes;
            Tickets = tickets;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class BackfillAssignment : DeploymentDTO
    {
        [JsonProperty("request_id")]
        public string RequestID;
    }

    public class BackfillAttributes
    {
        [JsonProperty("assignment")]
        public BackfillAssignment Assignment;

        public BackfillAttributes(BackfillAssignment assignment)
        {
            Assignment = assignment;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SimpleBackfillRequestDTO : BackfillRequestDTO<BackfillTicketAttributesDTO>
    {
        public SimpleBackfillRequestDTO(
            string profile,
            BackfillAttributes attributes,
            Dictionary<string, BackfillAssignedTicket<BackfillTicketAttributesDTO>> tickets
        )
            : base(profile, attributes, tickets) { }
    }

    public class BackfillTicketAttributesDTO : LatenciesAttributesDTO
    {
        [JsonProperty("backfill_group_size")]
        public string[] BackfillGroupSize;

        public BackfillTicketAttributesDTO(
            Dictionary<string, float> beacons,
            string[] backfillGroupSize
        )
            : base(beacons)
        {
            BackfillGroupSize = backfillGroupSize;
        }
    }
}

using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public class GroupTicketsResponseDTO
    {
        [JsonProperty("player_tickets")]
        public TicketResponseDTO[] Tickets;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

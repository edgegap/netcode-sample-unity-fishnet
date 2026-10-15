using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class ReservationsDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("users")]
        public List<ReservationsUserDTO> Users;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ReservationsUserDTO
    {
        [JsonProperty("user_id")]
        public string UserID;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AutoAssignReservationsResponseDTO
    {
        [JsonProperty("request_id")]
        public string RequestID;

        [JsonProperty("server")]
        public DeploymentDTO Server;

        [JsonProperty("slot")]
        public AssignedSlotDTO Slot;

        [JsonProperty("users")]
        public List<ReservationsUserDTO> Users;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AssignedSlotDTO
    {
        [JsonProperty("name")]
        public string Name;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ConfirmReservationsDTO
    {
        [JsonProperty("user_ids")]
        public List<string> UserIDs;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ConfirmReservationsResponseDTO
    {
        [JsonProperty("unknown_user_ids")]
        public List<string> UnknownIDs;

        [JsonProperty("slots")]
        public List<SlotConfirmations> Slots;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SlotConfirmations
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("accepted_user_ids")]
        public List<string> AcceptedUserIDs;

        [JsonProperty("expired_user_ids")]
        public List<string> ExpiredUserIDs;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

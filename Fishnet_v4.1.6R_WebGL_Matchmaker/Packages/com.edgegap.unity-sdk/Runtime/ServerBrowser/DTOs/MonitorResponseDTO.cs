using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class MonitorResponseDTO
    {
        [JsonProperty("status")]
        public string Status;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

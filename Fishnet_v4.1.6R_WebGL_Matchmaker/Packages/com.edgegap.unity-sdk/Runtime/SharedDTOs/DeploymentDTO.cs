using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap
{
    public class DeploymentDTO
    {
        [JsonProperty("fqdn")]
        public string Fqdn;

        [JsonProperty("public_ip")]
        public string PublicIP;

        [JsonProperty("ports")]
        public Dictionary<string, PortMappingDTO> Ports;

        [JsonProperty("location")]
        public LocationDTO Location;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class PortMappingDTO
    {
        [JsonProperty("internal")]
        public string Internal;

        [JsonProperty("external")]
        public string External;

        [JsonProperty("link")]
        public string Link;

        [JsonProperty("protocol")]
        public string Protocol;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

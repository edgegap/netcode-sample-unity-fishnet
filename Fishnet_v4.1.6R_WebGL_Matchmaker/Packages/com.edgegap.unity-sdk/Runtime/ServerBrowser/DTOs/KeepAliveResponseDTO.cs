using System;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class KeepAliveResponseDTO
    {
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

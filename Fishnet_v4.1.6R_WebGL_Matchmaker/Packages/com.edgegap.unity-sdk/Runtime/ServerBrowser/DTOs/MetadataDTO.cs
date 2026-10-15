using System.Reflection;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class MetadataDTO
    {
        public static JsonSerializerSettings SerializationSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        public T Merge<T>(T other)
            where T : MetadataDTO, new()
        {
            T result = new T();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (var field in result.GetType().GetFields(flags))
            {
                field.SetValue(result, field.GetValue(other) ?? field.GetValue(this));
            }

            return result;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, SerializationSettings);
        }
    }

    public class SimpleInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;
    }

    public class SimpleSlotMetadataDTO : MetadataDTO { }

    public class SocialInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("level")]
        public string Level;

        [JsonProperty("mode")]
        public string Mode;

        [JsonProperty("difficulty")]
        public string Difficulty;

        [JsonProperty("seed")]
        public string Seed;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("location.city")]
        public string City;
    }

    public class SocialSlotMetadataDTO : MetadataDTO
    {
        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("avg_latency")]
        public float AvgLatency;

        [JsonProperty("player_ids")]
        public string PlayerIDs;
    }

    public class CooperativeInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("level")]
        public string Level;

        [JsonProperty("mode")]
        public string Mode;

        [JsonProperty("difficulty")]
        public string Difficulty;

        [JsonProperty("avg_rank")]
        public int AvgRank;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("tags")]
        public string Tags;

        [JsonProperty("match_id")]
        public string MatchID;

        [JsonProperty("location.city")]
        public string City;
    }

    public class CooperativeSlotMetadataDTO : MetadataDTO
    {
        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("player_ids")]
        public string PlayerIDs;
    }

    public class CompetitiveInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("avg_rank")]
        public int AvgRank;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("is_ranked")]
        public bool IsRanked;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("cpu_frequency")]
        public int CpuFrequency;

        [JsonProperty("match_id")]
        public string MatchID;

        [JsonProperty("location.city")]
        public string City;
    }

    public class CompetitiveSlotMetadataDTO : MetadataDTO
    {
        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("player_ids")]
        public string PlayerIDs;

        [JsonProperty("avg_rank")]
        public int AvgRank;

        [JsonProperty("avg_latency")]
        public int AvgLatency;
    }
}

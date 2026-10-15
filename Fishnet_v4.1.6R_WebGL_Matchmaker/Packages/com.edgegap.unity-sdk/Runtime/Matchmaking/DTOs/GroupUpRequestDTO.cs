using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    public abstract class GroupUpRequestDTO<A> : TicketsRequestDTO<A>
    {
        [JsonProperty("is_ready")]
        public bool IsReady;

        public GroupUpRequestDTO(string profile, bool isReady = false)
            : base(profile)
        {
            IsReady = isReady;
        }
    }

    public class SimpleGroupUpRequestDTO : GroupUpRequestDTO<LatenciesAttributesDTO>
    {
        public SimpleGroupUpRequestDTO(
            Dictionary<string, float> latencyBeacons,
            bool isReady = false
        )
            : base("simple-example", isReady)
        {
            Attributes = new LatenciesAttributesDTO(latencyBeacons);
        }
    }

    public class BackfillGroupUpRequestDTO : GroupUpRequestDTO<BackfillTicketAttributesDTO>
    {
        public BackfillGroupUpRequestDTO(
            Dictionary<string, float> latencyBeacons,
            string[] backfillGroupSize,
            bool isReady = false
        )
            : base("backfill-example", isReady)
        {
            Attributes = new BackfillTicketAttributesDTO(latencyBeacons, backfillGroupSize);
        }
    }

    public class AdvancedGroupUpRequestDTO : GroupUpRequestDTO<AdvancedGroupUpAttributesDTO>
    {
        public AdvancedGroupUpRequestDTO(
            Dictionary<string, float> latencyBeacons,
            int eloRating,
            string selectedGameMode,
            string[] selectedMap,
            string[] backfillGroupSize
        )
            : base("advanced-example")
        {
            Attributes = new AdvancedGroupUpAttributesDTO(
                latencyBeacons,
                eloRating,
                selectedGameMode,
                selectedMap,
                backfillGroupSize
            );
        }
    }

    public class LatenciesAttributesDTO
    {
        [JsonProperty("beacons")]
        public Dictionary<string, float> Beacons;

        public LatenciesAttributesDTO(Dictionary<string, float> beacons)
        {
            Beacons = beacons;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AdvancedGroupUpAttributesDTO : LatenciesAttributesDTO
    {
        [JsonProperty("elo_rating")]
        public int EloRating;

        [JsonProperty("selected_game_mode")]
        public string SelectedGameMode;

        [JsonProperty("selected_map")]
        public string[] SelectedMap;

        [JsonProperty("backfill_group_size")]
        public string[] BackfillGroupSize;

        public AdvancedGroupUpAttributesDTO(
            Dictionary<string, float> beacons,
            int eloRating,
            string selectedGameMode,
            string[] selectedMap,
            string[] backfillGroupSize
        )
            : base(beacons)
        {
            EloRating = eloRating;
            SelectedGameMode = selectedGameMode;
            SelectedMap = selectedMap;
            BackfillGroupSize = backfillGroupSize;
        }
    }
}

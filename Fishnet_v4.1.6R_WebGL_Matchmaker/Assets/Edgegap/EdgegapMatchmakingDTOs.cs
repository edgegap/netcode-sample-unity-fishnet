// Edgegap matchmaking ticket data. Requires com.edgegap.unity-sdk 3.5.5+.
// The profile name sent in each ticket must equal a profile name in the matchmaker config,
// and every attribute's JSON name must equal the rule attribute name in that profile.
using System.Collections.Generic;
using Edgegap.Matchmaking;
using Newtonsoft.Json;

/// <summary>
/// Ticket attributes. Add one field per custom rule in your matchmaker profile
/// (e.g. "selected_map" for a string_equality/intersection rule, "elo_rating" for number_difference).
/// </summary>
public class GameTicketAttributes
{
    // Only sent when latency beacons were measured. WebGL can't ping (no ICMP in browsers),
    // so the key is omitted there and the profile must not have a `latencies` rule for WebGL tickets.
    [JsonProperty("beacons", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, float> Beacons;

    [JsonConstructor]
    public GameTicketAttributes(Dictionary<string, float> beacons)
    {
        Beacons = beacons;
    }

    public override string ToString() => JsonConvert.SerializeObject(this);
}

/// <summary>
/// Group Up membership request. A solo player is a group of one that is ready immediately.
/// </summary>
public class GameGroupUpRequestDTO : GroupUpRequestDTO<GameTicketAttributes>
{
    public GameGroupUpRequestDTO(string profile, Dictionary<string, float> beacons, bool isReady = true)
        : base(profile, isReady)
    {
        Attributes = new GameTicketAttributes(beacons);
    }
}

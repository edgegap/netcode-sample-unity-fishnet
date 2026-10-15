using System.Collections.Generic;
using Edgegap.Matchmaking;
using Newtonsoft.Json;

// todo define custom ticket request Data Transfer Objects based on config
// todo rename all CustomXYZ to MyXYZ, remove Simple example above
public class CustomTicketsRequestDTO : TicketsRequestDTO<CustomTicketsRequestAttributes>
{
    public CustomTicketsRequestDTO(Dictionary<string, float> latencyBeacons)
        // todo replace custom-profile with your profile name
        : base("custom-profile") { }
};

public class CustomTicketsRequestAttributes : LatenciesAttributesDTO
{
    // todo define custom attributes with Newtonsoft (de)serialization
    // https://www.newtonsoft.com/json/help/html/SerializationAttributes.htm

    public CustomTicketsRequestAttributes(Dictionary<string, float> beacons)
        : base(beacons)
    {
        // todo assign custom attributes
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Edgegap
{
    using L = Logger;

    public class DeploymentEnvironmentDTO
    {
        public string RequestID;
        public string HostID;
        public string PublicIP;
        public List<string> Tags;

        public uint HostBaseClockFrequency;
        public uint DeploymentVCPUUnits;
        public uint DeploymentMemoryMB;

        public string SelfStopURL;
        public string SelfStopToken;

        public bool PrivateBeaconEnabled;
        public string BeaconPublicIP;
        public uint BeaconPortUDP;
        public uint BeaconPortTCP;

        public LocationDTO Location;
        public Dictionary<string, PortMappingDTO> PortMapping;

        public string Fqdn => string.IsNullOrEmpty(RequestID) ? "" : $"{RequestID}.pr.edgegap.net";

        public DeploymentEnvironmentDTO(IDictionary env)
        {
            foreach (DictionaryEntry entry in env)
            {
                string key = entry.Key.ToString();

                if (key == "ARBITRIUM_REQUEST_ID")
                {
                    RequestID = TryParseEnvVariableString(entry);
                }
                else if (key == "ARBITRIUM_HOST_ID")
                {
                    HostID = TryParseEnvVariableString(entry);
                }
                else if (key == "ARBITRIUM_PUBLIC_IP")
                {
                    PublicIP = TryParseEnvVariableString(entry);
                }
                else if (key == "ARBITRIUM_DEPLOYMENT_TAGS")
                {
                    Tags = TryParseEnvVariableString(entry).Split(",").ToList();
                }
                else if (key == "ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY")
                {
                    HostBaseClockFrequency = TryParseEnvVariableUInt(entry);
                }
                else if (key == "ARBITRIUM_DEPLOYMENT_VCPU_UNITS")
                {
                    DeploymentVCPUUnits = TryParseEnvVariableUInt(entry);
                }
                else if (key == "ARBITRIUM_DEPLOYMENT_MEMORY_MB")
                {
                    DeploymentMemoryMB = TryParseEnvVariableUInt(entry);
                }
                else if (key == "ARBITRIUM_DELETE_URL")
                {
                    SelfStopURL = TryParseEnvVariableString(entry);
                }
                else if (key == "ARBITRIUM_DELETE_TOKEN")
                {
                    SelfStopToken = TryParseEnvVariableString(entry);
                }
                else if (key == "ARBITRIUM_BEACON_ENABLED")
                {
                    PrivateBeaconEnabled = TryParseEnvVariableBool(entry);
                }
                else if (key == "ARBITRIUM_HOST_BEACON_PUBLIC_IP")
                {
                    BeaconPublicIP = TryParseEnvVariableString(entry);
                }
                else if (key == "ARBITRIUM_HOST_BEACON_PORT_UDP_EXTERNAL")
                {
                    BeaconPortUDP = TryParseEnvVariableUInt(entry);
                }
                else if (key == "ARBITRIUM_HOST_BEACON_PORT_TCP_EXTERNAL")
                {
                    BeaconPortTCP = TryParseEnvVariableUInt(entry);
                }
                else if (key == "ARBITRIUM_DEPLOYMENT_LOCATION")
                {
                    Location = TryParseEnvVariableJSON<LocationDTO>(entry);
                }
                else if (key == "ARBITRIUM_PORTS_MAPPING")
                {
                    PortMapping = TryParseEnvVariableJSON<PortMappingEnvironmentVariable>(
                        entry
                    ).Ports;
                }
            }

            foreach (PortMappingDTO port in PortMapping.Values)
            {
                port.Link ??= $"{Fqdn}:{port.External}";
            }
        }

        public static string TryParseEnvVariableString(DictionaryEntry keyValuePair)
        {
            return TryParseEnvVariable(keyValuePair, raw => raw ?? string.Empty);
        }

        public static uint TryParseEnvVariableUInt(DictionaryEntry keyValuePair)
        {
            return TryParseEnvVariable(keyValuePair, uint.Parse);
        }

        public static bool TryParseEnvVariableBool(DictionaryEntry keyValuePair)
        {
            return TryParseEnvVariable(keyValuePair, raw => raw == "true");
        }

        public static V TryParseEnvVariableJSON<V>(DictionaryEntry keyValuePair)
        {
            return TryParseEnvVariable(keyValuePair, JsonConvert.DeserializeObject<V>);
        }

        public static V TryParseEnvVariable<V>(
            DictionaryEntry keyValuePair,
            Func<string, V> convertFn
        )
        {
            V value = default;
            try
            {
                value = convertFn(keyValuePair.Value.ToString());
            }
            catch (Exception e)
            {
                L.Warn(
                    $"Edgegap Env | Couldn't parse variable '{keyValuePair.Key.ToString()}', "
                        + $"consider updating Edgegap SDK.\n{e.Message}"
                );
            }
            return value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    internal class PortMappingEnvironmentVariable
    {
        [JsonProperty("ports")]
        public Dictionary<string, PortMappingDTO> Ports { get; set; }
    }
}

using System;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public abstract class SlotBase<SlotMetadata>
        where SlotMetadata : MetadataDTO, new()
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("metadata")]
        public SlotMetadata Metadata = new SlotMetadata();

        [JsonProperty("available_seats")]
        public uint AvailableSeats;
    }

    public class SlotDTO<SlotMetadata> : SlotBase<SlotMetadata>
        where SlotMetadata : MetadataDTO, new()
    {
        [JsonProperty("reserved_seats")]
        public uint ReservedSeats;

        [JsonProperty("joinable_seats")]
        public uint JoinableSeats;

        public SlotDTO<SlotMetadata> ApplyUpdate(SlotUpdateDTO<SlotMetadata> update)
        {
            if (update.Name != Name)
            {
                throw new InvalidOperationException(
                    $"Slot name mismatch, expected '{Name}' but got '{update.Name}' (update)."
                );
            }

            return new SlotDTO<SlotMetadata>()
            {
                Name = Name,
                Metadata = Metadata.Merge(update.Metadata),
                AvailableSeats = (uint)(AvailableSeats + update.AvailableSeats),
                ReservedSeats = ReservedSeats,
                JoinableSeats = JoinableSeats,
            };
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SlotUpdateDTO<SlotMetadata> : SlotBase<SlotMetadata>
        where SlotMetadata : MetadataDTO, new()
    {
        public static JsonSerializerSettings SerializationSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        [JsonIgnore]
        public new string Name;

        [JsonProperty("available_seats")]
        public new int AvailableSeats;

        public SlotUpdateDTO(string name, int availableSeats, SlotMetadata metadata = null)
        {
            Name = name;
            AvailableSeats = availableSeats;
            Metadata = metadata;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, SerializationSettings);
        }
    }
}

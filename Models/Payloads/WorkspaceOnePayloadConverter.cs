using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads
{
    /// <summary>
    /// Deserializes Workspace ONE profile payload entries into the correct concrete payload type
    /// based on the "PayloadType" discriminator.
    /// </summary>
    public sealed class WorkspaceOnePayloadConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => typeof(WorkspaceOnePayloadBase).IsAssignableFrom(objectType);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var jo = JObject.Load(reader);
            var payloadType = jo["PayloadType"]?.ToString();

            Type targetType = payloadType switch
            {
                "WiFi" => typeof(WiFiPayload),
                "Restriction.Plural" => typeof(RestrictionPluralPayload),
                "DataUsage" => typeof(DataUsagePayload),
                "Custom" => typeof(CustomPayload),
                "CustomSettings" => typeof(CustomSettingsPayload),
                "ApplicationControl" => typeof(ApplicationControlPayload),
                _ => typeof(UnknownPayload),
            };

            // Create a new reader from the JObject so the serializer can populate the target type.
            return jo.ToObject(targetType, serializer);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // Default serialization is fine.
            serializer.Serialize(writer, value);
        }
    }
}

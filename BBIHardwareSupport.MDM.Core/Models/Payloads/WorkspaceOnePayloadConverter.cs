using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
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
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jo = JObject.Load(reader);
            var payloadType = jo["PayloadType"]?.Value<string>();

            var targetType = payloadType switch
            {
                "WiFi" => typeof(WiFiPayload),
                "Restriction.Plural" => typeof(RestrictionPluralPayload),
                "DataUsage" => typeof(DataUsagePayload),
                "Custom" => typeof(CustomPayload),
                "CustomSettings" => typeof(CustomSettingsPayload),
                "ApplicationControl" => typeof(ApplicationControlPayload),
                "Permission.plural" => typeof(AndroidForWorkPermissionsPayload),
                _ => typeof(UnknownPayload)
            };

            return jo.ToObject(targetType, serializer);
        }

        private static JsonSerializer CreateSerializerWithoutThisConverter(JsonSerializer serializer)
        {
            var nested = new JsonSerializer
            {
                Culture = serializer.Culture,
                ContractResolver = serializer.ContractResolver,
                NullValueHandling = serializer.NullValueHandling,
                DefaultValueHandling = serializer.DefaultValueHandling,
                ObjectCreationHandling = serializer.ObjectCreationHandling,
                MissingMemberHandling = serializer.MissingMemberHandling,
                ReferenceLoopHandling = serializer.ReferenceLoopHandling,
                TypeNameHandling = serializer.TypeNameHandling,
                MetadataPropertyHandling = serializer.MetadataPropertyHandling,
                DateFormatHandling = serializer.DateFormatHandling,
                DateTimeZoneHandling = serializer.DateTimeZoneHandling,
                DateParseHandling = serializer.DateParseHandling,
                FloatFormatHandling = serializer.FloatFormatHandling,
                FloatParseHandling = serializer.FloatParseHandling,
                StringEscapeHandling = serializer.StringEscapeHandling,
                Formatting = serializer.Formatting,
                MaxDepth = serializer.MaxDepth,
                CheckAdditionalContent = serializer.CheckAdditionalContent,
            };

            foreach (var conv in serializer.Converters)
            {
                if (conv is WorkspaceOnePayloadConverter) continue;
                nested.Converters.Add(conv);
            }

            return nested;
        }


        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // Default serialization is fine.
            serializer.Serialize(writer, value);
        }
    }
}

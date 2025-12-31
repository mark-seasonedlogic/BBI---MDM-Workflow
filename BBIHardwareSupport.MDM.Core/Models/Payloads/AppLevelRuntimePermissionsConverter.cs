using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
{
    /// <summary>
    /// Handles AppLevelRuntimePermissions arriving either as:
    /// 1) JSON string: "[{...},{...}]"
    /// 2) actual JSON array: [{...},{...}]
    /// </summary>
    public sealed class AppLevelRuntimePermissionsConverter : JsonConverter<List<AndroidAppRuntimePermissions>>
    {
        public override List<AndroidAppRuntimePermissions>? ReadJson(
            JsonReader reader,
            Type objectType,
            List<AndroidAppRuntimePermissions>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return new List<AndroidAppRuntimePermissions>();

            if (reader.TokenType == JsonToken.String)
            {
                var s = (string?)reader.Value;
                if (string.IsNullOrWhiteSpace(s))
                    return new List<AndroidAppRuntimePermissions>();

                // string contains JSON
                var token = JToken.Parse(s);
                return token.ToObject<List<AndroidAppRuntimePermissions>>(serializer) ?? new List<AndroidAppRuntimePermissions>();
            }

            // direct array/object
            var jt = JToken.Load(reader);
            return jt.ToObject<List<AndroidAppRuntimePermissions>>(serializer) ?? new List<AndroidAppRuntimePermissions>();
        }

        public override void WriteJson(JsonWriter writer, List<AndroidAppRuntimePermissions>? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}

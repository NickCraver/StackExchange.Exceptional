using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Adapted from http://antonkallenberg.com/2015/03/13/json-net-case-insensitive-dictionary/
    /// Used for case insensitive dictionaries from configs, like HTTP header filters.
    /// </summary>
    /// <typeparam name="T">The type in the string-keyed dictionary.</typeparam>
    internal class CaseInsensitiveDictionaryConverter<T> : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jsonObject = JObject.Load(reader);
            var originalDictionary = JsonConvert.DeserializeObject<Dictionary<string, T>>(jsonObject.ToString());
            return originalDictionary == null ? null : new Dictionary<string, T>(originalDictionary, StringComparer.OrdinalIgnoreCase);
        }

        public override bool CanConvert(Type objectType) =>
            objectType.GetInterfaces().Count(i => HasGenericTypeDefinition(i, typeof(IDictionary<,>))) > 0;

        private static bool HasGenericTypeDefinition(Type objectType, Type typeDefinition) =>
            objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeDefinition;
    }
}

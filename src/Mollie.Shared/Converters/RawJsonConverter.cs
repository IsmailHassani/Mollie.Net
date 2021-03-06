﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Mollie.Converters
{
    public class RawJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(string));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return JToken.Load(reader).ToString();
        }
    }
}

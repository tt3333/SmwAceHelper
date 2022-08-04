using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmwAceHelper.Utilities
{
    public class CustomEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                T result;
                if (Enum.TryParse<T>(reader.GetString(), true, out result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return (T)Enum.ToObject(typeToConvert, reader.GetInt64());
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            string? result = Enum.GetName<T>(value);
            if (result == null)
            {
                writer.WriteNumberValue(Convert.ToInt64(value));
            }
            else
            {
                writer.WriteStringValue(result);
            }
        }
    }
}

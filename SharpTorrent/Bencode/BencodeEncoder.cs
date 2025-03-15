using System.Reflection;
using System.Text;

namespace SharpTorrent.Bencode
{
    public class BencodeEncoder
    {
        public string EncodeToBencode(object value)
        {
            var builder = new StringBuilder();
            // if is not a dictionary or a primitive try to convert the class to a dictionary
            if (value is not Dictionary<string, object> && value is not (string or int or long or double or bool))
            {
                value = EncodeClassToDictionary(value);
            }
            EncodeValue(value, builder);
            return builder.ToString();
        }

    private Dictionary<string, object> EncodeClassToDictionary(object value)
    {
        var toReturn = new Dictionary<string, object>();

        foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var fieldName = char.ToLower(field.Name[0]) + field.Name[1..];
            var valueOfField = field.GetValue(value);
            if (valueOfField == null) continue;
            toReturn.Add(fieldName, valueOfField);
        }

        return toReturn;
    }

        private void EncodeValue(object value, StringBuilder builder)
        {
            switch (value)
            {
                case string stringValue:
                    EncodeString(stringValue, builder);
                    break;
                case int or long or ulong or ushort or double or float or uint:
                    EncodeInteger(Convert.ToInt64(value), builder);
                    break;
                case Dictionary<string, object> dictionaryValue:
                    EncodeDictionary(dictionaryValue, builder);
                    break;
                case List<object> listValue:
                    EncodeList(listValue, builder);
                    break;
                case byte[] bytesValue:
                    EncodeByteArray(bytesValue, builder);
                    break;
                default:
                    throw new ArgumentException($"Unsupported type {value?.GetType().Name ?? "null"} for Bencode encoding");
            }
        }

        private void EncodeInteger(dynamic num, StringBuilder builder)
        {
            builder.Append('i');
            builder.Append(num.ToString());
            builder.Append('e');
        }

        private void EncodeString(string value, StringBuilder builder)
        {
            builder.Append(value.Length);
            builder.Append(':');
            builder.Append(value);
        }

        private void EncodeByteArray(byte[] value, StringBuilder builder)
        {
            builder.Append(value.Length);
            builder.Append(':');
            builder.Append(Encoding.ASCII.GetString(value));
        }

        private void EncodeDictionary(Dictionary<string, object> dict, StringBuilder builder)
        {
            builder.Append('d');

            // Sort dictionary keys lexicographically as required by BEP
            var keys = new List<string>(dict.Keys);
            keys.Sort(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                EncodeString(key, builder);
                EncodeValue(dict[key], builder);
            }

            builder.Append('e');
        }

        private void EncodeList(List<object> list, StringBuilder builder)
        {
            builder.Append('l');

            foreach (var item in list)
            {
                EncodeValue(item, builder);
            }

            builder.Append('e');
        }
    }
}

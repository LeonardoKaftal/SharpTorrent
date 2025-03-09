using System.Numerics;
using System.Text;

namespace SharpTorrent.Bencode
{
    public class BencodeEncoder
    {
        public string EncodeToBencode(object value)
        {
            StringBuilder builder = new StringBuilder();
            EncodeValue(value, builder);
            return builder.ToString();
        }

        private void EncodeValue(object value, StringBuilder builder)
        {
            switch (value)
            {
                case string stringValue:
                    EncodeString(stringValue, builder);
                    break;
                case int intValue:
                    EncodeInteger(intValue, builder);
                    break;
                case long longValue:
                    EncodeInteger(longValue, builder);
                    break;
                case BigInteger bigIntValue:
                    EncodeInteger(bigIntValue, builder);
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

        private void EncodeInteger(BigInteger value, StringBuilder builder)
        {
            builder.Append('i');
            builder.Append(value.ToString());
            builder.Append('e');
        }

        private void EncodeInteger(long value, StringBuilder builder)
        {
            builder.Append('i');
            builder.Append(value.ToString());
            builder.Append('e');
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





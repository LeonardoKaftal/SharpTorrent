using System.Reflection;
using System.Text;

namespace SharpTorrent.Bencode
{
    public class BencodeEncoder
    {
        public byte[] EncodeToBencode(object value)
        {
            using var stream = new MemoryStream();
            // if is not a dictionary or a primitive try to convert the class to a dictionary
            if (value is not Dictionary<string, object> && value is not (string or int or long or double or bool or byte[]))
            {
                value = EncodeClassToDictionary(value);
            }
            EncodeValue(value, stream);
            return stream.ToArray();
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

        private void EncodeValue(object value, MemoryStream stream)
        {
            switch (value)
            {
                case string stringValue:
                    EncodeString(stringValue, stream);
                    break;
                case int or long or ulong or ushort or double or float or uint:
                    EncodeInteger(Convert.ToInt64(value), stream);
                    break;
                case Dictionary<string, object> dictionaryValue:
                    EncodeDictionary(dictionaryValue, stream);
                    break;
                case List<object> listValue:
                    EncodeList(listValue, stream);
                    break;
                case byte[] bytesValue:
                    EncodeByteArray(bytesValue, stream);
                    break;
                default:
                    throw new ArgumentException($"Unsupported type {value?.GetType().Name ?? "null"} for Bencode encoding");
            }
        }

        private void EncodeInteger(long num, MemoryStream stream)
        {
            var bytes = Encoding.ASCII.GetBytes($"i{num}e");
            stream.Write(bytes, 0, bytes.Length);
        }

        private void EncodeString(string value, MemoryStream stream)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(value);
            byte[] lengthPrefix = Encoding.ASCII.GetBytes($"{stringBytes.Length}:");
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(stringBytes, 0, stringBytes.Length);
        }

        private void EncodeByteArray(byte[] value, MemoryStream stream)
        {
            byte[] lengthPrefix = Encoding.ASCII.GetBytes($"{value.Length}:");
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(value, 0, value.Length);
        }

        private void EncodeDictionary(Dictionary<string, object> dict, MemoryStream stream)
        {
            stream.WriteByte((byte)'d');
            
            // Sort dictionary keys lexicographically as required by BEP
            var keys = new List<string>(dict.Keys);
            keys.Sort(StringComparer.Ordinal);
            
            foreach (var key in keys)
            {
                EncodeString(key, stream);
                EncodeValue(dict[key], stream);
            }
            
            stream.WriteByte((byte)'e');
        }

        private void EncodeList(List<object> list, MemoryStream stream)
        {
            stream.WriteByte((byte)'l');
            
            foreach (var item in list)
            {
                EncodeValue(item, stream);
            }
            
            stream.WriteByte((byte)'e');
        }
    }
}

using System.Numerics;
using System.Text;

namespace SharpTorrent.Bencode;

public class BencodeParser
{

   // global index to traverse the bencode
   private int _index  = 0;
   
   public object ParseBencode(byte[] bencode)
   {
      if (bencode.Length == 0) throw new FormatException($"Invalid bencode: Is empty");
      var parsedValue = ParseValue(bencode);
      switch (parsedValue)
      {
         case Dictionary<string, object> dictionary:
            return dictionary;
         case string stringValue:
            return stringValue;
         case int intValue:
            return intValue;
         case List<object> list:
            return list;
      }

      throw new FormatException("Invalid bencode: impossible to parse the bencode");
   }

   private object ParseValue(byte[] bencode)
   {
      var c = (char)(bencode[_index]);
      
      return c switch
      {
         'd' => HandleDictionary(bencode),
         'i' => HandleInteger(bencode),
         'l' => HandleList(bencode),
         _ => HandleString(bencode)
      };
   }

   private Dictionary<string, object> HandleDictionary(byte[] bencode)
   {
      _index++; // skip 'd'
      var result = new Dictionary<string, object>();

      while (_index < bencode.Length && bencode[_index] != 'e')
      {
        var key = HandleString(bencode);
        if (_index >= bencode.Length)
            throw new FormatException("Invalid bencode: unexpected end in dictionary");
        object value;
        switch (key)
        {
           // pieces field or peers field in compact form contains string that cannot be parsed like a normal string as it would produce unpredictable result once converted again in bytes
           case "pieces":
              value = HandleNonUtf8String(bencode);
              break;
            case "peers":
                var startingIndex = _index;
                var peersValue = ParseValue(bencode);

                // BEP 23
                if (peersValue is string)
                {
                    _index = startingIndex;
                    value = HandleNonUtf8String(bencode);
                }
                else value = peersValue;
                break;
           default:
              value = ParseValue(bencode);
              break;
        }
        result.Add(key, value);
      }

      if (_index >= bencode.Length) throw new FormatException("Invalid bencode: unterminated dictionary");

      _index++; // skip 'e'
      return result;
   }
   

   private List<object> HandleList(byte[] bencode)
   {
      // skip l
      _index++;
      
      char c;
      List<object> toReturn = [];

      do
      {
         var value = ParseValue(bencode);
         toReturn.Add(value);

         if (_index == bencode.Length) throw new FormatException("Invalid bencode: the list is not closed");
         c = (char)(bencode[_index]);
      } while (c != 'e');
      
      // skip e
      _index++;
      
      return toReturn;
   }

   private string HandleString(byte[] bencode)
   {
      var start = _index;
      var end = _index;
      var c = (char) (bencode[end]);
      
      while (char.IsDigit(c))
      {
         _index++;
         end++;
         c = (char)bencode[end];
      }
      
      // if start == _index then it means a lenght has not been extracted because there were no numeric chars
      if (start == _index) throw new FormatException($"Invalid bencode: string at index {_index} miss length");

      var length= int.Parse(new ReadOnlySpan<byte>(bencode, start: start, length: end - start));
      if (length == 0) return "";
      
      if (_index + length >= bencode.Length)
         throw new FormatException($"Invalid bencode: current index: {_index}" +
                                   $"+ string length {length} exceed bencode length");

      if (bencode[_index] != ':') throw new FormatException("Invalid bencode: string miss : at index " + _index);
      // skip :
      _index++;

      var content = new ReadOnlySpan<byte>(bencode, _index, length);
      var result = Encoding.UTF8.GetString(content);
      _index += length;
      
      return result;
   }

   private long HandleInteger(byte[] bencode)
   {
      // skip i
      _index++;
      
      var start = _index;
      var end = _index; 
      
      var c = (char)bencode[_index];

      while (c != 'e' && _index != bencode.Length) 
      {
         if (!char.IsDigit(c) && c != '-') throw new FormatException($"Invalid bencode: at index {_index} there is not a number");
         end++; 
         _index++;
         c = (char)bencode[_index];
      }
      
      // index out of bounds, it misses the e for closing the integer 
      if (_index == bencode.Length) throw new FormatException("Invalid bencode: the dictionary is not closed");

      var numStr = Encoding.ASCII.GetString(bencode, start, end - start);
      var num = long.Parse(numStr);
      
      // skip e
      _index++;

      return num;
   }
   
   private byte[] HandleNonUtf8String(byte[] bencode)
   {
      var start = _index;
      var end = _index;
      var c = (char) (bencode[end]);
      
      while (char.IsDigit(c))
      {
         _index++;
         end++;
         c = (char)bencode[end];
      }
      
      // if start == _index then it means a lenght has not been extracted because there were no numeric chars
      if (start == _index) throw new FormatException($"Invalid bencode: string at index {_index} miss length");

      var length= int.Parse(new ReadOnlySpan<byte>(bencode, start: start, length: end - start));
      if (length == 0) return [];
      
      if (_index + length >= bencode.Length)
         throw new FormatException($"Invalid bencode: current index: {_index}" +
                                   $"+ string length {length} exceed bencode length");

      if (bencode[_index] != ':') throw new FormatException("Invalid bencode: string miss : at index " + _index);
      // skip :
      _index++;

      var content = new ReadOnlySpan<byte>(bencode, _index, length);
      _index += length;
      
      return content.ToArray();
   }

}
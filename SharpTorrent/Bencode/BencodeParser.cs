using System.Text;

namespace SharpTorrent.Bencode;

public class BencodeParser
{

   // index to traverse the bencode
   private int _index  = 0;
   
   public object ParseBencode(string bencode)
   {
      if (string.IsNullOrEmpty(bencode)) throw new FormatException($"Invalid bencode: Is empty");
      var parsedValue = ParseValue(bencode);
      return parsedValue switch
      {
         Dictionary<string, object> dictionary => dictionary,
         string stringValue => stringValue,
         int intValue => intValue,
         List<object> list => list,
         _ => throw new FormatException("Invalid bencode: impossible to parse the bencode")
      };
   }

   private object ParseValue(string bencode)
   {
      var c = bencode[_index];
      
      return c switch
      {
         'd' => HandleDictionary(bencode),
         'i' => HandleInteger(bencode),
         'l' => HandleList(bencode),
         _ => HandleString(bencode)
      };
   }

   private Dictionary<string, object> HandleDictionary(string bencode)
   {
      // skip d
      _index++;

      
      char c;
      var toReturn = new Dictionary<string, object>();

      try
      {
         do
         {
            var key = HandleString(bencode);
            var value = ParseValue(bencode);
            toReturn.Add(key, value);
            c = bencode[_index];
         } while (c != 'e');
      }
      catch (IndexOutOfRangeException)
      {
         throw new FormatException("Invalid bencode: the dictionary is not closed");
      }

      // skip e
      _index++;
      
      return toReturn;
   }
   
   
   private List<object> HandleList(string bencode)
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
         c = bencode[_index];
      } while (c != 'e');
      
      // skip e
      _index++;
      
      return toReturn;
   }

   private string HandleString(string bencode)
   {
      var sb = new StringBuilder();
      var c = bencode[_index];

      while (char.IsDigit(c))
      {
         sb.Append(c);
         _index++;
         c = bencode[_index];
      }

      var lenghtString = sb.ToString();
      if (string.IsNullOrEmpty(lenghtString)) throw new FormatException($"Invalid bencode: string at index {_index} miss length");

      var length = int.Parse(lenghtString);

      if (_index + length >= bencode.Length)
         throw new FormatException($"Invalid bencode: current index: {_index}" +
                                   $"+ string length {length} exceed bencode length");
      // skip :
      _index++;
      
      var result = bencode.Substring(_index, length);
      _index += length;
      
      return result;
   }

   private int HandleInteger(string bencode)
   {
      var sb = new StringBuilder();
      // skip i
      _index++;
      var c = bencode[_index];

      while (c != 'e')
      {
         if (!char.IsDigit(c)) throw new FormatException($"Invalid bencode: at index {_index} there is not a number");
         sb.Append(c);
         
         _index++;
         c = bencode[_index];
         // index out of bounds, it miss the e for closing the integer 
         if (_index == bencode.Length) throw new FormatException("Invalid bencode: the dictionary is not closed");
      }
      
      // bencoded integer can't start with a 0 unless they are exacltly zero, so i04e is illegal
      if (sb.ToString()[0] == '0') throw new FormatException("Invalid bencode: the number at index {_index} start with a 0");
      
      // skip e
      _index++;
      
      return int.Parse(sb.ToString());
   }
}
using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.Bencode;
using Xunit;

namespace SharpTorrent.Tests.Bencode;

[TestSubject(typeof(BencodeParser))]
public class BencodeParserTest
{
    private BencodeParser _bencodeParser;
     
    [Fact]
    public void BencodeParser_ParseBencode_ReturnListOfObject()
    {
        // given
        _bencodeParser = new BencodeParser();
        const string bencodeInput = "l4:spami3e6:piecesli45e3:abcee";
        // when
        var act = _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(bencodeInput));
        
        List<object> expected = [
             "spam", (long) 3, 
             "pieces", new List<object> { (long)45, "abc" } 
        ];
        
        // then
        act.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void BencodeParser_ParseBencode_ReturnDictOfObject()
    {
        // given
        _bencodeParser = new BencodeParser();
        
        // more complex bencode
        const string input = "d6:lengthi12345e4:name8:file.txt" +
                    "4:infod12:piece lengthi512e" +
                    "6:pieces12:abcdef123456ee";
        
        
        var expected = new Dictionary<string, object>
        {
            { "length", (long)12345},
            { "name", "file.txt" },
            { "info", new Dictionary<string, object>
                {
                    { "piece length", (long)512},
                    { "pieces", Encoding.UTF8.GetBytes("abcdef123456") }
                }
            }
        };
        
        // when
        var act = _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(input));
        
        // then 
        act.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void BencodeParser_ParseBencodeWithNonNumericInteger_ShouldThrowFormatException()
    {
        // given 
        _bencodeParser = new BencodeParser();
        const string input = "iabe";
        // when
        var act = () => _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(input));
        
        // then
        act.Should().Throw<FormatException>().WithMessage("Invalid bencode: *");
    }

    [Fact]
    public void BencodeParser_ParseBencodeWithMalformedDictionary_ShouldThrowFormatException()
    {
        // given
        _bencodeParser = new BencodeParser();
        const string input = "d6:lengthi12345e4:name";
        // when
        var act = () => _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(input));
    
        // then
        act.Should().Throw<FormatException>().WithMessage("Invalid bencode: *");
    }
}
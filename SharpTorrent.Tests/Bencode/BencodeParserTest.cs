using System;
using System.Collections.Generic;
using System.Numerics;
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
        _bencodeParser = new BencodeParser();
        // simple bencode
        const string input = "l4:spami3e6:piecesli45e3:abcee";
        var act = _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(input));
        
        List<object> expected = [
             "spam", BigInteger.Parse("3"), 
             "pieces", new List<object> { BigInteger.Parse("45"), "abc" } 
        ];

        act.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void BencodeParser_ParseBencode_ReturnDictOfObject()
    {
        _bencodeParser = new BencodeParser();
        
        // more complex bencode
        const string input = "d6:lengthi12345e4:name8:file.txt" +
                    "4:infod12:piece lengthi512e" +
                    "6:pieces12:abcdef123456ee";
        
        var act = _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(input));
        
        var expected = new Dictionary<string, object>
        {
            { "length", BigInteger.Parse("12345")},
            { "name", "file.txt" },
            { "info", new Dictionary<string, object>
                {
                    { "piece length", BigInteger.Parse("512") },
                    { "pieces", "abcdef123456" }
                }
            }
        };
        
        act.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ParseBencode_WithNonNumericInteger_ShouldThrowFormatException()
    {
        _bencodeParser = new BencodeParser();
        const string input = "iabe";
        var act = () => _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(input));

        act.Should().Throw<FormatException>().WithMessage("Invalid bencode: *");
    }

    [Fact]
    public void ParseBencode_WithMalformedDictionary_ShouldThrowFormatException()
    {
        _bencodeParser = new BencodeParser();
        const string input = "d6:lengthi12345e4:name";
        var act = () => _bencodeParser.ParseBencode(Encoding.UTF8.GetBytes(input));

        act.Should().Throw<FormatException>().WithMessage("Invalid bencode: *");
    }
}
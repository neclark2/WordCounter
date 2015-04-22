
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;

/*
 *  Did this for an Evernote Round 1 interview  
 *  
 * 
Write a function that takes two parameters: 
(1) a String representing the contents of a text document 
(2) an integer providing the number of items to return

- Implement the function such that it returns a list of Strings ordered by word frequency, the most frequently occurring word first. 
- Use your best judgement to decide how words are separated. 
- Implement this function as you would for a production / commercial system 
- You may use any standard data structures.

Performance Constraints: 
- Your solution should run in O(n) time where n is the number of characters in the document. 
- Please provide reasoning on how the solution obeys the O(n) constraint.
 */

public interface IKeyable
{
    int Key { get; set; }
}

public class WordCountPair : IKeyable
{
    public int Key { get; set; }
    public string Word { get; set; }
}

public class WordCounter
{

    public IEnumerable<string> IdentifyCommonWords(string documentText, int numWords)
    {
        if (numWords <= 0)
        {
            throw new ArgumentException("numWords must be a positive integer");
        }

        if (String.IsNullOrEmpty(documentText))
        {
            return new List<string>();
        }

        var words = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        var wordIdx = 0;
        var insideWord = false;
        var totalWords = 0;

        for (var i = 0; i < documentText.Length; i++)
        {
            var currChar = documentText[i];
            var isValidStartEndChar = IsValidWordStartOrEndCharacter(currChar) && !IsSingleCharacterWord(currChar);
            var isValidMiddleChar = IsValidWordMiddleCharacter(currChar) && !IsSingleCharacterWord(currChar);
            if (!insideWord && isValidStartEndChar)
            {
                insideWord = true;
                wordIdx = i;
            }
            else if (insideWord && !isValidMiddleChar)
            {
                var hasValidEndChar = IsValidWordStartOrEndCharacter(documentText[i-1]);
                var wordLength = hasValidEndChar ? i - wordIdx : i - wordIdx - 1;
                var word = documentText.Substring(wordIdx, wordLength);
                insideWord = false;
                AddWord(words, word);
                totalWords++;
            }
            if (IsSingleCharacterWord(currChar))
            {
                AddWord(words, currChar.ToString());
                totalWords++;
            }
        }

        //look for word at the end of the string
        if (insideWord)
        {
            var hasValidEndChar = IsValidWordStartOrEndCharacter(documentText[documentText.Length - 1]);
            var wordLength = (hasValidEndChar) ? documentText.Length - wordIdx : documentText.Length - wordIdx - 1;
            var word = documentText.Substring(wordIdx, wordLength);
            AddWord(words, word);
            totalWords++;
        }

        var counts = words.Select(w => new WordCountPair { Key = w.Value, Word = w.Key }).ToList();
        return CountingSort(counts, totalWords).
                Reverse().
                Take(numWords).
                Select(s => s.Word);

    }

    private void AddWord(Dictionary<string, int> words, string word)
    {
        if (words.ContainsKey(word))
        {
            words[word] += 1;
        }
        else
        {
            words.Add(word, 1);
        }
    }

    private bool IsSingleCharacterWord(char c)
    {
        //Just treat all common Japanese/Chinese characters as single character words
        var isKanji = IsCharInRange(c, 0x4E00, 0x9FBF);
        var isCJKUnified = IsCharInRange(c, 0x4E00, 0x9FFF);

        return IsValidWordStartOrEndCharacter(c) && (isKanji || isCJKUnified);
    }

    private bool IsCharInRange(char c, int min, int max)
    {
        return c >= min && c <= max;
    }

    private bool IsValidWordStartOrEndCharacter(char c)
    {
        //considering anything in the letter Unicode category to be a valid character for a word
        return new[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.OtherLetter }.
                      Contains(char.GetUnicodeCategory(c));
    }

    private bool IsValidWordMiddleCharacter(char c)
    {
        //Allow apostrophes and hyphens to appear in the middle of words, to support possesive, hyphenated, and contracted words
        return IsValidWordStartOrEndCharacter(c) || c.Equals('\'') || c.Equals('-');
    }


    private IList<T> CountingSort<T>(IList<T> input, int totalNumItems)
        where T : IKeyable
    {
        //Counting Sort can sort an array of N integers of size K in O(N+K) time
        
        //build count array as a histogram, recording frequency of each key in the input
        var count = new int[totalNumItems];
        for (var i = 0; i < input.Count; i++)
        {
            var val = input[i];
            count[val.Key] = count[val.Key] + 1;
        }

        //overwrite count array to store the first index each value must appear in the final output
        var total = 0;
        for (var i = 0; i < totalNumItems; i++)
        {
            var oldCount = count[i];
            count[i] = total;
            total += oldCount;
        }

        //populate the final output array
        var output = new T[input.Count];
        for (var i = 0; i < input.Count; i++)
        {
            var j = input[i];
            var currCount = count[j.Key];
            output[currCount] = j;
            count[j.Key] = currCount + 1;
        }
        return output;
    }
}


[TestFixture]
public class WordCounterTests
{

    private WordCounter counter = new WordCounter();

    [Test]
    public void SimpleEnglishWordsWithSpaces()
    {
        var words = counter.IdentifyCommonWords(@"this is a basic text is a basic text a basic text text", 6);
        CollectionAssert.AreEqual(new[]{"text", "basic", "a", "is", "this"}, words.ToArray());
    }

    [Test]
    public void WordsWithVariousPunctuationAndModifiers()
    {
        var words = counter.IdentifyCommonWords(@"cul-de-säc?..Maître d'hôtel childrens' Maître child's¾something'", 10);
        CollectionAssert.AreEqual(new[] { "Maître", "something", "child's", "childrens", "d'hôtel", "cul-de-säc" }, words.ToArray());
    }

    [Test]
    public void EastAsianCharacters()
    {
        //just random text I got from the japanese wikipedia page
        var words = counter.IdentifyCommonWords(@"蜂蜜とはミツバチが花の蜜を採集し、巣の中で加工、貯蔵したものをいう。自然界で最も甘い蜜といわれ、本来はミツバチの食料であるが、しばしば他の生物が採集して食料としている。約8割の糖分と約2割の水分によって構成され、ビタミン、ミネラルなど微量の有効成分を含む。", 3);
        CollectionAssert.AreEqual(new[] { "の", "分", "蜜"}, words.ToArray());
    }

    [Test]
    public void NegativeSize()
    {
        Assert.Throws<ArgumentException>(() => counter.IdentifyCommonWords(@"this is a basic text is a basic text a basic text text", -1));
    }


    [Test]
    public void NullOrEmptyInput()
    {
        var words = counter.IdentifyCommonWords(@"", 6);
        Assert.IsEmpty(words);

        words = counter.IdentifyCommonWords(null, 6);
        Assert.IsEmpty(words);
    }

    [Test]
    public void Top10WordsFromTheEpicOfGilgamesh()
    {
        //just for fun :)

        var req = WebRequest.Create("http://www.gutenberg.org/cache/epub/11000/pg11000.txt");
        var resp = req.GetResponse();
        var text = "";
        using (var reader = new StreamReader(resp.GetResponseStream()))
        {
            text = reader.ReadToEnd();
        }

        var words = counter.IdentifyCommonWords(text, 10);
        CollectionAssert.AreEqual(new[] { "The", "of", "to", "in", "and", "a", "is", "as", "with", "Enkidu" }, words.ToArray());
    }

    [Test]
    public void Top5WordsFromSocratesApology()
    {
        //more fun

        var req = WebRequest.Create("https://www.gutenberg.org/files/39462/39462-0.txt");
        var resp = req.GetResponse();
        var text = "";
        using (var reader = new StreamReader(resp.GetResponseStream()))
        {
            text = reader.ReadToEnd();
        }

        
        var words = counter.IdentifyCommonWords(text, 5);
        CollectionAssert.AreEqual(new[] { "και", "να", "εις", "του", "την" }, words.ToArray());
    }

}

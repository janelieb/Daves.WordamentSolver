﻿// This is my first C# program... I was just here to learn... Be careful...

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

// Not using a trie, just a List<List<string>> where List<string>s share a common 3-letter prefix.
// Gotten from a pre-generated word list grouped in this way.

// See if the prefix exists; if it doesn't, then disregard the entire group.
// But that only works for a board full of BasicTiles.
// Digrams, suffixes, and prefixes > 3 in length screw this optimization up. 
// (N,E,AR but not A on board => NEAR wouldn't be found as NEA prefix group is thrown out.)
// BUT: Only 1 special tile exists per board.
//      Assume prefixes and suffixes <= 3 in length.
//      Digrams are length 2. 
//      Only have to worry about suffixes of length 2 and 3.
// Given all that, only have to worry if last letter of prefix equals first letter of digram/suffix
// or if last two letters of prefix equals first two letters of suffix.
// In this case, go ahead and check the entire group.

// In the future it might be easier just to remove this optimization entirely; depends on new tile types.

namespace WordamentRecursiveOOSolver
{
  public class Solver
  {
    #region Some variables I'm using
    public static bool digram = false;
    private static string specialTileValue = "empty";
    private static string dictionaryName = "3PrefixedTWL.txt";
    private const int dim = 4;
    // Let's not work with ASCII values.
    private static Dictionary<char, int> alphabet = new Dictionary<char, int>
                                               {{'A', 1}, {'B', 2}, {'C', 3}, {'D', 4}, {'E', 5}, {'F', 6}, {'G', 7}, {'H', 8}, {'I', 9}, 
                                               {'J', 10}, {'K', 11}, {'L', 12}, {'M', 13}, {'N', 14}, {'O', 15}, {'P', 16}, {'Q', 17}, 
                                               {'R', 18}, {'S', 19}, {'T', 20}, {'U', 21}, {'V', 22}, {'W', 23}, {'X', 24}, {'Y', 25}, {'Z', 26}};
    // First line on which the ith letter begins a word.
    //For TWL
    private static int[] firstLine = {0, 1, 278, 420, 552, 687, 883, 997, 1118, 1235, 1337, 1424, 1545, 1663, 1785, 1892, 2080, 2238, 2254, 2367, 2560, 2702, 2808, 2875, 2969, 2977, 3043, 3106};
    // For SOWPODS 
    //private static int[] firstLine = {0, 1, 315, 467, 608, 756, 997, 1119, 1254, 1382, 1499, 1597, 1735, 1863, 2007, 2125, 2351, 2520, 2543, 2666, 2881, 3039, 3170, 3251, 3359, 3369, 3483};

    #endregion

    public static List<List<string>> RunSolver(string[] stringBoard, List<List<List<int>>> paths)
    {
      Tile[,] board = new Tile[dim, dim];
      List<char> uniqueLetters = ReadBoard(board, stringBoard);
      List<List<string>> prefixDict = ReadPrefixDictionary(dictionaryName);
      List<List<string>> wordsOnBoard = new List<List<string>>();

      // Building up wordsOnBoard with a (set of words beginning with the same letter) at a time.
      for (int i = 0; i < uniqueLetters.Count(); ++i)
      {
        List<List<int>> pathsForLetter = new List<List<int>>();
        wordsOnBoard.Add(SearchBoard(board, prefixDict, uniqueLetters[i], pathsForLetter));
        paths.Add(pathsForLetter);
      }
      return wordsOnBoard;
    }

    // READ BOARD AND RETURN UNIQUE LETTERS
    static List<char> ReadBoard(Tile[,] board, string[] stringBoard)
    {
      string[] tileValues = stringBoard;
      List<char> allLetters = new List<char>();
      for (int i = 0; i < tileValues.Count(); ++i)
      {
        if (Char.IsLetter(tileValues[i][0]))
        {
          allLetters.Add(tileValues[i][0]);
        }
        // Suffix still has to be included I think, as -END would => END is a valid word?
        if (tileValues[i][0] == '-')
        {
          allLetters.Add(tileValues[i][1]);
        }
        // Either/Or
        if (tileValues[i].Count() == 3 && (tileValues[i][1] == '\\' || tileValues[i][1] == '/'))
        {
          allLetters.Add(tileValues[i][2]);
        }
      }
      for (int i = 0; i < dim; ++i)
      {
        for (int j = 0; j < dim; ++j)
        {
          // BasicTile
          int k = i * dim + j;
          if (tileValues[k].Count() == 1)
          {
            board[i, j] = new BasicTile(i, j, tileValues[k]);
          }
          // PrefixTile
          else if (tileValues[k][tileValues[k].Count() - 1] == '-')
          {
            board[i, j] = new PrefixTile(i, j, tileValues[k]);
          }
          // SuffixTile
          else if (tileValues[k][0] == '-')
          {
            if (tileValues[k].Count() >= 3)
            {
              specialTileValue = tileValues[k];
            }
            board[i, j] = new SuffixTile(i, j, tileValues[k]);
          }
          // DigramTile
          else if (tileValues[k].Count() == 2)
          {
            digram = true;
            specialTileValue = tileValues[k];
            board[i, j] = new DigramTile(i, j, tileValues[k]);
          }
          // EitherOrTile (Either/Or)
          else if (tileValues[k][1] == '/' || tileValues[k][1] == '\\')
          {
            board[i, j] = new EitherOrTile(i, j, tileValues[k]);
          }
        }
      }
      // Find unique letters
      allLetters.Sort();
      List<char> uniqueLetters = new List<char>();
      uniqueLetters.Add(allLetters[0]);
      for (int i = 1; i < allLetters.Count(); ++i)
      {
        if (allLetters[i] != uniqueLetters[uniqueLetters.Count() - 1])
        {
          uniqueLetters.Add(allLetters[i]);
        }
      }
      return uniqueLetters;
    }

    // READ PREFIX DICTIONARY
    static List<List<string>> ReadPrefixDictionary(string prefixDictionaryFile)
    {
      List<List<string>> prefixDict = new List<List<string>>();
      using (StreamReader prefixDictReader = new StreamReader(prefixDictionaryFile))
      {
        Regex r = new Regex(@"\s+");
        string line;
        int counter = 0;
        while ((line = prefixDictReader.ReadLine()) != null)
        {
          prefixDict.Add(new List<string>());
          line.Trim();
          string[] words = r.Split(line);
          foreach (string s in words)
          {
            prefixDict[counter].Add(s);
          }
          ++counter;
        }
      }
      return prefixDict;
    }

    // SEARCH BOARD FOR WORDS BEGINNING WITH firstLetter
    static List<string> SearchBoard(Tile[,] board, List<List<string>> prefixDict, char firstLetter, List<List<int>> paths)
    {
      List<string> wordsFound = new List<string>();
      int startIndex = firstLine[alphabet[firstLetter]] - 1;
      int endIndex = firstLine[alphabet[firstLetter] + 1] - 1;
      // Search for words from [startIndex, endIndex) within prefixDict
      for (int i = startIndex; i < endIndex; ++i)
      {
        SearchBoardForLine(board, prefixDict[i], wordsFound, paths);
      }
      return wordsFound;
    }

    // SEARCH BOARD FOR THE LINE prefixDictLine
    static void SearchBoardForLine(Tile[,] board, List<string> prefixDictLine, List<string> wordsFound, List<List<int>> paths)
    {
      // Capitalize the word as board is in all caps, and some prefixes have lowercase beginning.
      string word = prefixDictLine[0].ToUpper();
      List<int> path = new List<int>();
      // If line's prefix isn't there, return; nothing from the line can be there.
      if (!SearchBoardForWord(board, word, path))
      {
        #region Annoying checks for digram/suffix tiles
        if (specialTileValue == "empty")
        {
          return;
        }
        if (digram == true)
        {
          if (specialTileValue[0] != word[word.Count() - 1])
          {
            return;
          }
        }
        else
        {
          bool ret = true;
          if (specialTileValue.Count() == 3)
          {
            if (specialTileValue[1] == word[word.Count() - 1])
            {
              ret = false;
            }
          }
          if (specialTileValue.Count() == 4)
          {
            if (specialTileValue[1] == word[word.Count() - 1])
            {
              ret = false;
            }
            if (specialTileValue[1] == word[word.Count() - 2] && specialTileValue[2] == word[word.Count() - 1])
            {
              ret = false;
            }
          }
          if (ret)
          {
            return;
          }
        }
        #endregion
      }
      else if (Char.ToLower(prefixDictLine[0][0]) == prefixDictLine[0][0])
      {
        // The prefix itself is a word; add it.
        wordsFound.Add(word);
        paths.Add(path);
      }
      // Continue through the rest of the list
      for (int i = 1; i < prefixDictLine.Count(); ++i)
      {
        word = (prefixDictLine[0] + prefixDictLine[i]).ToUpper();
        path = new List<int>();
        if (SearchBoardForWord(board, word, path))
        {
          wordsFound.Add(word);
          paths.Add(path);
        }
      }
    }

    // SETTING UP THE RECURSION
    static bool SearchBoardForWord(Tile[,] board, string word, List<int> path)
    {
      for (int i = 0; i < dim; ++i)
      {
        for (int j = 0; j < dim; ++j)
        {
          int k = board[i, j].ValidContinuation(0, word);
          if (k != 0)
          {
            if (SearchBoardForWordRecursively(board, i, j, k, word, path))
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    // RECURSING
    static bool SearchBoardForWordRecursively(Tile[,] board, int i, int j, int length, string word, List<int> path)
    {
      board[i, j].Take();
      bool returnValue = false;
      // Base case
      if (length == word.Count())
      {
        returnValue = true;
      }
      else
      {
        for (int k = i - 1; k <= i + 1 && !returnValue; ++k)
        {
          for (int l = j - 1; l <= j + 1 && !returnValue; ++l)
          {
            // Don't have to worry about case where k == i, l == j, as ValidContinuation will return false.
            // Some premature optimization here; test ValidContinuation before recursing.
            int m = ValidateContinuation(board, k, l, length, word);
            if (m != 0)
            {
              returnValue = SearchBoardForWordRecursively(board, k, l, length + m, word, path);
            }
          }
        }
      }
      board[i, j].Return();
      if (returnValue)
      {
        path.Add(i * dim + j);
      }
      return returnValue;
    }

    // RECURSION HELPER
    static int ValidateContinuation(Tile[,] board, int i, int j, int index, string word)
    {
      // Words are guaranteed to be all uppercase at this point, so the second () group works.
      if (i < dim && i >= 0 && j < dim && j >= 0)
      {
        return board[i, j].ValidContinuation(index, word);
      }
      return 0;
    }

    // TILE CLASSES
    public abstract class Tile
    {
      protected string value;
      protected int i;
      protected int j;
      protected bool taken;
      protected Tile(int i, int j, string value)
      {
        this.i = i;
        this.j = j;
        this.value = value;
        taken = false;
      }
      public void Take()
      {
        taken = true;
      }
      public void Return()
      {
        taken = false;
      }
      // Returns how many characters of the word it consumes. Index is current index in word.
      abstract public int ValidContinuation(int index, string word);
    }
    public class BasicTile : Tile
    {
      public BasicTile(int i, int j, string value)
        : base(i, j, value)
      {
      }
      public override int ValidContinuation(int index, string word)
      {
        if (taken == false && value[0] == word[index])
        {
          return 1;
        }
        return 0;
      }
    }
    public class PrefixTile : Tile
    {
      public PrefixTile(int i, int j, string value)
        : base(i, j, value)
      {
      }
      public override int ValidContinuation(int index, string word)
      {
        if (taken == false && index == 0 && (value.Count() - 1) <= word.Count())
        {
          for (int i = 0; i < value.Count() - 1; ++i)
          {
            if (value[i] != word[i])
            {
              return 0;
            }
          }
          return value.Count() - 1;
        }
        return 0;
      }
    }
    public class SuffixTile : Tile
    {
      public SuffixTile(int i, int j, string value)
        : base(i, j, value)
      {
      }
      public override int ValidContinuation(int index, string word)
      {
        if (taken == false && (value.Count() - 1) == (word.Count() - index))
        {
          for (int i = 1; i < value.Count(); ++i)
          {
            if (value[i] != word[i + index - 1])
            {
              return 0;
            }
          }
          return value.Count() - 1;
        }
        return 0;
      }
    }
    public class DigramTile : Tile
    {
      public DigramTile(int i, int j, string value)
        : base(i, j, value)
      {
      }
      public override int ValidContinuation(int index, string word)
      {
        if (taken == false && value.Count() <= (word.Count() - index))
        {
          for (int i = 0; i < value.Count(); ++i)
          {
            if (value[i] != word[i + index])
            {
              return 0;
            }
          }
          return value.Count();
        }
        return 0;
      }
    }
    public class EitherOrTile : Tile
    {
      public EitherOrTile(int i, int j, string value)
        : base(i, j, value)
      {
      }
      public override int ValidContinuation(int index, string word)
      {
        if (taken == false && (value[0] == word[index] || value[2] == word[index]))
        {
          return 1;
        }
        return 0;
      }
    }
  }
}

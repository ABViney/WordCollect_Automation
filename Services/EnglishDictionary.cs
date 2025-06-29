using System.Text;
using Microsoft.Data.Sqlite;

namespace WordCollect_Automated.Services;

public class EnglishDictionary
{
    public static List<string> GetPotentialWords(List<string> availableCharacters, int minimumLength = 3)
    {
        // Count how many of each character is available
        var characterCounts = new Dictionary<char, int>();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            characterCounts[c] = availableCharacters.Count(character => c.ToString().Equals(character.ToUpper()));
        }
        
        // Build SQLite query
        StringBuilder query = new StringBuilder();
        query.Append("SELECT DISTINCT word from entries e\n");
        query.Append($"WHERE LENGTH(word) >= {minimumLength} AND LENGTH(word) <= {availableCharacters.Count}\n");
        
        // Inverting available characters
        foreach (char c in characterCounts.Keys)
        {
            if (characterCounts[c] == 0) // If character was NOT provided by caller then exclude records that contain it
            {
                query.Append($"AND word NOT LIKE '%{c.ToString()}%'\n");
            }
        }
        query.Append("AND word NOT GLOB '*[0-9]*'"); // No dashes and no numbers in results
        
        // Connect to database
        using var connection = new SqliteConnection($"Data Source={Path.ToAssets}/Dictionary.db");
        connection.Open();

        // Issue query
        using var command = connection.CreateCommand();
        command.CommandText = query.ToString();

        List<string> potentialWords = new();
        
        // Check results for validity
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string potentialWord = reader.GetString(0).ToUpper(); // Column index 0: "word", normalize value
            bool isValidWord = true;
            foreach (char c in potentialWord)
            {
                int charCount = potentialWord.Count(character => character == c);
                if (!characterCounts.ContainsKey(c) || charCount > characterCounts[c])
                {
                    isValidWord = false;
                    break;
                }
            }

            if (isValidWord)
            {
                potentialWords.Add(potentialWord);
            }
        }

        potentialWords.Sort((a, b) => a.Length.CompareTo(b.Length));
        return potentialWords;
    }
}
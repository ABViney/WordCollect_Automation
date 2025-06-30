using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services.WordCollect;

public class SolvableWordParser
{
    private IdentifiedCharacterPool _knownSolvedLetters = new();

    private const string _knownSolvedCharacterPrefix = "Solved_";

    public SolvableWordParser()
    {
        // Load known characters
        string[] characterFiles = Directory.GetFiles(Path.CharacterImageRepository);
        var knownSolvedLetterFiles = characterFiles.Where(filepath => System.IO.Path.GetFileName(filepath).StartsWith(_knownSolvedCharacterPrefix));

        // Populate solved letters
        foreach (string knownSolvedLetterFile in knownSolvedLetterFiles)
        {
            string letter = System.IO.Path.GetFileName(knownSolvedLetterFile).Substring(_knownSolvedCharacterPrefix.Length, 1);
            _knownSolvedLetters.Add(letter, knownSolvedLetterFile);
        }
    }

    /// <summary>
    /// Instantiates a new solvable word pool instance for the current pool of solvable words.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void SetUpSolvedWordsPool()
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Compares the current solved words against the solvable word pool and updates listings accordingly
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void UpdateSolvedWords()
    {
        throw new NotImplementedException();
    }
}
using WordCollect_Automated.Services;

namespace WordCollect_Automated.Models;

/// <summary>
/// Collection of the available letters and their positioning data that can be used to create a word.
/// </summary>
public class SelectableLetterPool
{
    public List<SelectableLetter> SelectableLetters { get; }
    public BoundingBox Area { get; }
    public List<string> PotentialWords { get; }

    public SelectableLetterPool(List<SelectableLetter> selectableLetters)
    {
        SelectableLetters = selectableLetters;
        List<string> letters = selectableLetters.Select(sl => sl.Letter).ToList();
        PotentialWords = EnglishDictionary.GetPotentialWords(letters);
        Area = BoundingBox.ThatEncapsulates(SelectableLetters.Select(sl => sl.BoundingBox));
    }

    public List<SelectableLetter> BuildWord(string word)
    {
        if (!PotentialWords.Contains(word))
        {
            throw new ArgumentException($"{word} is not in {nameof(PotentialWords)}");
        }
        
        var used = new HashSet<SelectableLetter>(); // stores indices of used SelectableLetters
        var result = new List<SelectableLetter>();

        foreach (char c in word)
        {
            SelectableLetter? selectableLetter = SelectableLetters.Find(sl =>
                sl.Letter.Equals(c.ToString(), StringComparison.OrdinalIgnoreCase) && !used.Contains(sl));

            if (selectableLetter is null)
                throw new ApplicationException(
                    $"Could not find another selectable instance of the letter {c.ToString()}");

            used.Add(selectableLetter);
            result.Add(selectableLetter);
        }

        if (result.Count == 0)
        {
            throw new ApplicationException(
                $"Fucked up spelling {word}. Available pool: {String.Join(',', SelectableLetters.Select(sl => sl.Letter))}");
        }
        return result;
    }
}
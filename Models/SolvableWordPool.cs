namespace WordCollect_Automated.Models;

public class SolvableWordPool
{
    private List<string> _solvedWords;
    private List<List<BoundingBox>> _unsolvedWordPositions;
    private List<List<IdentifiedCharacter>> _identifiedCharacterGroups;

    public IReadOnlyList<IReadOnlyList<BoundingBox>> UnsolvedWordPositions => _unsolvedWordPositions;

    public IEnumerable<IEnumerable<IdentifiedCharacter>> IdentifiedCharacterGroups =>
        _identifiedCharacterGroups.Select(inner => inner.Select(ic => ic));
    public IEnumerable<string> SolvedWords => _solvedWords;
    
    public int MinimumWordLength { get; }
    public int MaximumWordLength { get; }

    public SolvableWordPool(List<List<BoundingBox>> unsolvedWordPositions)
    {
        _unsolvedWordPositions = unsolvedWordPositions; // plz don't modify this list after passing kthx
        _solvedWords = new();
        _identifiedCharacterGroups = new();

        MinimumWordLength = _unsolvedWordPositions.Min(uwp => uwp.Count);
        MaximumWordLength = _unsolvedWordPositions.Max(uwp => uwp.Count);
    }

    public void AddSolvedWord(List<IdentifiedCharacter> identifiedCharacters)
    {
        string word = String.Join("", identifiedCharacters.Select(ic => ic.Character));
        if (HasWord(word))
        {
            throw new ArgumentException($"{word} is already solved.");
        }
        _solvedWords.Add(word);
        
        // Meta af, but the bounding box of an identified character should be provided from the bounding box of an 
        // unsolved word's position.
        var unsolvedLetters = 
            _unsolvedWordPositions.FirstOrDefault(uwp => uwp[0] == identifiedCharacters[0].BoundingBox);

        // Make sure this word belongs to this pool. Odds are if it's in the same position and same length it does.
        if (unsolvedLetters is null || identifiedCharacters.Count != unsolvedLetters.Count)
        {
            throw new ArgumentException($"{nameof(identifiedCharacters)} does not belong to this {nameof(SolvableWordPool)}");
        }

        // Update properties
        _unsolvedWordPositions.Remove(unsolvedLetters);
        _identifiedCharacterGroups.Add(identifiedCharacters.Slice(0, identifiedCharacters.Count)); // Shallow copy so it can't get modified by the caller
    }
    
    public bool HasWord(string word)
    {
        return _solvedWords.Contains(word);
    }
    
}
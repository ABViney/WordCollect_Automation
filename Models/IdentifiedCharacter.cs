namespace WordCollect_Automated.Models;

/// <summary>
/// Represents an identified character.
/// </summary>
public class IdentifiedCharacter
{
    /// <summary>
    /// String representation of this character
    /// </summary>
    public string Character { get; }
    
    
    /// <summary>
    /// Where in the image this letter was found
    /// </summary>
    public BoundingBox BoundingBox { get; }

    public IdentifiedCharacter(string character, BoundingBox boundingBox)
    {
        Character = character;
        BoundingBox = boundingBox;
    }
}
namespace WordCollect_Automated.Models;

public class SelectableLetter : IdentifiedCharacter
{
    /// <summary>
    /// The character this letter represents
    /// </summary>
    public string Letter => Character;

    public SelectableLetter(string letter, BoundingBox boundingBox)
    :base(letter, boundingBox)
    {
    }
}
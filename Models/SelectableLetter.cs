namespace WordCollect_Automated.Models;

public class SelectableLetter
{
    /// <summary>
    /// The character this letter represents
    /// </summary>
    public string Letter { get; }
    
    /// <summary>
    /// Where in the image this letter was found
    /// </summary>
    public BoundingBox BoundingBox { get; }

    public SelectableLetter(string letter, BoundingBox boundingBox)
    {
        Letter = letter;
        BoundingBox = boundingBox;
    }
}
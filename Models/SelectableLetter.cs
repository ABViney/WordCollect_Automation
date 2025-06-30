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
    public BoundingBox Geometry { get; }

    public SelectableLetter(string letter, BoundingBox geometry)
    {
        Letter = letter;
        Geometry = geometry;
    }
}
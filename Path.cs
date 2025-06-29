using System.Reflection;

namespace WordCollect_Automated;

public static class Path
{
    public static string ToAssets =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "assets");
    
    public static string ToEnglishDictionaryDB => System.IO.Path.Combine(ToAssets, "EnglishDictionary.db");
}
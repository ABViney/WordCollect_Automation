namespace WordCollect_Automated.Services;

public interface ITemporaryDirectory : IDisposable
{
    string Path { get; }
    void EnsureExists();
}

public interface ITemporaryFile : IDisposable
{
    string Path { get; }
}

public static class TemporaryDataManager
{
    static TemporaryDataManager()
    {
        if (Directory.Exists(Path.ToTemporaryData))
        {
            Directory.Delete(Path.ToTemporaryData, true);
        }
        Directory.CreateDirectory(Path.ToTemporaryData);
    }

    public static ITemporaryDirectory CreateTemporaryDirectory() =>
        new TemporaryDirectory(System.IO.Path.Combine(Path.ToTemporaryData, System.IO.Path.GetRandomFileName()));
    
    public static ITemporaryFile CreateTemporaryFile() =>
            new TemporaryFile(System.IO.Path.Combine(Path.ToTemporaryData, System.IO.Path.GetRandomFileName()));

    public static ITemporaryFile CreateTemporaryPNGFile() =>
            new TemporaryFile(System.IO.Path.Combine(Path.ToTemporaryData, System.IO.Path.ChangeExtension(System.IO.Path.GetRandomFileName(), ".png")));
    
    public static void CleanUpTemporaryFiles()
    {
        if (Directory.Exists(Path.ToTemporaryData))
        {
            Directory.Delete(Path.ToTemporaryData);
        }
    }

    private class TemporaryDirectory : ITemporaryDirectory
    {
        public string Path { get; }
        
        public void EnsureExists() => Directory.CreateDirectory(Path);

        public TemporaryDirectory(string path)
        {
            Path = path;
        }
        
        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }

    private class TemporaryFile : ITemporaryFile
    {
        public string Path { get; }

        public TemporaryFile(string path)
        {
            Path = path;
        }
        
        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
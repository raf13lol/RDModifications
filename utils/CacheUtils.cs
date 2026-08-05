using System.IO;

namespace RDModifications;

public class CachePathUtils
{
    public static string BasePath = Path.Combine(Entry.UserDataFolder, "__rdmcache");

    static string CreatePathIfNeeded(string path)
    {
        // @FUCK (my old code was @FUCKed)
        if (Directory.Exists(path))
            Directory.Delete(path);
        // apparently creates needed directories without errors if they already exist
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        return path;
    }

    public static string GetPath(string modificationName, string subFolderName, string fileName)
        => CreatePathIfNeeded(Path.Combine(BasePath, modificationName, subFolderName, fileName));

    public static string GetPath(string modificationName, string fileName)
        => CreatePathIfNeeded(Path.Combine(BasePath, modificationName, fileName));

    public static string GetPath(string modificationName)
        => CreatePathIfNeeded(Path.Combine(BasePath, modificationName));
}
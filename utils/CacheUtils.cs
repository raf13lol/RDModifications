using System.IO;

namespace RDModifications;

public class CachePathUtils
{
    public static string BasePath = Path.Combine(Entry.UserDataFolder, "__rdmcache");

    static string CreatePathIfNeeded(string path)
    {
        // apparently creates needed directories without errors if they already exist
        Directory.CreateDirectory(path);
        return path;
    }

    public static string GetPath(string modificationName, string subFolderName, string fileName)
        => CreatePathIfNeeded(Path.Combine(BasePath, modificationName, subFolderName, fileName));

    public static string GetPath(string modificationName, string fileName)
        => CreatePathIfNeeded(Path.Combine(BasePath, modificationName, fileName));

    public static string GetPath(string modificationName)
        => CreatePathIfNeeded(Path.Combine(BasePath, modificationName));
}
using System.IO;
using System.Linq;

namespace RDModifications;

public class LevelUtils
{
    public static string GetLevelFolderName(CustomLevelData data)
    {
        if (data == null)
            return "";
        string path = data.path;
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            path = path[..^1];
        string[] directories = path.Split(Path.DirectorySeparatorChar);
        if (directories.Length <= 1)
            directories = path.Split(Path.AltDirectorySeparatorChar);

        return directories.Last().Replace(".rdzip", "");
    }
}
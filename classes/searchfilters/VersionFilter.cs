// Cache encodes incorrect information so this cannot work for now

namespace RDModifications.SearchFilters;

public class VersionFilter : SearchFilter
{
    public bool Inverted { get; set; } = false;

    public bool Enabled => true;
    public string[] Prefixes => ["version", "ver"];

    private int VersionToCheck;

    public bool Check(string version, out SearchFilter filterToUse)
    {
        if (!int.TryParse(version, out int filterVersion) || filterVersion <= 0)
        {
            filterToUse = null;
            return false;
        }

        Modification.Log.LogMessage(filterVersion);
        filterToUse = new VersionFilter()
        {
            VersionToCheck = filterVersion
        };
        return true;
    }

    public bool CheckLevel(CustomLevelData level)
        => level.settings.version == VersionToCheck;
}
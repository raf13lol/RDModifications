using System;

namespace RDModifications.SearchFilters;

public class PRStatusFilter : SearchFilter
{
    public bool Inverted { get; set; } = false;

    public bool Enabled => Modification.Enabled[typeof(LevelPRStatus)].Value;
    public string[] Prefixes => ["peerreview", "peer-review", "pr"];

    private PRStatus StatusToCheck;

    public bool Check(string status, out SearchFilter filterToUse)
    {
        if (!Enum.TryParse(status, true, out PRStatus filterStatus))
        {
            filterToUse = null;
            return false;
        }
        
        filterToUse = new PRStatusFilter()
        {
            StatusToCheck = filterStatus
        };
        return true;
    }

    public bool CheckLevel(CustomLevelData level)
        => LevelPRStatus.PRLevels.Get(level) == StatusToCheck;
}
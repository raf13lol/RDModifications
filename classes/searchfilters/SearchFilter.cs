using System.Collections.Generic;

namespace RDModifications.SearchFilters;

public interface SearchFilter
{
    public bool Inverted { get; set; }

    public bool Enabled { get; }
    public string[] Prefixes { get; }

    public bool Check(string text, out SearchFilter filterToUse);
    public bool CheckLevel(CustomLevelData level);

    public static List<SearchFilter> GetFilters()
    {
        return [
            new DifficultyFilter(),
            new PRStatusFilter(),
            new RankFilter(),
            new PlayersFilter(),
            // new VersionFilter()
        ];
    }
}
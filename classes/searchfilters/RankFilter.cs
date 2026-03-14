namespace RDModifications.SearchFilters;

public class RankFilter : SearchFilter
{
    public bool Inverted { get; set; } = false;

    public bool Enabled => true;
    public string[] Prefixes => ["rank"];

    private Rank RankToCheck;

    public bool Check(string rank, out SearchFilter filterToUse)
    {
        rank = rank switch
        {
            "nf" or "notfinished" => "NotFinished",
            "n/a" or "na" or "notavailable" => "NotAvailable",
            "ns" or "neverselected" => "NeverSelected",
            _ => rank.Replace("plus", "+").Replace("minus", "-").ToUpper(),
        };

        Rank filterRank = Rank.FromString(rank);
        if (filterRank == Rank.F && rank != "F")
        {
            filterToUse = null;
            return false;
        }
        
        filterToUse = new RankFilter()
        {
            RankToCheck = filterRank
        };
        return true;
    }

    public bool CheckLevel(CustomLevelData level)
        => Persistence.GetCustomLevelRank(level.Hash) == RankToCheck;
}
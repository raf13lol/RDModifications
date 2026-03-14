using System;

namespace RDModifications.SearchFilters;

public class DifficultyFilter : SearchFilter
{
    public bool Inverted { get; set; } = false;

    public bool Enabled => true;
    public string[] Prefixes => ["difficulty", "diff"];

    private LevelDifficulty DifficultyToCheck;

    public bool Check(string difficulty, out SearchFilter filterToUse)
    {
        switch (difficulty)
        {
            case "e": difficulty = LevelDifficulty.Easy.ToString(); break;
            case "m": difficulty = LevelDifficulty.Medium.ToString(); break;
            case "t": difficulty = LevelDifficulty.Tough.ToString(); break;
            case "vt": difficulty = LevelDifficulty.VeryTough.ToString(); break;
        }

        if (!Enum.TryParse(difficulty, true, out LevelDifficulty filterDifficulty))
        {
            filterToUse = null;
            return false;
        }
        
        filterToUse = new DifficultyFilter()
        {
            DifficultyToCheck = filterDifficulty
        };
        return true;
    }

    public bool CheckLevel(CustomLevelData level)
        => level.settings.difficulty == DifficultyToCheck;
}
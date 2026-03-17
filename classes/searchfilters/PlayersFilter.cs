using System;

namespace RDModifications.SearchFilters;

public class PlayersFilter : SearchFilter
{
    public bool Inverted { get; set; } = false;

    public bool Enabled => true;
    public string[] Prefixes => ["canbeplayedon", "players"];

    private LevelPlayMode PlayersToCheck;
    private bool AtLeast;

    public bool Check(string players, out SearchFilter filterToUse)
    {
        bool atLeast = players.Length > 0 ? players[0] == '+' : false;
        if (atLeast)
            players = players[1..];

        switch (players)
        {
            case "1p": players = LevelPlayMode.OnePlayerOnly.ToString(); break;
            case "2p": players = LevelPlayMode.TwoPlayerOnly.ToString(); break;
            case "1p2p":
            case "2p1p":
                players = LevelPlayMode.BothModes.ToString(); 
                atLeast = false;
                break;
        }

        if (!Enum.TryParse(players, true, out LevelPlayMode filterPlayers) || filterPlayers == LevelPlayMode.None)
        {
            filterToUse = null;
            return false;
        }

        filterToUse = new PlayersFilter()
        {
            PlayersToCheck = filterPlayers,
            AtLeast = atLeast
        };
        return true;
    }

    public bool CheckLevel(CustomLevelData level)
    {
        if (!AtLeast)
            return level.settings.canBePlayedOn == PlayersToCheck;
        return PlayersToCheck switch
        {
            LevelPlayMode.OnePlayerOnly => level.settings.canBePlayedOn != LevelPlayMode.TwoPlayerOnly,
            LevelPlayMode.TwoPlayerOnly => level.settings.canBePlayedOn != LevelPlayMode.OnePlayerOnly,
            _ => true
        };
    }
}
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using RDModifications.SearchFilters;

namespace RDModifications;

[Modification(
    "If there should be more ways to search in the CLS.\n" +
    "'rank=RANK' searches for a specific rank (e.g. NF means Not Finished and corresponds to unfinished levels).\n" +
    "'difficulty=DIFFICULTY' searches for a specific difficulty (e.g. VT means very tough).\n" +
    "'pr=STATUS' searches for a specific PR status if LevelPRStatus is enabled (e.g. PR means peer-reviewed levels).\n" +
    "'players=PLAYERS' searches for levels where you can play as only PLAYERS or if there's a '+' before, at least PLAYERS (e.g. 1p means 1 player only levels).\n" +
    "If any of these filters are prefixed with '!', it will invert the filter."
)]
public class ExtraSearchFilters : Modification
{
    [Configuration<string>("=", "What should separate the filter name from the filter value.")]
    public static ConfigEntry<string> SeparatorCharacters;

    [HarmonyPatch(typeof(scnCLS), nameof(scnCLS.SetSearchData))]
    public class FiltersPatch
    {
        public static List<SearchFilter> FiltersToApply = [];

        public static void Prefix(ref string textToSearch)
        {
            FiltersToApply.Clear();
            List<SearchFilter> allFilters = SearchFilter.GetFilters();

            string sepChar = SeparatorCharacters.Value.ToLower();
            List<string> potentialFilters = [.. textToSearch.Split(" ")];
            List<string> searchWords = [];

            List<SearchFilter> filtersToUse = [.. allFilters.Where(filter => filter.Enabled)];
            foreach (string filter in potentialFilters)
            {
                bool filtered = false;
                bool inverted = filter.Length > 0 && filter[0] == '!';
                string trimmedFilter = (inverted ? filter[1..] : filter).ToLower();

                foreach (SearchFilter filterToCheck in filtersToUse)
                {
                    string prefix = filterToCheck.Prefixes.FirstOrDefault(s => trimmedFilter.StartsWith(s + sepChar));
                    if (prefix == default)
                        continue;

                    string removedPrefix = trimmedFilter.Replace(prefix + sepChar, "");
                    if (!filterToCheck.Check(removedPrefix, out SearchFilter searchFilter))
                        continue;

                    searchFilter.Inverted = inverted;
                    FiltersToApply.Add(searchFilter);
                    filtered = true;
                    break;
                }

                if (!filtered)
                    searchWords.Add(filter);
            }

            textToSearch = string.Join(" ", searchWords);
        }

        public static void Postfix(scnCLS __instance)
        {
            for (int i = 0; i < __instance.searchLevelsDataIndex.Count; i++)
            {
                CustomLevelData level = __instance.levelsData[__instance.searchLevelsDataIndex[i]];

                bool removeLevel = false;
                foreach (SearchFilter filter in FiltersToApply)
                {
                    if (filter.CheckLevel(level) != filter.Inverted)
                        continue;

                    removeLevel = true;
                    break;
                }

                if (removeLevel)
                    __instance.searchLevelsDataIndex.RemoveAt(i--);
            }
        }
    }
}
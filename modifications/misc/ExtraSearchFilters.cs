using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;

namespace RDModifications;

[Modification(
    "If there should be more search filters in the CLS.\n" +
    "'pr:STATUS' searches for a specific PR status if LevelPRStatus is enabled (e.g. PR means peer-reviewed levels).\n" +
    "'2ponly:BOOLEAN' searches for levels that are either 2 players only if true, and 1 player only or both modes if false."
)]
public class ExtraSearchFilters : Modification
{
    [HarmonyPatch(typeof(scnCLS), nameof(scnCLS.SetSearchData))]
    public class FiltersPatch
    {
        public static List<string> ValidExtraFilters = ["pr", "2ponly"];
        public static Dictionary<string, string> ExtraFiltersAliases = new()
        {
            ["peerreview"] = "pr"
        };

        public static MethodInfo GetSearchBool = AccessTools.Method(typeof(scnCLS), "GetSearchBool");

        public static void AddFilterParameters(Dictionary<string, List<string>> filters, string name, IEnumerable<string> parameters)
        {
            if (!filters.TryGetValue(name, out List<string> filterParameters))
                filters[name] = filterParameters = [];
            filterParameters.AddRange(parameters);
        }

        public static void Prefix(ref string textToSearch, Dictionary<string, List<string>> filters)
        {
            if (!textToSearch.Contains(":") || !Enabled[typeof(LevelPRStatus)].Value)
                return;
                
            string[] potentialFilters = textToSearch.Split(" ");
            string finalString = "";

            foreach (string potentialFilter in potentialFilters)
            {
                if (!potentialFilter.Contains(":"))
                    goto NotAFilter;

                string[] args = potentialFilter.ToLowerInvariant().Split(":");
                if (args.Length != 2)
                    goto NotAFilter;

                string name = args[0];
                if (ExtraFiltersAliases.TryGetValue(name, out string realName))
                    name = realName;

                if (!ValidExtraFilters.Contains(name))
                    goto NotAFilter;

                string[] parameters = args[1].Split(",");

                switch (name)
                {
                    case "pr":
                        List<string> statuses = [];
                        foreach (string parameter in parameters)
                        {
                            if (!Enum.TryParse(parameter, true, out PRStatus _))
                                continue;

                            statuses.Add(parameter);
                        }

                        if (statuses.Count <= 0)
                            break;
                        AddFilterParameters(filters, name, statuses);
                        break;

                    case "2ponly":
                        AddFilterParameters(filters, name, parameters);
                        break;
                }

                continue;
            NotAFilter:
                finalString += potentialFilter;
                continue;
            }

            textToSearch = finalString;
        }

        public static void Postfix(scnCLS __instance, Dictionary<string, List<string>> filters)
        {
            foreach (KeyValuePair<string, List<string>> filter in filters)
            {
                string name = filter.Key;
                List<string> parameters = filter.Value;

                switch (name)
                {
                    case "pr":
                        if (parameters.Count < 0)
                            continue;

                        List<PRStatus> prStatuses = [.. parameters.Select(s => Enum.Parse<PRStatus>(s, true))];
                        __instance.searchLevelsDataIndex = [.. __instance.searchLevelsDataIndex.Where(i =>
                        {
                            PRStatus levelStatus = LevelPRStatus.PRLevels.Get(__instance.levelsData[i]);
                            return prStatuses.Contains(levelStatus);
                        })];
                        break;

                    case "2ponly":
                        bool? is2pOnly = (bool?)GetSearchBool.Invoke(__instance, [parameters]);

                        if (!is2pOnly.HasValue)
                            break;

                        __instance.searchLevelsDataIndex = [.. __instance.searchLevelsDataIndex.Where(i =>
                        {
                            return is2pOnly.Value == (__instance.levelsData[i].settings.canBePlayedOn == LevelPlayMode.TwoPlayerOnly);
                        })];
                        break;
                }
            }
        }
    }
}
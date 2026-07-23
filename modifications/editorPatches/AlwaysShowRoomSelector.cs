using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If the room selector for events should always be shown.", true)]
public class AlwaysShowRoomSelector : Modification
{
    [HarmonyPatch(typeof(BarBeatPosition), "Update")]
    public class RoomSelectorVisibilityPatch
    {
        public static void Postfix(BarBeatPosition __instance)
            => __instance.roomsContainer.SetActive(!__instance.forceRoomsHidden && __instance.roomsUsage != RoomsUsage.NotUsed);
    }
}
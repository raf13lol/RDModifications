using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RDModifications;

[Modification(
	"If you should be able to place more than 4 rows in one room.\n" +
	"(You are still only allowed 16 rows per level.)"
, true)]
public class RemoveFourRowLimit : Modification
{
 	static readonly FieldInfo maxUsedYField = AccessTools.Field(typeof(Timeline), "maxUsedY");

	public static void SetMaxYUsed()
	{
		scnEditor editor = scnEditor.instance;
		if (editor.currentTab != Tab.Rows)
			return;

		int maxUsedY = editor.currentPageRowsData.Count - 1;
		if (editor.rowsData.Count >= 16)
			maxUsedY--;
		maxUsedYField.SetValue(editor.timeline, maxUsedY);
	}

	[HarmonyPatch(typeof(TabSection_Rows), nameof(TabSection_Rows.Setup))]
	private class CreateRowHeadersPatch
    {
        public static void Postfix(TabSection_Rows __instance)
        {
			List<GameObject> rowHeaders = __instance.rowHeaders;
			GameObject baseHolder = __instance.rowHeaders[^1].transform.parent.gameObject;
			// __instance.rowsListRect.offsetMax = __instance.rowsListRect.offsetMax.WithY(-64f);
            while (rowHeaders.Count < 16)
            {
				GameObject holder = Object.Instantiate(baseHolder, baseHolder.transform.parent);
				GameObject rowHeader = holder.transform.GetChild(0).gameObject; 
				holder.name = $"holder{rowHeaders.Count}";
				rowHeader.name = $"rowHeader{rowHeaders.Count}";
				holder.transform.position = baseHolder.transform.position;
				rowHeader.GetComponent<RowHeader>().index = rowHeaders.Count;

				holder.SetActive(true);
				rowHeader.SetActive(true);
				rowHeaders.Add(rowHeader);
            }
			GameObject rowsList = baseHolder.transform.parent.gameObject;
			GameObject spritesList = __instance.editor.tabSection_sprites.headersListRect.parent.gameObject;
			RectTransform spritesListRectTransform = spritesList.GetComponent<RectTransform>();
			
			GameObject maskObject = new()
			{
				name = "MaskObjectRDM"
			};
			maskObject.transform.SetParent(rowsList.transform.parent);

			RectTransform rectTransform = maskObject.AddComponent<RectTransform>();
			rectTransform.offsetMin = spritesListRectTransform.offsetMin;
			rectTransform.offsetMax = spritesListRectTransform.offsetMax;
			rectTransform.anchorMin = spritesListRectTransform.anchorMin;
			rectTransform.anchorMax = spritesListRectTransform.anchorMax;
			rectTransform.pivot = spritesListRectTransform.pivot;
			rectTransform.anchoredPosition = spritesListRectTransform.anchoredPosition;
			rectTransform.sizeDelta = spritesListRectTransform.sizeDelta;

			maskObject.AddComponent<SpritesListScroll>();
			Image image = maskObject.AddComponent<Image>();
			Mask mask = maskObject.AddComponent<Mask>();
			image.sprite = spritesList.GetComponent<Image>().sprite;
			mask.showMaskGraphic = false;

			rowsList.transform.SetParent(maskObject.transform);

			ScrollSidePatch.VerticalScrollRectLastY = ScrollSidePatch.ScrollbarOffset = 0;
        }
    }

	[HarmonyPatch(typeof(TabSection), nameof(TabSection.LateUpdate))]
	private class ScrollSidePatch
    {
		public static float VerticalScrollRectLastY = 0;
		public static float ScrollbarOffset = 0;

        public static void Postfix(TabSection __instance)
        {
			float y = __instance.timeline.scrollViewVertContent.anchoredPosition.y;
			if (VerticalScrollRectLastY == y)
				return;

			Vector2 anchoredPosition = __instance.tabSection_rows.rowsListRect.anchoredPosition;

			ScrollbarOffset += y - VerticalScrollRectLastY;
			anchoredPosition.y += y - VerticalScrollRectLastY;

			__instance.tabSection_rows.rowsListRect.anchoredPosition = anchoredPosition;
			VerticalScrollRectLastY = y;
        }
    }

	[HarmonyPatch(typeof(Timeline), "UpdateUIInternalCo")]
	private class SideSizePatch
    {   
		public static RectTransform SavedRowsListRect = null;
		public static RectTransform DummyRectTransform = null;

		public static void Prefix(Timeline __instance)
        {
			if (GameObject.Find("DummyRectTransform") == null)
			{
				GameObject dummy = new()
                {
                    name = "DummyRectTransform"
                };
				DummyRectTransform = dummy.AddComponent<RectTransform>();
				SavedRowsListRect = __instance.tabSection_rows.rowsListRect;
			}
			__instance.tabSection_rows.rowsListRect = DummyRectTransform;
		}

		public static IEnumerator Postfix(IEnumerator __result, Timeline __instance)
		{
			FieldInfo isUpdatingUI = AccessTools.Field(typeof(Timeline), "isUpdatingUI");
			float offset = ScrollSidePatch.ScrollbarOffset;
			// Run original enumerator code
			while (__result.MoveNext())
			{
				isUpdatingUI.SetValue(__instance, true);
				if (__instance.editor.currentTab == Tab.Rows && (int)maxUsedYField.GetValue(__instance) == 0)
				{
					SetMaxYUsed();
        	        __instance.scrollViewVertContent.SizeDeltaY((__instance.usedRowCount - __instance.scaledRowCellCount) * __instance.cellHeight);
				}
				yield return __result.Current;
			}
			isUpdatingUI.SetValue(__instance, false);

			__instance.tabSection_rows.rowsListRect = SavedRowsListRect;
			RectTransform rectTransform = __instance.tabSection_rows.rowsListRect;
			Log.LogMessage($"BEFORE ap: {rectTransform.anchoredPosition} sd: {rectTransform.sizeDelta} h: {__instance.height} ch: {__instance.cellHeight} final {__instance.height - __instance.cellHeight * 16f}");

			// remove old scroll offset
			Vector2 anchoredPosition = rectTransform.anchoredPosition;
			anchoredPosition.y -= offset;
			rectTransform.anchoredPosition = anchoredPosition;

			rectTransform.offsetMin = rectTransform.offsetMin.WithY(__instance.height - __instance.cellHeight * 16f);

			// add the new one
			anchoredPosition = rectTransform.anchoredPosition;
			anchoredPosition.y += __instance.scrollViewVertContent.anchoredPosition.y;
			rectTransform.anchoredPosition = anchoredPosition;  

			Log.LogMessage($"AFTER ap: {rectTransform.anchoredPosition} sd: {rectTransform.sizeDelta} h: {__instance.height} ch: {__instance.cellHeight} final {__instance.height - __instance.cellHeight * 16f}");
		}
	}

	[HarmonyPatch(typeof(Timeline), "ApplyNewMaxUsedY")]
	private class ApplyNewMaxUsedYPatch
    {
        public static void Prefix()
        	=> SetMaxYUsed();
	}

	[HarmonyPatch(typeof(Timeline), "LateUpdate")]
	private class DisableRectPatch
    {
        public static void Postfix(Timeline __instance)
        {
			if (__instance.editor.currentTab != Tab.Rows)
				return;

            int maxHeight = __instance.scaledRowCellCount;
			if (__instance.editor.rowsData.Count < 16)
				maxHeight--;

			int enabledRows = Mathf.Min(__instance.editor.currentPageRowsData.Count, maxHeight);
			int height = (__instance.scaledRowCellCount - enabledRows) * __instance.cellHeight;
			__instance.disabledRowsQuad.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
        }
	}

	[HarmonyPatch(typeof(Timeline), nameof(Timeline.usedRowCount), MethodType.Getter)]
	private class UsedRowCountPatch
    {
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
				.MatchForward(false, [new(OpCodes.Ldc_I4_1)]) // Tab.Rows
				.SetInstruction(new(OpCodes.Ldc_I4_3)) // Tab.Rooms
				.InstructionEnumeration();
        }
	}

	[HarmonyPatch(typeof(TabSection_Rows), nameof(TabSection_Rows.UpdateUIInternal))]
	private class UpdateUI16RowHeadersPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
				.MatchForward(false, [new(OpCodes.Ldc_I4_4)]) // loop 4
				.SetInstruction(new(OpCodes.Ldc_I4, 16)) // loop 16
				.InstructionEnumeration();
        }

		public static void Postfix(RowHeader __instance)
			=> __instance.timeline.UpdateMaxUsedY();
    }

	[HarmonyPatch(typeof(RowHeader), nameof(RowHeader.UpdateUI))]
	private class RowHeaderUpdateUIPatch
    {
        public static void Prefix(ref bool isNextToLastRow)
			=> isNextToLastRow = isNextToLastRow && scnEditor.instance.rowsData.Count < 16;
	}

	[HarmonyPatch(typeof(RowHeader), nameof(RowHeader.Select))]
	private class RowHeaderUpdateScrollbarAddPatch
    {
        public static void Postfix(RowHeader __instance)
			=> __instance.timeline.UpdateMaxUsedY();
	}

	[HarmonyPatch(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.RoomDropdownWasUpdated))]
	private class RoomDropdownWasUpdatedPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			// skip the return on "room full"
            return new CodeMatcher(instructions)
				.MatchForward(false, [new(OpCodes.Bge)]) 
				.Advance(-2) // skip the ldc.i4.4
				.SetOpcodeAndAdvance(OpCodes.Ldc_I4_0) // 0 is not >= 4
				.InstructionEnumeration();
        }
	}

	[HarmonyPatch(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.UpdateRoomDropdown))]
	private class UpdateRoomDropdownPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			bool removeFullRoomSuffix = false;
			foreach (CodeInstruction instruction in instructions)
            {
				if (removeFullRoomSuffix)
                {
					yield return new(OpCodes.Nop); // call
					yield return new(OpCodes.Nop); // pointer byte 1
					yield return new(OpCodes.Nop); // pointer byte 2
					yield return new(OpCodes.Nop); // pointer byte 3
					yield return new(OpCodes.Nop); // pointer byte 4
                    removeFullRoomSuffix = false;
					continue;
                }
                if (instruction.OperandIs("editor.MakeRow.fullRoomSuffix"))
				{
					instruction.operand = "";
					removeFullRoomSuffix = true;
				}
				yield return instruction;
            }
        }
	}
}
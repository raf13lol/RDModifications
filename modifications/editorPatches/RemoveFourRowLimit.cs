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

				rowHeaders.Add(rowHeader);
            }
			GameObject rowsList = baseHolder.transform.parent.gameObject;
			GameObject spritesList = GameObject.Find("Sprites_List");
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
        }
    }

	[HarmonyPatch(typeof(TabSection), nameof(TabSection.LateUpdate))]
	private class ScrollSidePatch
    {
		private static float verticalScrollRectLastY = float.NaN;

        public static void Postfix(TabSection __instance)
        {
			if (__instance.tab != Tab.Rows)
				return;
			float y = __instance.timeline.scrollViewVertContent.anchoredPosition.y;
			if (verticalScrollRectLastY != y)
			{
				verticalScrollRectLastY = y;
				((TabSection_Rows)__instance).rowsListRect.AnchorPosY(y - __instance.editor.timeline.cellHeight * 4);
			}
        }
    }

	[HarmonyPatch(typeof(Timeline), "UpdateUIInternalCo")]
	private class SideSizePatch
    {   
		public static IEnumerator Postfix(IEnumerator __result, Timeline __instance)
		{
			// Run original enumerator code
			while (__result.MoveNext())
				yield return __result.Current;

			RectTransform rectTransform = __instance.tabSection_rows.rowsListRect;
			rectTransform.offsetMin = rectTransform.offsetMin.WithY(__instance.height - __instance.cellHeight * 16f);
			if (__instance.editor.currentTab == Tab.Rows)
            {
				FieldInfo maxUsedY = AccessTools.Field(typeof(Timeline), "maxUsedY");
				maxUsedY.SetValue(__instance, __instance.editor.currentPageRowsData.Count - 1);
                __instance.scrollViewVertContent.SizeDeltaY((__instance.usedRowCount - __instance.scaledRowCellCount) * __instance.cellHeight);
            }			
		}
	}

	[HarmonyPatch(typeof(Timeline), "ApplyNewMaxUsedY")]
	private class ApplyNewMaxUsedYPatch
    {
        public static void Prefix(Timeline __instance, ref int ___maxUsedY)
        {
            if (__instance.editor.currentTab == Tab.Rows)
                ___maxUsedY = __instance.editor.currentPageRowsData.Count - 1;
        }
	}

	[HarmonyPatch(typeof(Timeline), nameof(Timeline.usedRowCount), MethodType.Getter)]
	private class UsedRowCountPatch
    {
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
				.MatchForward(false, [new(OpCodes.Ldc_I4_1)])
				.SetInstruction(new(OpCodes.Ldc_I4_3))
				.InstructionEnumeration();
        }
	}

	[HarmonyPatch(typeof(TabSection_Rows), nameof(TabSection_Rows.UpdateUIInternal))]
	private class UpdateUI16RowHeadersPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
				.MatchForward(false, [new(OpCodes.Ldc_I4_4)])
				.SetInstruction(new(OpCodes.Ldc_I4, 16))
				.InstructionEnumeration();
        }

		public static void Postfix(RowHeader __instance)
			=> __instance.editor.timeline.UpdateMaxUsedY();
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
			=> __instance.editor.timeline.UpdateMaxUsedY();
	}

	[HarmonyPatch(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.RoomDropdownWasUpdated))]
	private class RoomDropdownWasUpdatedPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
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
					yield return new(OpCodes.Nop);
					yield return new(OpCodes.Nop);
					yield return new(OpCodes.Nop);
					yield return new(OpCodes.Nop);
					yield return new(OpCodes.Nop);
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
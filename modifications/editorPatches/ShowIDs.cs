using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If the IDs of sprites, rows and events related to Floating Text should be shown via the title text when the tag is visible as well.", true)]
public class ShowIDs : Modification
{
	private class TextPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.Show))]
		public static void MainPostfix(LevelEventControl_Base levelEventControl)
			=> DoText(levelEventControl.levelEvent);

		[HarmonyPostfix]
		[HarmonyPatch(typeof(RowHeader), nameof(RowHeader.ShowPanel))]
		public static void MakeRowPostfix(int rowIndex)
		{
			LevelEvent_MakeRow makeRow = scnEditor.instance.rowsData[rowIndex];
			makeRow.row = rowIndex; // Disgusting bullshit that is placed in my hands for On row creation
			DoText(makeRow); 
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteHeader), nameof(SpriteHeader.ShowPanel))]
		public static void MakeSpritePostfix(string spriteId)
		{
			LevelEvent_MakeSprite spriteData = SpriteHeader.GetSpriteData(spriteId);
			if (spriteData != null)
				DoText(spriteData);
		}

		public static void DoText(LevelEvent_Base levelEvent)
        {
			string text = string.Empty;
			if (levelEvent is LevelEvent_FloatingText ft)
				text = ft.id.ToString();
			else if (levelEvent is LevelEvent_AdvanceText at)
				text = at.id.ToString();
			else if (levelEvent is LevelEvent_MakeRow mr)
				text = mr.row.ToString();
			else if (levelEvent is LevelEvent_MakeSprite ms)
				text = ms.spriteId;

			if (text.IsNullOrEmpty())
				return;
			scnEditor.instance.inspectorTitle.text = RDString.Get($"editor.{levelEvent.type}");
		 	scnEditor.instance.inspectorTitle.text += (levelEvent is not LevelEvent_AdvanceText) ? '\n' : ' ';
			scnEditor.instance.inspectorTitle.text += $"(#{text})";
        }
	}

	[HarmonyPatch(typeof(Conditionals), nameof(Conditionals.Edit))]
	private class ConditionalTextPatch
    {
		public static void Postfix(int id)
			=> scnEditor.instance.inspectorTitle.text = $"{RDString.Get("editor.Conditionals.editCondition")}\n(#{id})";
    }
}
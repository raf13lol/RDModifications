using BepInEx.Configuration;
using BepInEx.Logging;

namespace RDModifications
{
    [EditorModification]
    public class TemplateModificationEditor
    {
        public static ManualLogSource logger;
        
        public static ConfigEntry<bool> enabled;

        public static bool Init(ConfigFile config, ManualLogSource logging)
        {
            logger = logging;
            enabled = config.Bind("EditorPatches", "Template", false,
            "Template");

            return enabled.Value;
        }
    }
}
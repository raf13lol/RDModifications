using BepInEx.Configuration;
using BepInEx.Logging;

namespace RDModifications;

[Modification]
public class TemplateModification
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("Template", "Enabled", false,
        "Template");

        return enabled.Value;
    }
}
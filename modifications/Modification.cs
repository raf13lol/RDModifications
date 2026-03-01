using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace RDModifications;

public class Modification
{
    public static ManualLogSource Log;
    public static Dictionary<Type, ConfigEntry<bool>> Enabled;
}
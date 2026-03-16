using BepInEx;
using BepInEx.Logging;

namespace RDModifications;

public struct AutoUpdateData
{
    public ManualLogSource Logger;
    public PluginInfo PluginInfo;
    
    public string GithubRepoURL;
    public string GithubRepoBranch;

    public string ReleaseName;
    public string VersionName;
    public string? ChangelogName;

    public bool IsZip;
}
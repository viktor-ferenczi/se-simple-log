using System.Diagnostics.CodeAnalysis;
using System.Text;
using HarmonyLib;
using VRage.Utils;

namespace ClientPlugin.Patches;

[HarmonyPatch(typeof(MyLog))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class GetLogNamePatch
{
    // Replace the log filename to exclude the timestamp
    [HarmonyPrefix]
    [HarmonyPatch("GetLogName")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool GetLogNamePrefix(string appName, ref StringBuilder __result)
    {
        __result = new StringBuilder(appName);
        __result.Append(".log");
        return false;
    }
}
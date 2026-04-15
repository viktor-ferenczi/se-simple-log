using System.Diagnostics.CodeAnalysis;
using System.Text;
using HarmonyLib;
using VRage.Utils;

namespace ClientPlugin.Patches;

[HarmonyPatch(typeof(MyLog))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class GetLogNamePatch
{
    private static Config Config => Config.Current;

    // Let the original GetLogName run, then modify the result:
    // - Remove the timestamp from the filename if configured
    // - Change the extension to .jsonl if configured
    [HarmonyPostfix]
    [HarmonyPatch("GetLogName")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void GetLogNamePostfix(string appName, ref StringBuilder __result)
    {
        var name = __result.ToString();

        if (Config.RemoveTimestamp)
            name = appName + ".log";

        if (Config.JsonlFormat)
            name = name.Substring(0, name.Length - 4) + ".jsonl";

        __result = new StringBuilder(name);
    }
}

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;

// DO NOT USE A NAMESPACE HERE!
// CRITICAL: Using a namespace here will prevent Pulsar from finding the Preloader class.
// INTENTIONALLY COMMENTED OUT: namespace ClientPlugin;

public class Preloader
{
    public static IEnumerable<string> TargetDLLs { get; } = [];

    public static void Initialize()
    {
    }

    public static void Patch(AssemblyDefinition assembly)
    {
    }

    // Runs right before Space Engineers starts, early enough to patch log file creation
    public static void Finish()
    {
        new Harmony(ClientPlugin.Plugin.Name).PatchAll(Assembly.GetExecutingAssembly());
    }
}

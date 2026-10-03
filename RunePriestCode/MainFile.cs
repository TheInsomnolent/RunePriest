using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RunePriest.RunePriestCode;

//You're recommended but not required to keep all your code in this package and all your assets in the RunePriest folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = ModVariant.ModId; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    /// <summary>Model ID / loc key prefix, matching BaseLib's root-namespace prefix (e.g. <c>RUNEPRIEST-</c>).</summary>
    public const string ModPrefix = ModVariant.IdUpper + "-";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        if (assembly.GetName().Name != ModId || typeof(MainFile).Namespace != $"{ModId}.RunePriestCode")
            Logger.Error($"Build variant mismatch: assembly '{assembly.GetName().Name}', namespace " +
                         $"'{typeof(MainFile).Namespace}', expected mod id '{ModId}'. Content IDs may collide.");

        // Required so custom Godot nodes created from code (e.g. NRuneBuffer) get their _Process callbacks.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);
     
        Harmony harmony = new(ModId);

        harmony.PatchAll(assembly);
    }
}

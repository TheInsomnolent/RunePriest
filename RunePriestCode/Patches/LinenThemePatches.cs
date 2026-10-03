using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using RunePriest.RunePriestCode.Character;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>Serves <see cref="RunePriestScenes"/>' virtual paths through the asset cache.</summary>
[HarmonyPatch(typeof(AssetCache))]
public static class RunePriestScenePatches
{
    [HarmonyPatch(nameof(AssetCache.GetScene))]
    [HarmonyPrefix]
    public static bool ResolveVirtualScene(string path, ref PackedScene __result)
    {
        if (RunePriestScenes.Resolve(path) is not { } scene) return true;
        __result = scene;
        return false;
    }

    // Preloading goes straight to ResourceLoader, which can't see virtual paths.
    [HarmonyPatch(nameof(AssetCache.CreateSession))]
    [HarmonyPrefix]
    public static void SkipVirtualPreloads(ref IEnumerable<string> paths) =>
        paths = paths.Select(RunePriestScenes.PreloadPath).OfType<string>().Distinct().ToList();
}

/// <summary>Swaps <see cref="RunePriestScenes"/>' marker scenes for their code-built nodes.</summary>
[HarmonyPatch]
public static class RunePriestSceneInstantiatePatch
{
    // The non-generic overload; Instantiate<T> forwards to it.
    public static MethodBase TargetMethod() =>
        typeof(PackedScene).GetMethod(nameof(PackedScene.Instantiate), 0, [typeof(PackedScene.GenEditState)])!;

    public static bool Prefix(PackedScene __instance, ref Node __result)
    {
        if (RunePriestScenes.Instantiate(__instance) is not { } node) return true;
        __result = node;
        return false;
    }
}

/// <summary>Applies <see cref="LinenTheme"/> to card frames, card trails and the energy counter.</summary>
public static class LinenThemePatches
{
    private const string LinenMeta = $"{MainFile.ModId}_linen";

    [HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.FrameMaterial), MethodType.Getter)]
    public static class FrameMaterial
    {
        // Postfix so it wins over BaseLib's HSV prefix.
        [HarmonyPostfix]
        public static void UseLinenFrame(CardPoolModel __instance, ref Material __result)
        {
            if (__instance is RunePriestCardPool) __result = LinenTheme.CardFrameMaterial;
        }
    }

    [HarmonyPatch(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create))]
    public static class CardTrail
    {
        [HarmonyPostfix]
        public static void RecolorTrail(string characterTrailPath, NCardTrailVfx? __result)
        {
            if (__result != null && characterTrailPath == RunePriestScenes.CardTrailPath) LinenTheme.Recolor(__result);
        }
    }

    [HarmonyPatch(typeof(NEnergyCounter))]
    public static class EnergyCounter
    {
        [HarmonyPatch(nameof(NEnergyCounter.Create))]
        [HarmonyPostfix]
        public static void RecolorVfx(Player player, NEnergyCounter? __result)
        {
            if (__result == null || player.Character is not Character.RunePriest) return;
            __result.SetMeta(LinenMeta, true);
            foreach (var name in (string[])["%EnergyVfxBack", "%EnergyVfxFront"])
            {
                if (__result.GetNodeOrNull(name) is { } vfx) LinenTheme.Recolor(vfx);
            }
        }

        // RefreshLabel resets the orb layers' material on every energy change.
        [HarmonyPatch("RefreshLabel")]
        [HarmonyPostfix]
        public static void TintOrb(NEnergyCounter __instance)
        {
            if (!__instance.HasMeta(LinenMeta)) return;
            foreach (var name in (string[])["%Layers", "%RotationLayers"])
            {
                if (__instance.GetNodeOrNull(name) is not { } layers) continue;
                foreach (var layer in layers.GetChildren().OfType<TextureRect>()) layer.Material = LinenTheme.EnergyOrbMaterial;
            }
        }
    }
}

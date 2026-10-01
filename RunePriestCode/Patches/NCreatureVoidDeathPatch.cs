using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RunePriest.RunePriestCode.Nodes;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>
/// The borrowed Architect rig has no death animation, so the Rune Priest is swallowed by a void instead
/// (<see cref="NVoidSwallow"/>). The body stays hidden while dead and reappears on revive.
/// </summary>
[HarmonyPatch(typeof(NCreature))]
public static class NCreatureVoidDeathPatch
{
    private static bool IsRunePriest(NCreature creature) => creature.Entity.Player?.Character is Character.RunePriest;

    [HarmonyPatch(nameof(NCreature._Ready))]
    [HarmonyPostfix]
    public static void HideIfAlreadyDead(NCreature __instance)
    {
        if (IsRunePriest(__instance) && __instance.Entity.IsDead) __instance.Visuals.Body.Visible = false;
    }

    // StartDeathAnim bails out early if a death animation is already running.
    [HarmonyPatch(nameof(NCreature.StartDeathAnim))]
    [HarmonyPrefix]
    public static void WasDying(NCreature __instance, out bool __state) =>
        __state = __instance.DeathAnimationTask is { IsCompleted: false };

    [HarmonyPatch(nameof(NCreature.StartDeathAnim))]
    [HarmonyPostfix]
    public static void PlayVoidSwallow(NCreature __instance, bool __state, ref float __result)
    {
        if (__state || !IsRunePriest(__instance)) return;
        NVoidSwallow.Create(__instance.Visuals);
        __result = NVoidSwallow.Duration;
    }

    [HarmonyPatch(nameof(NCreature.StartReviveAnim))]
    [HarmonyPostfix]
    public static void Restore(NCreature __instance)
    {
        if (!IsRunePriest(__instance)) return;
        foreach (var vfx in __instance.Visuals.GetChildren().OfType<NVoidSwallow>()) vfx.QueueFree();
        __instance.Visuals.Body.Visible = true;
    }
}

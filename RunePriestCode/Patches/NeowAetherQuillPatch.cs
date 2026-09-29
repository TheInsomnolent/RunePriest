using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using RunePriest.RunePriestCode.Relics;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>
/// Makes the Aether Quill a possible Neow starting reward for the Rune Priest: sometimes one of Neow's two positive
/// offers is replaced by <see cref="AetherInkwell"/> (a Neow relic that grants the potion).
/// </summary>
[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
public static class NeowAetherQuillPatch
{
    /// <summary>Chance per run that the offer appears (roughly the odds of any single vanilla positive offer).</summary>
    public const float Chance = 0.25f;

    [HarmonyPostfix]
    public static void Postfix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        var owner = __instance.Owner;
        // Only the regular Neow (two positive offers, then one cursed offer); modifier runs are left alone.
        if (owner?.Character is not Character.RunePriest || owner.RunState.Modifiers.Count > 0 || __result.Count < 3) return;

        // The event's own RNG keeps the offers deterministic (co-op, reloads).
        if (__instance.Rng.NextFloat() >= Chance) return;

        var options = __result.ToList();
        options[__instance.Rng.NextInt(2)] =
            __instance.RelicOption<AetherInkwell>("INITIAL", "NEOW.pages.DONE.POSITIVE.description");
        __result = options;
        MainFile.Logger.Info("[Rune] Neow offers the Aether Inkwell");
    }
}

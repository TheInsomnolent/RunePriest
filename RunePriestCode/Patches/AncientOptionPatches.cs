using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using RunePriest.RunePriestCode.Relics;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>
/// Adds Rune Priest boons to vanilla ancients (BaseLib only supports custom ancients). Each boon competes for its
/// ancient's slot with roughly the odds of one more relic in that slot's pool, rolled on the event's own RNG so offers
/// stay deterministic (co-op, reloads):
/// <list type="bullet">
/// <item>Darv: <see cref="DarkTablet"/> may replace one of the old relics (never Dusty Tome).</item>
/// <item>Vakuu pool 2: <see cref="CorruptedSigil"/>.</item>
/// <item>Vakuu pool 3: <see cref="Supernova"/>.</item>
/// <item>Tezcatara pool 3: <see cref="EternalCandle"/>.</item>
/// </list>
/// Orobas' Touch of Orobas / Archaic Tooth and Darv's Dusty Tome use BaseLib hooks instead (see <c>BlessedToolbox</c>,
/// the <c>ITranscendenceCard</c> starting runes and <c>Mirrororrim</c>).
/// </summary>
public static class AncientOptionPatches
{
    public const float DarkTabletChance = 0.25f;
    public const float CorruptedSigilChance = 0.25f;
    public const float SupernovaChance = 0.25f;
    public const float EternalCandleChance = 0.2f;

    private const int VakuuPool2Slot = 1;
    private const int VakuuPool3Slot = 2;
    private const int TezcataraPool3Slot = 2;

    private static bool OffersTo<T>(AncientEventModel ancient) where T : RelicModel =>
        ancient.Owner is { Character: Character.RunePriest } owner && owner.Relics.All(r => r is not T);

    private static void Replace(ref IReadOnlyList<EventOption> options, int slot, EventOption option, string ancient)
    {
        var list = options.ToList();
        list[slot] = option;
        options = list;
        MainFile.Logger.Info($"[Rune] {ancient} offers {option.Relic?.Id.Entry}");
    }

    [HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
    public static class DarvOptions
    {
        [HarmonyPostfix]
        public static void Postfix(Darv __instance, ref IReadOnlyList<EventOption> __result)
        {
            if (!OffersTo<DarkTablet>(__instance) || __instance.Rng.NextFloat() >= DarkTabletChance) return;
            var options = __result;
            var slots = Enumerable.Range(0, options.Count).Where(i => options[i].Relic is not DustyTome).ToList();
            if (slots.Count == 0) return;
            Replace(ref __result, __instance.Rng.NextItem(slots), __instance.RelicOption<DarkTablet>(), "Darv");
        }
    }

    [HarmonyPatch(typeof(Darv), nameof(Darv.AllPossibleOptions), MethodType.Getter)]
    public static class DarvAllOptions
    {
        [HarmonyPostfix]
        public static void Postfix(Darv __instance, ref IEnumerable<EventOption> __result) =>
            __result = __result.Append(__instance.RelicOption<DarkTablet>());
    }

    [HarmonyPatch(typeof(Vakuu), "GenerateInitialOptions")]
    public static class VakuuOptions
    {
        [HarmonyPostfix]
        public static void Postfix(Vakuu __instance, ref IReadOnlyList<EventOption> __result)
        {
            if (__result.Count > VakuuPool2Slot && OffersTo<CorruptedSigil>(__instance) &&
                __instance.Rng.NextFloat() < CorruptedSigilChance)
                Replace(ref __result, VakuuPool2Slot, __instance.RelicOption<CorruptedSigil>(), "Vakuu");

            if (__result.Count > VakuuPool3Slot && OffersTo<Supernova>(__instance) &&
                __instance.Rng.NextFloat() < SupernovaChance)
                Replace(ref __result, VakuuPool3Slot, __instance.RelicOption<Supernova>(), "Vakuu");
        }
    }

    [HarmonyPatch(typeof(Vakuu), nameof(Vakuu.AllPossibleOptions), MethodType.Getter)]
    public static class VakuuAllOptions
    {
        [HarmonyPostfix]
        public static void Postfix(Vakuu __instance, ref IEnumerable<EventOption> __result) =>
            __result = __result.Append(__instance.RelicOption<CorruptedSigil>()).Append(__instance.RelicOption<Supernova>());
    }

    [HarmonyPatch(typeof(Tezcatara), "GenerateInitialOptions")]
    public static class TezcataraOptions
    {
        [HarmonyPostfix]
        public static void Postfix(Tezcatara __instance, ref IReadOnlyList<EventOption> __result)
        {
            if (__result.Count <= TezcataraPool3Slot || !OffersTo<EternalCandle>(__instance) ||
                __instance.Rng.NextFloat() >= EternalCandleChance) return;
            Replace(ref __result, TezcataraPool3Slot, __instance.RelicOption<EternalCandle>(), "Tezcatara");
        }
    }

    [HarmonyPatch(typeof(Tezcatara), nameof(Tezcatara.AllPossibleOptions), MethodType.Getter)]
    public static class TezcataraAllOptions
    {
        [HarmonyPostfix]
        public static void Postfix(Tezcatara __instance, ref IEnumerable<EventOption> __result) =>
            __result = __result.Append(__instance.RelicOption<EternalCandle>());
    }
}

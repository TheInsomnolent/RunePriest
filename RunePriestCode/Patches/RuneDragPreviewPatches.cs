using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RunePriest.RunePriestCode.Nodes;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>Picking up a card (mouse drag or controller) starts a drag preview of the Incantation.</summary>
[HarmonyPatch]
public static class CardPlayStartPatch
{
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(NMouseCardPlay), nameof(NMouseCardPlay.Start)),
        AccessTools.Method(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start))
    ];

    [HarmonyPostfix]
    public static void Postfix(NCardPlay __instance) => RuneDragPreview.Begin(__instance);
}

/// <summary>The card's aimed-at creature: glyphs only merge into glyphs anchored to the same creature.</summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.SetPreviewTarget))]
public static class CardPreviewTargetPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCard __instance, Creature? creature) => RuneDragPreview.SetTarget(__instance, creature);
}

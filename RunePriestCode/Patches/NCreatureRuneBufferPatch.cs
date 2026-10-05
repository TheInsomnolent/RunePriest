using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RunePriest.RunePriestCode.Nodes;

namespace RunePriest.RunePriestCode.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class NCreatureRuneBufferPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        if (!__instance.Entity.IsPlayer) return;
        __instance.AddChild(NRuneBuffer.Create(__instance));
        __instance.AddChild(NCursedSpirits.Create(__instance));
    }
}

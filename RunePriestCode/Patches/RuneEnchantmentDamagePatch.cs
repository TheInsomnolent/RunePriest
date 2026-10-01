using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Cards;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>
/// Rune cards bake their enchantment into the rune's value when inscribed (<see cref="RuneCard"/>), so a rune's hit
/// (no card play, no preview) must skip the enchantment step of <see cref="Hook.ModifyDamage"/> or it would apply twice.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class RuneEnchantmentDamagePatch
{
    // Everything ModifyDamage does after the enchantment step; private, so looked up at runtime.
    private static readonly MethodInfo? ModifyDamageInternal = AccessTools.Method(typeof(Hook), "ModifyDamageInternal");

    [HarmonyPrefix]
    public static bool Prefix(IRunState runState, ICombatState? combatState, Creature? target, Creature? dealer,
        decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay,
        ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode,
        ref IEnumerable<AbstractModel> modifiers, ref decimal __result)
    {
        if (ModifyDamageInternal == null || cardSource is not RuneCard { Enchantment: not null } || cardPlay != null ||
            previewMode != CardPreviewMode.None)
            return true;

        object?[] args = [runState, combatState, target, dealer, damage, props, cardSource, cardPlay, modifyDamageHookType, null];
        __result = Math.Max(0m, (decimal)ModifyDamageInternal.Invoke(null, args)!);
        modifiers = (List<AbstractModel>)args[^1]!;
        return false;
    }
}

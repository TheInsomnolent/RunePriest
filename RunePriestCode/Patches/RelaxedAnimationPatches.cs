using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using RunePriest.RunePriestCode.Character;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>
/// Shop scenes idle players on a hard-coded <c>relaxed_loop</c>, which the borrowed Architect rig lacks (Spine logs an
/// error). These swap in <c>idle_loop</c> for rigs without it (<see cref="ArchitectRig.Fallback"/>).
/// </summary>
public static class RelaxedAnimationPatches
{
    /// <summary>The fake merchant event animates each player's combat visuals directly.</summary>
    [HarmonyPatch(typeof(NFakeMerchant), "StartCharacterAnimation")]
    public static class FakeMerchant
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions)
                .MatchStartForward(new CodeMatch(OpCodes.Ldstr, ArchitectRig.RelaxedAnim));
            if (matcher.IsInvalid)
            {
                MainFile.Logger.Warn($"[Rune] {ArchitectRig.RelaxedAnim} not found in NFakeMerchant.StartCharacterAnimation; patch skipped");
                return matcher.Start().InstructionEnumeration();
            }

            // Replace the string literal with AnimFor(visuals); both leave one string on the stack.
            return matcher
                .SetAndAdvance(OpCodes.Ldarg_1, null)
                .Insert(CodeInstruction.Call(() => AnimFor(null!)))
                .InstructionEnumeration();
        }

        public static string AnimFor(NCreatureVisuals visuals) =>
            visuals.SpineBody is { } body ? ArchitectRig.Fallback(body, ArchitectRig.RelaxedAnim) : ArchitectRig.RelaxedAnim;
    }

    /// <summary>The real merchant room uses <see cref="NMerchantCharacter"/>, whose first child is the rig.</summary>
    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
    public static class MerchantCharacter
    {
        [HarmonyPrefix]
        public static void UseAvailableAnim(NMerchantCharacter __instance, ref string anim)
        {
            if (__instance.GetChildCount() == 0 || __instance.GetChild(0) is not { } rig || rig.GetClass() != "SpineSprite") return;
            anim = ArchitectRig.Fallback(new MegaSprite(rig), anim);
        }
    }
}

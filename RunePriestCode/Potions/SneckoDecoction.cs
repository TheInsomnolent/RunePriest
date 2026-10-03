using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Potions;

/// <summary>
/// Randomly redistributes the values of every payload rune with a value (Strike, Defend, Mend, Blood, Hex, Kindle,
/// Swift, Diminish) in your Incantation. Modifiers and valueless runes are left alone.
/// </summary>
public sealed class SneckoDecoction : RunePriestPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    private static bool IsShuffled(Rune rune) => rune is PayloadRune { ShowsValue: true } && rune.WithValue(rune.Value) != null;

    private static List<int> ShuffledValues(RuneBuffer buffer) =>
        buffer.Glyphs.SelectMany(g => g.Runes).Where(IsShuffled).Select(r => r.Value).ToList();

    /// <summary>Only usable while at least two runes have values to swap, so the potion isn't wasted.</summary>
    public override bool PassesCustomUsabilityCheck =>
        Owner?.Creature is { } creature && RuneCmd.GetBuffer(creature) is { IsSpeaking: false } buffer
        && ShuffledValues(buffer).Count >= 2;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (RuneCmd.GetBuffer(Owner.Creature) is not { } buffer) return Task.CompletedTask;

        var values = ShuffledValues(buffer);
        Owner.RunState.Rng.CombatTargets.Shuffle(values);
        var next = 0;
        RuneCmd.Transform(Owner, g => g.Runes.Any(IsShuffled)
            ? g.WithRunes(g.Runes.Select(r => IsShuffled(r) ? r.WithValue(values[next++])! : r).ToArray())
            : g);
        MainFile.Logger.Info($"[Rune] Snecko Decoction shuffled values: {string.Join(" ", buffer.Glyphs)}");
        return Task.CompletedTask;
    }
}

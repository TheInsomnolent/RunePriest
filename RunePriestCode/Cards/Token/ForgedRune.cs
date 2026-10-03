using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Special;

/// <summary>
/// Made by the Enchanted Forge from two Common rune cards: Inscribes both cards' runes in order (their Imbued runes
/// first, upgrades baked in). Costs the higher of the two and is an Attack if either was. Only the runes carry over.
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class ForgedRune() : RuneCard(0, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    private string? _decodedRunes;
    private IReadOnlyList<Glyph> _decodedGlyphs = [];

    /// <summary>The forged runes as <see cref="GlyphCodec"/> text. Saved with the deck card.</summary>
    [SavedProperty]
    public string ForgedRunes { get; set; } = "";

    [SavedProperty]
    public int ForgedCost { get; set; }

    [SavedProperty]
    public bool ForgedAttack { get; set; }

    /// <summary>The originals' upgrades are already baked into the runes.</summary>
    public override int MaxUpgradeLevel => 0;

    public override CardType Type => ForgedAttack ? CardType.Attack : CardType.Skill;

    public IReadOnlyList<Glyph> ForgedGlyphs
    {
        get
        {
            if (_decodedRunes != ForgedRunes)
            {
                _decodedGlyphs = GlyphCodec.Decode(ForgedRunes);
                _decodedRunes = ForgedRunes;
            }
            return _decodedGlyphs;
        }
    }

    public override TargetType TargetType =>
        ForgedGlyphs.Any(g => g.Runes.Any(r => r is PayloadRune { Targeting: RuneTargeting.Enemy }))
            ? TargetType.AnyEnemy
            : base.TargetType;

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => ForgedGlyphs.Select(g => g.AnchoredTo(anchor));

    /// <summary>Takes on the runes, cost and type of <paramref name="cards"/>, in order.</summary>
    public void Forge(IEnumerable<RuneCard> cards)
    {
        var list = cards.ToList();
        ForgedRunes = GlyphCodec.Encode(list.SelectMany(c => c.ImbuedGlyphs.Concat(c.InscribedGlyphs)));
        ForgedCost = list.Count == 0 ? 0 : list.Max(c => c.EnergyCost.GetWithModifiers(CostModifiers.None));
        ForgedAttack = list.Any(c => c.Type == CardType.Attack);
        EnergyCost.SetCustomBaseCost(ForgedCost);
    }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        EnergyCost.SetCustomBaseCost(ForgedCost);
    }

    protected override void OnUpgrade()
    {
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("ForgedCount", (decimal)ForgedGlyphs.Count);
        description.Add("ForgedRunes", RuneTips.InscribeText(ForgedGlyphs));
    }
}

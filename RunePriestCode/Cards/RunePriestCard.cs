using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using RunePriest.RunePriestCode.Character;
using RunePriest.RunePriestCode.Extensions;
using RunePriest.RunePriestCode.Runes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace RunePriest.RunePriestCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(RunePriestCardPool))]
public abstract class RunePriestCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190
    
    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Unowned/canonical cards (ancient offer previews, card library) have no Incantation.
    protected RuneBuffer? Incantation =>
        IsMutable && Owner?.Creature is { } creature ? RuneCmd.GetBuffer(creature) : null;

    protected int IncantationSize => Incantation?.Glyphs.Count ?? 0;

    /// <summary>Individual runes in the Incantation: simultaneous runes (one compound glyph) each count.</summary>
    protected int IncantationRuneCount => Incantation?.Glyphs.Sum(g => g.Runes.Count) ?? 0;

    /// <summary>
    /// Runes bound to this card by Imbue, as <see cref="GlyphCodec"/> text ("" = the Imbue slot is free).
    /// Imbue only sets it on the combat copy of a card, so Imbues end with the combat; Aether Quill writes it through
    /// to the deck card (<see cref="MakeImbuePermanent"/>), which saves it with the run.
    /// </summary>
    [SavedProperty]
    public string ImbuedRunes { get; set; } = "";

    public bool IsImbued => !string.IsNullOrEmpty(ImbuedRunes);

    /// <summary>
    /// Whether a card is named "… Rune" (class <c>XRuneCard</c>, ID <c>…_RUNE_CARD</c>). Uses the model ID rather than
    /// the localized title so every player in co-op agrees.
    /// </summary>
    public static bool HasRuneInName(CardModel card) => card.Id.Entry.RemovePrefix().Split('_').Contains("RUNE");

    /// <summary>Ascended cards are Ancient cards that never upgrade; the runes they create are <see cref="Rune.Radiant"/>.</summary>
    public bool IsAscended => Rarity == CardRarity.Ancient && MaxUpgradeLevel == 0;

    /// <summary>Whether this combat card is Imbued and has a deck card its Imbue can be made permanent on.</summary>
    public bool CanMakeImbuePermanent =>
        IsImbued && DeckVersion is RunePriestCard deckCard && !ReferenceEquals(deckCard, this)
        && deckCard.ImbuedRunes != ImbuedRunes;

    /// <summary>
    /// Aether Quill: copies this combat card's Imbue onto its deck card, so it starts every later combat Imbued.
    /// </summary>
    /// <returns>False if the card isn't Imbued or has no deck card (e.g. it was generated this combat).</returns>
    public bool MakeImbuePermanent()
    {
        if (!CanMakeImbuePermanent) return false;
        ((RunePriestCard)DeckVersion!).ImbuedRunes = ImbuedRunes;
        MainFile.Logger.Info($"[Rune] {Id.Entry} permanently Imbued with {ImbuedRunes}");
        return true;
    }

    private string? _decodedRunes;
    private IReadOnlyList<Glyph> _decodedGlyphs = [];

    public IReadOnlyList<Glyph> ImbuedGlyphs
    {
        get
        {
            if (_decodedRunes != ImbuedRunes)
            {
                _decodedGlyphs = GlyphCodec.Decode(ImbuedRunes);
                _decodedRunes = ImbuedRunes;
            }
            return _decodedGlyphs;
        }
    }

    /// <summary>
    /// An Imbued card that doesn't normally pick an enemy asks for one while it holds enemy-targeting runes, so the
    /// bound runes are anchored to the chosen enemy instead of hitting a random one.
    /// </summary>
    public override TargetType TargetType =>
        base.TargetType is TargetType.Self or TargetType.None
        && ImbuedGlyphs.Any(g => g.Runes.Any(r => r is PayloadRune { Targeting: RuneTargeting.Enemy }))
            ? TargetType.AnyEnemy
            : base.TargetType;

    /// <summary>Imbue hover tip, plus the runes bound to this card once it is Imbued.</summary>
    protected IEnumerable<IHoverTip> ImbueHoverTips =>
        IsImbued ? [RuneTips.Imbue, RuneTips.Imbued(ImbuedGlyphs)] : [RuneTips.Imbue];

    /// <summary>
    /// Lets card text show what an Imbued card now does: <c>{ImbuedCount}</c> is the number of bound runes (0 = free
    /// slot) and <c>{ImbuedRunes}</c> their Inscribe lines, e.g.
    /// <c>{ImbuedCount:choose(0):[gold]Imbue[/gold] {Imbue}.|{ImbuedRunes}}</c>.
    /// </summary>
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        var glyphs = ImbuedGlyphs;
        description.Add("ImbuedCount", (decimal)glyphs.Count);
        description.Add("ImbuedRunes", RuneTips.InscribeText(glyphs));
    }

    /// <summary>
    /// Imbue: if this card's Imbue slot is free, bind up to <paramref name="count"/> of the most recent runes
    /// (matching <paramref name="filter"/>) to it, removing them from the Incantation. If the slot is already filled
    /// it can't be overwritten; instead the bound runes are Inscribed, anchored to the card's target.
    /// </summary>
    /// <returns>How many runes were bound by this play.</returns>
    protected async Task<int> Imbue(PlayerChoiceContext choiceContext, CardPlay cardPlay, int count, Func<Glyph, bool>? filter = null)
    {
        if (IsImbued)
        {
            await RuneCmd.Inscribe(choiceContext, Owner, ImbuedGlyphs.Select(g => g.AnchoredTo(cardPlay.Target)), this);
            return 0;
        }

        var taken = await RuneCmd.TakeForImbue(choiceContext, Owner, count, filter);
        if (taken.Count == 0) return 0;

        // Combat cards are copies of the deck cards, so the runes are gone once the combat ends.
        ImbuedRunes = GlyphCodec.Encode(taken);
        return taken.Count;
    }

    /// <summary>
    /// Drag preview: plays out on <paramref name="draft"/> what <c>OnPlay</c> would do to the Incantation if this card
    /// were played now at <paramref name="anchor"/>. Mirror every <see cref="RuneCmd"/> call; must be side-effect free.
    /// Default: nothing (no preview).
    /// </summary>
    public virtual void PreviewIncantation(IncantationDraft draft, Creature? anchor)
    {
    }

    /// <summary>Drag preview of <see cref="Imbue"/>.</summary>
    protected void PreviewImbue(IncantationDraft draft, Creature? anchor, int count, Func<Glyph, bool>? filter = null)
    {
        if (IsImbued) draft.Inscribe(ImbuedGlyphs.Select(g => g.AnchoredTo(anchor)), this);
        else draft.TakeForImbue(count, filter);
    }

    /// <summary>What <c>ResolveEnergyXValue</c> would return if this X-cost card were played now (drag preview).</summary>
    protected int PreviewXValue =>
        CombatState is { } combat ? Hook.ModifyXValue(combat, this, Owner.PlayerCombatState?.Energy ?? 0) : 0;

    protected Task Draw(PlayerChoiceContext choiceContext, decimal count) => CardPileCmd.Draw(choiceContext, count, Owner);

    protected async Task DealDamage(PlayerChoiceContext choiceContext, CardPlay cardPlay, decimal amount)
    {
        if (cardPlay.Target == null || amount <= 0) return;
        await DamageCmd.Attack(amount).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected async Task AddToDrawPile<T>(int count) where T : CardModel
    {
        for (var i = 0; i < count; i++)
        {
            var card = CombatState!.CreateCard<T>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Random));
        }
    }

    protected async Task AddToHand<T>(int count, bool upgraded = false) where T : CardModel
    {
        for (var i = 0; i < count; i++)
        {
            var card = CombatState!.CreateCard<T>(Owner);
            if (upgraded) CardCmd.Upgrade(card, CardPreviewStyle.None);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner));
        }
    }
}
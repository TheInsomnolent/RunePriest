using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using RunePriest.RunePriestCode.Character;
using RunePriest.RunePriestCode.Extensions;
using RunePriest.RunePriestCode.Runes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

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

    protected RuneBuffer? Incantation => RuneCmd.GetBuffer(Owner.Creature);

    protected int IncantationSize => Incantation?.Glyphs.Count ?? 0;

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
}
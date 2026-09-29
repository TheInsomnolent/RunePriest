using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using RunePriest.RunePriestCode.Cards.Basic;
using RunePriest.RunePriestCode.Extensions;
using RunePriest.RunePriestCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace RunePriest.RunePriestCode.Character;

public class RunePriest : PlaceholderCharacterModel
{
    public const string CharacterId = "RunePriest";
    
    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeRuneCard>(),
        ModelDb.Card<StrikeRuneCard>(),
        ModelDb.Card<StrikeRuneCard>(),
        ModelDb.Card<StrikeRuneCard>(),
        ModelDb.Card<StrikeRuneCard>(),
        ModelDb.Card<DefendRuneCard>(),
        ModelDb.Card<DefendRuneCard>(),
        ModelDb.Card<DefendRuneCard>(),
        ModelDb.Card<DefendRuneCard>(),
        ModelDb.Card<EchoRuneCard>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BlessedToolbox>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<RunePriestCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<RunePriestRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<RunePriestPotionPool>();
    
    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets. 
        These are just some of the simplest assets, given some placeholders to differentiate your character with. 
        You don't have to, but you're suggested to rename these images. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}
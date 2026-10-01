using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.NodeFactories;
using RunePriest.RunePriestCode.Cards.Basic;
using RunePriest.RunePriestCode.Extensions;
using RunePriest.RunePriestCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

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
        ModelDb.Card<DefendRuneCard>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BlessedToolbox>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<RunePriestCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<RunePriestRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<RunePriestPotionPool>();

    // Borrowed Architect Spine rig; HSV values feed res://shaders/hsv.gdshader (1,1,1 = unchanged).
    private static readonly string ArchitectVisualsPath = SceneHelper.GetScenePath("creature_visuals/architect");
    private const float VisualsHue = 0.75f;
    private const float VisualsSaturation = 1f;
    private const float VisualsValue = 1f;
    private const float VisualsScale = 0.75f;

    public override NCreatureVisuals CreateCustomVisuals()
    {
        var visuals = PreloadManager.Cache.GetScene(ArchitectVisualsPath).Instantiate<NCreatureVisuals>();
        visuals.DefaultScale = VisualsScale;
        visuals.Scale = Vector2.One * VisualsScale;

        // The Architect faces left (enemy side); mirror only the body so bounds/markers stay put.
        var body = visuals.GetNode<Node2D>("%Visuals");
        body.Scale = new Vector2(-body.Scale.X, body.Scale.Y);
        body.Call("set_normal_material", ShaderUtils.GenerateHsv(VisualsHue, VisualsSaturation, VisualsValue));
        return visuals;
    }

    // The Architect rig only has idle_loop/attack/hurt; anything else falls back to idle.
    public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", hitName: "hurt", attackName: "attack", castName: "attack");
    
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
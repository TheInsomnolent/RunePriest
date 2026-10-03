using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using RunePriest.RunePriestCode.Cards.Basic;
using RunePriest.RunePriestCode.Extensions;
using RunePriest.RunePriestCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace RunePriest.RunePriestCode.Character;

public class RunePriest : PlaceholderCharacterModel
{
    public const string CharacterId = "RunePriest";
    
    public static readonly Color Color = LinenTheme.Linen;

    public override Color NameColor => Color;
    public override Color EnergyLabelOutlineColor => LinenTheme.Umber;
    public override Color DialogueColor => LinenTheme.DeepUmber;
    public override VfxColor SpeechBubbleColor => VfxColor.White;
    // Map drawings sit on parchment, so they use a darker shade to stay visible.
    public override Color MapDrawingColor => LinenTheme.Umber;
    public override Color RemoteTargetingLineColor => LinenTheme.Linen;
    public override Color RemoteTargetingLineOutline => LinenTheme.Umber;
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

    // Borrowed Architect Spine rig (see ArchitectRig). CreateCustomVisuals builds the actual node; the path still
    // matters because it's preloaded with the run's character assets.
    public override string CustomVisualPath => ArchitectRig.ScenePath;
    public override string CustomRestSiteAnimPath => RunePriestScenes.RestSitePath;
    public override string CustomMerchantAnimPath => RunePriestScenes.MerchantPath;
    private const float VisualsScale = 0.75f;

    public override NCreatureVisuals CreateCustomVisuals()
    {
        var visuals = ArchitectRig.InstantiateVisuals();
        visuals.DefaultScale = VisualsScale;
        visuals.Scale = Vector2.One * VisualsScale;

        // The Architect faces left (enemy side); mirror only the body, then refit bounds/markers around it.
        var body = visuals.GetNode<Node2D>("%Visuals");
        body.Scale = new Vector2(-body.Scale.X, body.Scale.Y);
        FitToBody(visuals);

        // Player visuals need a %FormVfx holder: death and Form cards call Add/RemoveFormVfx, which throw without it.
        var formVfx = new Control { Name = "FormVfx", MouseFilter = Control.MouseFilterEnum.Ignore };
        visuals.AddChild(formVfx);
        visuals.MoveChild(formVfx, 0);
        formVfx.Owner = visuals;
        formVfx.UniqueNameInOwner = true;
        return visuals;
    }

    // Architect-scene units (pre-VisualsScale). Fitting %Bounds to ArchitectRig.BodyBounds keeps the hitbox, HP bar
    // (bounds width + 24) and rune row on the mirrored model instead of the NPC's wider left-facing pose.
    private static Rect2 BodyBounds => ArchitectRig.BodyBounds;
    private static readonly Vector2 BodyCenter = new(20f, -220f);
    // Orbs fall back to IntentPos when there's no OrbPos; vanilla puts it ~30px above the bounds.
    private static readonly Vector2 BodyIntent = new(20f, -450f);

    private static void FitToBody(NCreatureVisuals visuals)
    {
        var bounds = visuals.GetNode<Control>("%Bounds");
        bounds.OffsetLeft = BodyBounds.Position.X;
        bounds.OffsetTop = BodyBounds.Position.Y;
        bounds.OffsetRight = BodyBounds.End.X;
        bounds.OffsetBottom = BodyBounds.End.Y;

        visuals.GetNode<Marker2D>("%CenterPos").Position = BodyCenter;
        visuals.GetNode<Marker2D>("%IntentPos").Position = BodyIntent;
        var talk = visuals.GetNode<Marker2D>("%TalkPos");
        talk.Position = new Vector2(-talk.Position.X, talk.Position.Y);
    }

    // The Architect rig only has idle_loop/attack/hurt; anything else falls back to idle.
    public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, ArchitectRig.IdleAnim, hitName: "hurt", attackName: "attack", castName: "attack");
    
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
    public override string CustomCharacterSelectBg => RunePriestScenes.SelectBackgroundPath;
    public override string CustomTrailPath => RunePriestScenes.CardTrailPath;
    // Defect's orb has the most neutral silhouette; LinenThemePatches tints it.
    public override string CustomEnergyCounterPath => SceneHelper.GetScenePath("combat/energy_counters/defect_energy_counter");
}
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace RunePriest.RunePriestCode.Character;

/// <summary>
/// Scenes the Rune Priest serves through virtual <c>res://</c> paths (see <c>RunePriestScenePatches</c>). The CI
/// pck packer can't pack <c>.tscn</c> files, so scenes are built in code or borrowed from the base game. Scenes whose
/// root needs a game script (rest site, merchant) resolve to a marker scene that is swapped for a code-built node on
/// instantiation.
/// </summary>
public static class RunePriestScenes
{
    public const string SelectBackgroundPath = $"{MainFile.ResPath}/scenes/char_select_bg.tscn";

    // An alias rather than the vanilla path so trail patches can tell Rune Priest trails apart from a co-op Regent's.
    public const string CardTrailPath = $"{MainFile.ResPath}/scenes/card_trail.tscn";

    public const string RestSitePath = $"{MainFile.ResPath}/scenes/rest_site_character.tscn";
    public const string MerchantPath = $"{MainFile.ResPath}/scenes/merchant_character.tscn";

    private static readonly string BorrowedTrailPath = SceneHelper.GetScenePath("vfx/card_trail_regent");

    // The "Arise" epoch portrait ships with the base game, so it's referenced rather than bundled.
    private static readonly string SelectArtPath = ImageHelper.GetImagePath("timeline/epoch_portraits/relic5_epoch.png");

    // Body scale relative to the Architect scene root (combat uses 0.75). Vanilla rest-site/merchant rigs draw the
    // characters larger than in combat (Ironclad: 0.28 combat vs 0.47 merchant).
    private const float RestSiteScale = 1.0f;
    private const float MerchantScale = 1.2f;

    private static PackedScene? _selectBackground;
    private static PackedScene? _restSiteMarker;
    private static PackedScene? _merchantMarker;

    public static PackedScene? Resolve(string path) => path switch
    {
        SelectBackgroundPath => SelectBackground,
        CardTrailPath => PreloadManager.Cache.GetScene(BorrowedTrailPath),
        RestSitePath => Valid(ref _restSiteMarker, PackMarker),
        MerchantPath => Valid(ref _merchantMarker, PackMarker),
        _ => null
    };

    /// <summary>Maps a path queued for preloading to what should actually be loaded (null to skip it).</summary>
    public static string? PreloadPath(string path) => path switch
    {
        SelectBackgroundPath => null,
        CardTrailPath => BorrowedTrailPath,
        RestSitePath or MerchantPath => ArchitectRig.ScenePath,
        _ => path
    };

    /// <summary>Builds the node a marker scene stands in for, or null if <paramref name="scene"/> isn't a marker.</summary>
    public static Node? Instantiate(PackedScene scene)
    {
        if (scene == _restSiteMarker) return BuildRestSiteCharacter();
        if (scene == _merchantMarker) return BuildMerchantCharacter();
        return null;
    }

    private static PackedScene SelectBackground => Valid(ref _selectBackground, BuildSelectBackground);

    private static PackedScene Valid(ref PackedScene? cached, Func<PackedScene> build)
    {
        if (cached != null && GodotObject.IsInstanceValid(cached)) return cached;
        cached = build();
        return cached;
    }

    // Never instantiated as-is, but packed with a root so it's a valid scene if anything inspects it.
    private static PackedScene PackMarker()
    {
        var root = new Node2D { Name = "RunePriestMarker" };
        var scene = new PackedScene();
        scene.Pack(root);
        root.Free();
        return scene;
    }

    /// <summary>Mirrors the vanilla <c>*_rest_site.tscn</c> layout around the Architect body.</summary>
    private static NRestSiteCharacter BuildRestSiteCharacter()
    {
        var root = new NRestSiteCharacter { Name = "RunePriestRestSite" };

        // Body goes under ControlRoot: FlipX still mirrors it, and the act-specific rest animations (which the
        // Architect rig lacks) only target direct SpineSprite children.
        var controlRoot = new Control { Name = "ControlRoot" };
        root.AddChild(controlRoot);
        controlRoot.Owner = root;

        // Vanilla rest-site rigs are centred on the origin (the room zeroes the root position).
        var bounds = ScaleRect(ArchitectRig.BodyBounds, RestSiteScale);
        var offset = -bounds.GetCenter();
        bounds.Position += offset;

        var body = ArchitectRig.TakeBody(RestSiteScale);
        body.Position += offset;
        controlRoot.AddChild(body);

        var reticle = SceneHelper.Instantiate<NSelectionReticle>("ui/selection_reticle");
        AddUnique(root, controlRoot, reticle, "SelectionReticle", bounds);
        AddUnique(root, controlRoot, new Control(), "Hitbox", bounds);
        AddUnique(root, controlRoot, new Control(), "ThoughtBubbleRight", Anchor(bounds, 0.85f, 0.05f));
        AddUnique(root, controlRoot, new Control(), "ThoughtBubbleLeft", Anchor(bounds, 0.2f, 0.05f));
        return root;
    }

    /// <summary>Vanilla merchant rigs are a single SpineSprite child with its feet at the origin.</summary>
    private static NMerchantCharacter BuildMerchantCharacter()
    {
        var root = new NMerchantCharacter { Name = "RunePriestMerchant" };
        var body = ArchitectRig.TakeBody(MerchantScale);
        body.Position += new Vector2(-ArchitectRig.BodyBounds.GetCenter().X * MerchantScale, 0f);
        root.AddChild(body);
        return root;
    }

    private static Rect2 ScaleRect(Rect2 rect, float scale) => new(rect.Position * scale, rect.Size * scale);

    private static Rect2 Anchor(Rect2 bounds, float x, float y) =>
        new(bounds.Position + bounds.Size * new Vector2(x, y), Vector2.Zero);

    private static void AddUnique(Node owner, Node parent, Control child, string name, Rect2 rect)
    {
        child.Name = name;
        parent.AddChild(child);
        child.OffsetLeft = rect.Position.X;
        child.OffsetTop = rect.Position.Y;
        child.OffsetRight = rect.End.X;
        child.OffsetBottom = rect.End.Y;
        child.Owner = owner;
        child.UniqueNameInOwner = true;
    }

    private static PackedScene BuildSelectBackground()
    {
        var root = new Control { Name = "RunePriestBg", MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        if (ResourceLoader.Exists(SelectArtPath))
        {
            var art = new TextureRect
            {
                Name = "Art",
                Texture = ResourceLoader.Load<Texture2D>(SelectArtPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = new Color(0.85f, 0.85f, 0.85f)
            };
            AddFullRect(root, art);
        }
        else
        {
            MainFile.Logger.Warn($"Character select art missing: {SelectArtPath}");
        }

        // Darken the left side, where the character info panel sits.
        var gradient = new Gradient
        {
            Offsets = [0f, 0.2f, 0.5f],
            Colors = [new Color(0f, 0f, 0f, 0.8f), new Color(0f, 0f, 0f, 0.6f), new Color(0f, 0f, 0f, 0f)]
        };
        var shade = new TextureRect
        {
            Name = "Shade",
            Texture = new GradientTexture2D { Gradient = gradient, FillFrom = Vector2.Zero, FillTo = Vector2.Right, Width = 256, Height = 4 },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddFullRect(root, shade);

        var scene = new PackedScene();
        var error = scene.Pack(root);
        if (error != Error.Ok) MainFile.Logger.Error($"Failed to pack character select background: {error}");
        root.Free();
        return scene;
    }

    private static void AddFullRect(Control root, Control child)
    {
        root.AddChild(child);
        child.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        child.Owner = root;
    }
}

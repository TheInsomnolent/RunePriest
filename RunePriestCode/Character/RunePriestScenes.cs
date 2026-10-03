using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;

namespace RunePriest.RunePriestCode.Character;

/// <summary>
/// Scenes the Rune Priest serves through virtual <c>res://</c> paths (see <c>RunePriestScenePatches</c>). The CI
/// pck packer can't pack <c>.tscn</c> files, so scenes are built in code or borrowed from the base game.
/// </summary>
public static class RunePriestScenes
{
    public const string SelectBackgroundPath = $"{MainFile.ResPath}/scenes/char_select_bg.tscn";

    // An alias rather than the vanilla path so trail patches can tell Rune Priest trails apart from a co-op Regent's.
    public const string CardTrailPath = $"{MainFile.ResPath}/scenes/card_trail.tscn";

    private static readonly string BorrowedTrailPath = SceneHelper.GetScenePath("vfx/card_trail_regent");

    // The "Arise" epoch portrait ships with the base game, so it's referenced rather than bundled.
    private static readonly string SelectArtPath = ImageHelper.GetImagePath("timeline/epoch_portraits/relic5_epoch.png");

    private static PackedScene? _selectBackground;

    public static PackedScene? Resolve(string path) => path switch
    {
        SelectBackgroundPath => SelectBackground,
        CardTrailPath => PreloadManager.Cache.GetScene(BorrowedTrailPath),
        _ => null
    };

    /// <summary>Maps a path queued for preloading to what should actually be loaded (null to skip it).</summary>
    public static string? PreloadPath(string path) => path switch
    {
        SelectBackgroundPath => null,
        CardTrailPath => BorrowedTrailPath,
        _ => path
    };

    private static PackedScene SelectBackground
    {
        get
        {
            if (_selectBackground != null && GodotObject.IsInstanceValid(_selectBackground)) return _selectBackground;
            _selectBackground = BuildSelectBackground();
            return _selectBackground;
        }
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

using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RunePriest.RunePriestCode.Character;

/// <summary>
/// The Rune Priest borrows the Architect's Spine rig everywhere it appears (combat, rest site, merchant). The rig only
/// has idle_loop/attack/hurt and faces left, so callers mirror it and fall back to idle for missing animations.
/// </summary>
public static class ArchitectRig
{
    public static readonly string ScenePath = SceneHelper.GetScenePath("creature_visuals/architect");

    public const string IdleAnim = "idle_loop";
    public const string RelaxedAnim = "relaxed_loop";

    // Architect-scene units with the body mirrored. The scene's own %Bounds wraps the NPC's left-facing pose, so this
    // is a hand-fitted box around the mirrored body (~240x305 px at the 0.75 combat scale).
    public static readonly Rect2 BodyBounds = new(-140f, -410f, 320f, 410f);

    public static NCreatureVisuals InstantiateVisuals() =>
        PreloadManager.Cache.GetScene(ScenePath).Instantiate<NCreatureVisuals>();

    /// <summary>
    /// Detaches the mirrored SpineSprite body from a fresh Architect scene, scaled relative to the scene root. Its
    /// <c>NArchitectVfx</c> child comes along and starts <see cref="IdleAnim"/> once the skeleton is ready.
    /// </summary>
    public static Node2D TakeBody(float scale)
    {
        var visuals = InstantiateVisuals();
        var body = visuals.GetNode<Node2D>("%Visuals");
        visuals.RemoveChild(body);
        visuals.Free();

        body.UniqueNameInOwner = false;
        body.Scale = new Vector2(-body.Scale.X, body.Scale.Y) * scale;
        body.Position *= scale;
        return body;
    }

    /// <summary>Swaps <see cref="RelaxedAnim"/> for <see cref="IdleAnim"/> on rigs that lack it.</summary>
    public static string Fallback(MegaSprite sprite, string anim) =>
        anim == RelaxedAnim && !sprite.HasAnimation(anim) && sprite.HasAnimation(IdleAnim) ? IdleAnim : anim;
}

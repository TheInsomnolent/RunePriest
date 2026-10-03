using BaseLib.Abstracts;
using RunePriest.RunePriestCode.Extensions;
using Godot;

namespace RunePriest.RunePriestCode.Character;

/// <summary>
/// Card pool for Token cards — generated cards that appear as rewards (Curses, rune constructs, etc).
/// Visually distinct from the main card pool to signal their special origin.
/// </summary>
public class TokenCardPool : CustomCardPoolModel
{
    public override string Title => "Token"; // Not a display name; used for pool identification.
    
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    /// <summary>
    /// Token cards use a desaturated/darkened appearance to visually distinguish them from standard cards.
    /// HSV values are applied as a shader: this creates a muted, otherworldly look.
    /// </summary>
    public override float H => 0.85f;  // Slight hue shift (toward cool tones)
    public override float S => 0.6f;   // Reduced saturation (muted)
    public override float V => 0.85f;  // Slightly darker

    /// <summary>
    /// Small card icons in deck list use a grayscale tint to reinforce "generated/special" status.
    /// </summary>
    public override Color DeckEntryCardColor => new("aaaaaa");

    /// <summary>
    /// Token cards are colorless — they don't belong to the character's color identity.
    /// This ensures they stand out in deck lists and reward screens.
    /// </summary>
    public override bool IsColorless => true;
}

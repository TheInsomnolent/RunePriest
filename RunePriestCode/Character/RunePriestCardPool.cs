using BaseLib.Abstracts;
using RunePriest.RunePriestCode.Extensions;
using Godot;

namespace RunePriest.RunePriestCode.Character;

public class RunePriestCardPool : CustomCardPoolModel
{
    public override string Title => RunePriest.CharacterId; //This is not a display name.
    
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();


    // Card frames use LinenTheme.CardFrameMaterial (see LinenThemePatches), not BaseLib's HSV tint.

    //Color of small card icons
    public override Color DeckEntryCardColor => LinenTheme.Linen;
    public override Color EnergyOutlineColor => LinenTheme.Umber;
    
    public override bool IsColorless => false;
}
using BaseLib.Abstracts;
using RunePriest.RunePriestCode.Extensions;
using Godot;

namespace RunePriest.RunePriestCode.Character;

public class RunePriestPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => RunePriest.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
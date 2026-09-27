using BaseLib.Abstracts;
using BaseLib.Extensions;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// This is the base class for your mod's powers, which is set up to load the power's images from your mod's resources.
/// When creating a power, right click the Powers folder and create a new file with the Custom Power template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
public abstract class RunePriestPower : CustomPowerModel
{
    //Loads from RunePriest/images/powers/your_power.png
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    /// <summary>
    /// Whether this power is a buff or debuff.
    /// </summary>
    public abstract override PowerType Type { get; }
    
    /// <summary>
    /// How this power stacks if reapplied. Counter is the most common type, where applying the power again just
    /// adds to the amount. Single means the power does not stack, like Barricade. None functions identically to
    /// Single, but you're suggested to use Single as it is more explicit about how it will work.
    /// </summary>
    public abstract override PowerStackType StackType { get; }

    /// <summary>Concentration powers are removed when their owner loses HP.</summary>
    public virtual bool Concentration => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        Concentration ? [HoverTipFactory.FromKeyword(RunePriestKeywords.Concentration)] : [];

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (Concentration && creature == Owner && delta < 0)
        {
            MainFile.Logger.Info($"[Rune] {Id.Entry} lost Concentration");
            await PowerCmd.Remove(this);
        }
    }
}
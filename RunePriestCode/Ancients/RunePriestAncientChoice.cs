using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>Base class for RunePriest ancient choices.</summary>
public abstract class RunePriestAncientChoice
{
    public abstract string Id { get; }
    public abstract LocString Title { get; }
    public abstract LocString Description { get; }
    
    public abstract Task Apply(Player player);
    
    public virtual bool CanOffer(Player player) => true;
}

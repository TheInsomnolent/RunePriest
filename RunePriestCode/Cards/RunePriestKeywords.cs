using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace RunePriest.RunePriestCode.Cards;

public static class RunePriestKeywords
{
    /// <summary>The power this card grants is removed when its owner loses HP. See <c>RunePriestPower.Concentration</c>.</summary>
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Concentration;
}

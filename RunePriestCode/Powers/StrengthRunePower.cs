using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;
using RunePriest.RunePriestCode.Cards.Common;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Temporary Strength from the Strength rune: grants <see cref="StrengthPower"/> and removes it at the end of the
/// turn. A rune Spoken at end of turn therefore only empowers the runes Spoken after it.
/// </summary>
public sealed class StrengthRunePower : CustomTemporaryPowerModelWrapper<StrengthRuneCard, StrengthPower>;

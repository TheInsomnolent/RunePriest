using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Every 2nd rune Inscribed each turn has its values doubled.</summary>
public sealed class OddSigilPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int InscribedThisTurn
    {
        get => GetInternalData<TurnCounter>().Value;
        set => GetInternalData<TurnCounter>().Value = value;
    }

    protected override object InitInternalData() => new TurnCounter();

    public IReadOnlyList<Glyph> ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs, bool preview)
    {
        if (player != Owner.Player) return glyphs;

        var inscribed = InscribedThisTurn;
        var result = new List<Glyph>(glyphs.Count);
        foreach (var glyph in glyphs)
        {
            var doubled = ++inscribed % 2 == 0;
            if (doubled && !preview) Flash();
            result.Add(doubled ? glyph.Scaled(2) : glyph);
        }
        if (!preview) InscribedThisTurn = inscribed;
        return result;
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player) InscribedThisTurn = 0;
        return Task.CompletedTask;
    }
}


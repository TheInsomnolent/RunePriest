using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>The first two runes Inscribed each turn are wrapped in a closed Loop.</summary>
public sealed class RunicFormPower : RunePriestPower, IRuneListener
{
    public const int LoopCount = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int InscribedThisTurn
    {
        get => GetInternalData<TurnCounter>().Value;
        set => GetInternalData<TurnCounter>().Value = value;
    }

    protected override object InitInternalData() => new TurnCounter();

    public IReadOnlyList<Glyph> ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs)
    {
        if (player != Owner.Player || InscribedThisTurn >= 2) return glyphs;

        var result = new List<Glyph>();
        foreach (var glyph in glyphs)
        {
            if (InscribedThisTurn == 0) result.Add(Glyph.Of(new LoopRune(LoopCount)));
            result.Add(glyph);
            if (++InscribedThisTurn == 2)
            {
                result.Add(Glyph.Of(new EndLoopRune()));
                Flash();
            }
        }
        return result;
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player) InscribedThisTurn = 0;
        return Task.CompletedTask;
    }
}


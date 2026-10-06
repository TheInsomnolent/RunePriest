using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// The first two runes Inscribed each turn are wrapped in a closed Loop. Stacks: each Runic Form nests another Loop
/// around them (Amount Loops before the first rune, as many End Loops after the second).
/// </summary>
public sealed class RunicFormPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Per-turn progress; the Loops opened are remembered so the End Loops match even if Amount changes mid-turn.</summary>
    private sealed class TurnState
    {
        public int Inscribed { get; set; }
        public int OpenLoops { get; set; }
    }

    private TurnState State => GetInternalData<TurnState>();

    protected override object InitInternalData() => new TurnState();

    public IReadOnlyList<Glyph> ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs, bool preview)
    {
        var state = State;
        if (player != Owner.Player || state.Inscribed >= 2) return glyphs;

        var inscribed = state.Inscribed;
        var open = state.OpenLoops;
        var result = new List<Glyph>();
        foreach (var glyph in glyphs)
        {
            if (inscribed == 0)
            {
                open = Math.Max(1, Amount);
                for (var i = 0; i < open; i++) result.Add(Glyph.Of(new LoopRune()));
            }
            result.Add(glyph);
            if (++inscribed == 2)
            {
                for (var i = 0; i < open; i++) result.Add(Glyph.Of(new EndLoopRune()));
                open = 0;
                if (!preview) Flash();
            }
        }
        if (!preview)
        {
            state.Inscribed = inscribed;
            state.OpenLoops = open;
        }
        return result;
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return Task.CompletedTask;
        State.Inscribed = 0;
        State.OpenLoops = 0;
        return Task.CompletedTask;
    }
}

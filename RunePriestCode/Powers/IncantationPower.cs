using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Hosts the player's <see cref="RuneBuffer"/> and Speaks it at end of turn. Displays the glyph count.</summary>
public sealed class IncantationPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public RuneBuffer Buffer => GetInternalData<RuneBuffer>();

    private RuneBuffer? BufferOrNull => GetInternalData<RuneBuffer?>();

    public override int DisplayAmount => BufferOrNull?.Glyphs.Count ?? 0;

    protected override object InitInternalData()
    {
        var buffer = new RuneBuffer();
        buffer.Changed += InvokeDisplayAmountChanged;
        return buffer;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var glyphs = BufferOrNull?.Glyphs;
            if (glyphs == null || glyphs.Count == 0 || Owner.Player == null) return [];

            var script = string.Join("\n", glyphs.Select((g, i) => $"{i + 1}. {g.Label}"));
            var capacity = RuneListeners.Capacity(Owner.Player);
            if (capacity != null)
                script += "\n" + RuneTips.Capacity(glyphs.Count, capacity.Value);

            List<IHoverTip> tips = [new HoverTip(RuneTips.IncantationScriptTitle, script)];
            var forecast = RuneCmd.Forecast(Owner.Player);
            if (forecast is { IsEmpty: false })
                tips.Add(new HoverTip(RuneTips.ForecastTitle, RuneTips.FormatForecast(forecast)));
            return tips;
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner.Player == null || !participants.Contains(Owner)) return;
        await RuneCmd.Speak(choiceContext, Owner.Player, SpeakTiming.EndOfTurn);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Runes;

public static class RuneCmd
{
    /// <summary>Value added to each scalable payload of a glyph per Imbue.</summary>
    public const int ImbueBonus = 2;

    public static RuneBuffer? GetBuffer(Creature creature) => creature.GetPower<IncantationPower>()?.Buffer;

    public static async Task Inscribe(PlayerChoiceContext choiceContext, Player player, IEnumerable<Glyph> glyphs, CardModel? source)
    {
        IReadOnlyList<Glyph> list = glyphs.Select(g => g.Source == null ? g.WithSource(source) : g).ToList();
        if (list.Count == 0) return;

        var creature = player.Creature;
        var power = creature.GetPower<IncantationPower>()
                    ?? await PowerCmd.Apply<IncantationPower>(choiceContext, creature, 1, creature, source);
        if (power == null) return;

        foreach (var listener in RuneListeners.Of(player))
            list = listener.ModifyInscription(player, list);
        if (list.Count == 0) return;

        var buffer = power.Buffer;
        for (var i = 0; i < list.Count; i++)
        {
            // Only the first glyph of a cast merges; a card's own glyph sequence (e.g. Strike, Strike) stays separate.
            var last = buffer.Glyphs.Count > 0 ? buffer.Glyphs[^1] : null;
            if (i == 0 && !buffer.IsSpeaking && last?.MergeWith(list[i]) is { } merged)
            {
                buffer.Replace(buffer.Glyphs.Count - 1, merged);
                MainFile.Logger.Info($"[Rune] Merged {last} + {list[i]} -> {merged}");
            }
            else
            {
                buffer.Inscribe([list[i]]);
            }

            var capacity = RuneListeners.Capacity(player);
            // Overflow behaves like orb evoke: the oldest glyph is Spoken on its own, immediately.
            while (capacity != null && buffer.Glyphs.Count > capacity && !buffer.IsSpeaking)
                await SpeakAt(choiceContext, player, 0);
        }

        MainFile.Logger.Info($"[Rune] Inscribed {string.Join(" ", list)}");
        foreach (var listener in RuneListeners.Of(player))
            await listener.AfterInscribed(choiceContext, player, list);
    }

    /// <summary>
    /// Imbues up to <paramref name="count"/> glyphs (most recently inscribed first) that can be Imbued.
    /// </summary>
    /// <returns>How many glyphs were Imbued.</returns>
    public static async Task<int> Imbue(PlayerChoiceContext choiceContext, Player player, int count)
    {
        var buffer = GetBuffer(player.Creature);
        if (buffer == null || buffer.IsSpeaking || count <= 0) return 0;

        var indices = Enumerable.Range(0, buffer.Glyphs.Count).Reverse()
            .Where(i => buffer.Glyphs[i].CanImbue).Take(count).ToList();
        return await ImbueAt(choiceContext, player, buffer, indices);
    }

    /// <returns>How many glyphs were Imbued.</returns>
    public static async Task<int> ImbueAll(PlayerChoiceContext choiceContext, Player player)
    {
        var buffer = GetBuffer(player.Creature);
        if (buffer == null || buffer.IsSpeaking) return 0;

        var indices = Enumerable.Range(0, buffer.Glyphs.Count).Where(i => buffer.Glyphs[i].CanImbue).ToList();
        return await ImbueAt(choiceContext, player, buffer, indices);
    }

    private static async Task<int> ImbueAt(PlayerChoiceContext choiceContext, Player player, RuneBuffer buffer, List<int> indices)
    {
        var listeners = RuneListeners.Of(player);
        foreach (var index in indices)
        {
            var imbued = buffer.Glyphs[index].Imbue(ImbueBonus);
            buffer.Replace(index, imbued);
            MainFile.Logger.Info($"[Rune] Imbued #{index} -> {imbued}");
            foreach (var listener in listeners)
                await listener.AfterImbued(choiceContext, player, imbued);
        }
        return indices.Count;
    }

    /// <summary>Rewrites every glyph in place (no Imbue events). Used by Pacify, Thrumming Elixir.</summary>
    public static void Transform(Player player, Func<Glyph, Glyph> transform)
    {
        var buffer = GetBuffer(player.Creature);
        if (buffer == null || buffer.IsSpeaking) return;

        for (var i = 0; i < buffer.Glyphs.Count; i++)
        {
            var glyph = buffer.Glyphs[i];
            var changed = transform(glyph);
            if (!ReferenceEquals(glyph, changed)) buffer.Replace(i, changed);
        }
    }

    /// <summary>Removes a glyph without Speaking it. Index -1 removes the most recently inscribed glyph.</summary>
    public static async Task<Glyph?> Remove(PlayerChoiceContext choiceContext, Player player, int index = -1)
    {
        var buffer = GetBuffer(player.Creature);
        if (buffer == null || buffer.IsSpeaking || buffer.Glyphs.Count == 0) return null;

        var glyph = buffer.RemoveAt(index < 0 ? buffer.Glyphs.Count - 1 : index);
        if (glyph == null) return null;

        MainFile.Logger.Info($"[Rune] Removed {glyph}");
        foreach (var listener in RuneListeners.Of(player))
            await listener.AfterRemoved(choiceContext, player, glyph);
        return glyph;
    }

    /// <param name="keep">Keep the Incantation after speaking (also forced by any <see cref="IRuneListener.KeepsIncantation"/>).</param>
    public static async Task Speak(PlayerChoiceContext choiceContext, Player player, SpeakTiming timing, bool keep = false)
    {
        var buffer = GetBuffer(player.Creature);
        if (buffer == null || buffer.IsSpeaking || buffer.Glyphs.Count == 0) return;

        keep |= RuneListeners.Of(player).Any(l => l.KeepsIncantation);
        // Flag first so observers treat the TakeAll as "being spoken", not "erased".
        buffer.IsSpeaking = true;
        await SpeakGlyphs(choiceContext, player, buffer, buffer.TakeAll(), timing, keep);
    }

    /// <summary>Removes the glyph at <paramref name="index"/> and Speaks it alone, mid-turn.</summary>
    public static async Task SpeakAt(PlayerChoiceContext choiceContext, Player player, int index)
    {
        var buffer = GetBuffer(player.Creature);
        if (buffer == null || buffer.IsSpeaking) return;

        buffer.IsSpeaking = true;
        var glyph = buffer.RemoveAt(index);
        if (glyph == null)
        {
            buffer.IsSpeaking = false;
            return;
        }
        await SpeakGlyphs(choiceContext, player, buffer, [glyph], SpeakTiming.Invoked, keep: false);
    }

    /// <summary>Side-effect-free dry run of the current Incantation, for tooltips.</summary>
    public static RunePreview? Forecast(Player player)
    {
        var buffer = GetBuffer(player.Creature);
        if (buffer == null || buffer.Glyphs.Count == 0) return null;

        var preview = new RunePreview();
        var ctx = new RuneContext(new BlockingPlayerChoiceContext(), player, buffer, SpeakTiming.EndOfTurn, preview);
        var run = RuneInterpreter.Run(ctx, buffer.Glyphs.ToList());
        return run.IsCompletedSuccessfully ? preview : null;
    }

    private static async Task SpeakGlyphs(PlayerChoiceContext choiceContext, Player player, RuneBuffer buffer,
        IReadOnlyList<Glyph> glyphs, SpeakTiming timing, bool keep)
    {
        buffer.IsSpeaking = true;
        var ctx = new RuneContext(choiceContext, player, buffer, timing);
        try
        {
            IReadOnlyList<Glyph> retained;
            try
            {
                retained = await RuneInterpreter.Run(ctx, glyphs);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // A spell must never crash the run; treat unexpected errors as a total fizzle.
                MainFile.Logger.Error($"[Rune] Incantation collapsed: {e}");
                retained = [];
            }
            buffer.Retain(keep ? glyphs : retained);
        }
        finally
        {
            buffer.IsSpeaking = false;
            buffer.NotifySpeakEnded();
        }

        if (ctx.ShouldStop) return;
        foreach (var listener in RuneListeners.Of(player))
            await listener.AfterSpeak(ctx);
    }
}

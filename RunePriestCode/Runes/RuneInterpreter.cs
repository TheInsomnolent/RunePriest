using MegaCrit.Sts2.Core.Commands;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Speaks an Incantation left to right. Never throws on malformed programs: bad pieces fizzle and evaluation continues.
/// See docs/rune-system-design.md §5 for the semantics. Raises per-glyph events on <see cref="RuneContext.Buffer"/> for the UI.
/// </summary>
public static class RuneInterpreter
{
    public const int MaxPayloadExecutions = 60;
    public const int MaxSteps = 500;

    private sealed class LoopFrame(int start, int end, int remaining, List<ModifierRune> mods)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
        public int Remaining { get; set; } = remaining;
        public List<ModifierRune> Mods { get; } = mods;
    }

    /// <returns>Glyphs to keep for next turn (everything after a Seal).</returns>
    public static async Task<IReadOnlyList<Glyph>> Run(RuneContext ctx, IReadOnlyList<Glyph> glyphs)
    {
        var program = new RuneProgram(glyphs);
        var pending = new List<Glyph>();
        var loops = new List<LoopFrame>();
        int pc = 0, steps = 0, payloads = 0;

        Log(ctx, $"Speaking {glyphs.Count} glyph(s): {string.Join(" ", glyphs)}");

        while (!ctx.ShouldStop)
        {
            if (pc >= glyphs.Count)
            {
                if (loops.Count == 0) break;
                pc = CloseInnermostLoop(loops, pc);
                continue;
            }

            if (++steps > MaxSteps)
            {
                await Overload(ctx, glyphs, pc, "too many steps");
                return [];
            }

            var glyph = glyphs[pc];
            switch (glyph.Kind)
            {
                case null:
                    await Fizzle(ctx, pc, glyph, "malformed glyph");
                    pc++;
                    break;

                case RuneKind.Target:
                    await Activate(ctx, glyph);
                    ctx.ApplyTarget(((TargetRune)glyph.Runes[0]).Mode);
                    pc++;
                    break;

                case RuneKind.Modifier:
                    await Activate(ctx, glyph);
                    pending.Add(glyph);
                    pc++;
                    break;

                case RuneKind.Flow when glyph.Runes[0] is LoopRune loop:
                {
                    await Activate(ctx, glyph);
                    var end = program.MatchOf(pc);
                    var modGlyphs = TakeAll(pending);
                    var mods = AsModifiers(modGlyphs);
                    var count = ctx.Listeners.Aggregate(loop.Value, (c, l) => l.ModifyLoopCount(ctx, loop, c));
                    var iterations = count * (1 + mods.Sum(m => m.ExtraExecutions));
                    if (iterations <= 0)
                    {
                        await FizzlePending(ctx, modGlyphs, "loop never ran");
                        pc = end + 1;
                        break;
                    }
                    loops.Add(new LoopFrame(pc + 1, end, iterations, mods.Where(m => m.ExtraExecutions == 0).ToList()));
                    pc++;
                    break;
                }

                // Pending modifiers roll over to the next iteration's first glyph, or past the loop on the last one.
                case RuneKind.Flow when glyph.Runes[0] is EndLoopRune:
                    if (loops.Count == 0)
                    {
                        await Fizzle(ctx, pc, glyph, "no open loop");
                        pc++;
                    }
                    else
                    {
                        await Activate(ctx, glyph);
                        pc = CloseInnermostLoop(loops, pc);
                    }
                    break;

                case RuneKind.Flow when glyph.Runes[0] is SealRune:
                    await Activate(ctx, glyph);
                    await FizzlePending(ctx, pending, "sealed before it could apply");
                    var retained = glyphs.Skip(pc + 1).ToList();
                    Log(ctx, $"Sealed; retaining {retained.Count} glyph(s)");
                    return retained;

                case RuneKind.Payload:
                {
                    var mods = loops.SelectMany(f => f.Mods).Concat(AsModifiers(TakeAll(pending))).ToList();
                    var executions = 1 + mods.Sum(m => m.ExtraExecutions);
                    for (var i = 0; i < executions && !ctx.ShouldStop; i++)
                    {
                        if (++payloads > MaxPayloadExecutions)
                        {
                            await Overload(ctx, glyphs, pc, "too many payloads");
                            return [];
                        }
                        await Activate(ctx, glyph);
                        if (!await ExecutePayloadGlyph(ctx, glyph, mods))
                            await Fizzle(ctx, pc, glyph, "nothing resolved");
                    }
                    pc++;
                    break;
                }

                default:
                    await Fizzle(ctx, pc, glyph, "unknown rune");
                    pc++;
                    break;
            }
        }

        await FizzlePending(ctx, pending, "nothing left to empower");
        return [];
    }

    private static int CloseInnermostLoop(List<LoopFrame> loops, int pc)
    {
        var frame = loops[^1];
        if (--frame.Remaining > 0) return frame.Start;
        loops.RemoveAt(loops.Count - 1);
        return Math.Max(pc, frame.End) + 1;
    }

    private static async Task Activate(RuneContext ctx, Glyph glyph)
    {
        if (ctx.IsPreview) return;
        ctx.MarkSpoken(glyph);
        ctx.Buffer.NotifyActivated(glyph);
        // Brief beat so each glyph reads visually, even ones with no game animation (targets, modifiers, flow).
        await Cmd.CustomScaledWait(0.05f, 0.2f);
    }

    /// <returns>Whether any rune in the glyph resolved (or would, in preview).</returns>
    private static async Task<bool> ExecutePayloadGlyph(RuneContext ctx, Glyph glyph, List<ModifierRune> mods)
    {
        var resolved = false;
        foreach (var rune in glyph.Runes.Cast<PayloadRune>())
        {
            var value = ctx.Listeners.Aggregate(rune.Value, (v, l) => l.ModifyRuneValue(ctx, rune, v));
            if (rune.Scalable)
                value = mods.Aggregate(value, (v, m) => m.Apply(rune, v));
            if (value <= 0) continue;

            if (ctx.Preview != null)
            {
                // Preview never resolves targets: that would advance the shared combat RNG.
                ctx.Preview.Record(rune, value);
                resolved = true;
                continue;
            }

            var targets = ctx.ResolveTargets(rune, glyph);
            if (targets.Count == 0)
            {
                Log(ctx, $"  {rune} fizzles: no target");
                continue;
            }

            Log(ctx, $"  {rune.Key} {value} -> {string.Join(", ", targets.Select(t => t.ToString()))}");
            await rune.Resolve(ctx, glyph, value, targets);
            resolved = true;
            foreach (var listener in ctx.Listeners)
                await listener.AfterPayload(ctx, rune, value, targets);
            if (ctx.ShouldStop) break;
        }
        return resolved;
    }

    private static List<Glyph> TakeAll(List<Glyph> pending)
    {
        var taken = pending.ToList();
        pending.Clear();
        return taken;
    }

    private static List<ModifierRune> AsModifiers(List<Glyph> glyphs) => glyphs.Select(g => (ModifierRune)g.Runes[0]).ToList();

    private static async Task FizzlePending(RuneContext ctx, List<Glyph> pending, string reason)
    {
        var fizzled = TakeAll(pending);
        for (var i = 0; i < fizzled.Count; i++)
            await Fizzle(ctx, -1, fizzled[i], reason);
    }

    private static async Task Fizzle(RuneContext ctx, int index, Glyph glyph, string reason)
    {
        if (ctx.Preview != null)
        {
            ctx.Preview.Fizzles++;
            return;
        }

        Log(ctx, $"  #{index} {glyph} fizzles: {reason}");
        ctx.Fizzles++;
        ctx.Buffer.NotifyFizzled(glyph);
        foreach (var listener in ctx.Listeners)
            await listener.AfterFizzle(ctx, glyph);
    }

    private static async Task Overload(RuneContext ctx, IReadOnlyList<Glyph> glyphs, int pc, string reason)
    {
        Log(ctx, $"Overload: {reason}, the rest fizzles");
        if (ctx.Preview != null) ctx.Preview.Overloaded = true;
        foreach (var glyph in glyphs.Skip(pc))
            await Fizzle(ctx, -1, glyph, "overload");
    }

    private static void Log(RuneContext ctx, string message)
    {
        if (!ctx.IsPreview) MainFile.Logger.Info("[Rune] " + message);
    }
}

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

    /// <returns>Glyphs to keep for next turn (Persist glyphs, Growth carry-overs and everything after a Seal).</returns>
    public static async Task<IReadOnlyList<Glyph>> Run(RuneContext ctx, IReadOnlyList<Glyph> glyphs)
    {
        var program = new RuneProgram(glyphs);
        var pending = new List<Glyph>();
        var loops = new List<LoopFrame>();
        // Persist glyphs and Growth carry-overs, in encounter order; returned so the buffer retains them.
        var kept = new List<Glyph>();
        // How many glyphs later loop iterations skip per Growth glyph already handled this Speak.
        var growthSkip = new Dictionary<Glyph, int>();
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
                return kept;
            }

            var glyph = glyphs[pc];
            switch (glyph.Kind)
            {
                case null:
                    await Fizzle(ctx, pc, glyph, "malformed glyph");
                    pc++;
                    break;

                case RuneKind.Target:
                    await Activate(ctx, glyph, kept);
                    ctx.ApplyTarget(((TargetRune)glyph.Runes[0]).Mode);
                    pc++;
                    break;

                // Growth delays the following glyph: each Speak it ticks down once and keeps the glyph for next
                // turn, doubled, instead of resolving it. Fully ticked down, the Growth vanishes and it resolves.
                case RuneKind.Modifier when glyph.Runes[0] is GrowthRune growth:
                {
                    if (growthSkip.TryGetValue(glyph, out var skip))
                    {
                        // Later loop iterations: this Growth was already handled this Speak.
                        pc += skip;
                        break;
                    }
                    if (pc + 1 >= glyphs.Count)
                    {
                        await Fizzle(ctx, pc, glyph, "nothing to grow");
                        pc++;
                        break;
                    }
                    await Activate(ctx, glyph, kept);
                    if (growth.Value <= 1)
                    {
                        Log(ctx, $"  {glyph} is fully grown; {glyphs[pc + 1]} resolves");
                        growthSkip[glyph] = 1;
                        pc++;
                        break;
                    }
                    var grown = glyphs[pc + 1].Scaled(2);
                    kept.Add(glyph.WithRunes(new GrowthRune(growth.Value - 1)));
                    kept.Add(grown);
                    Log(ctx, $"  {glyph} ticks down; keeping {grown} for next turn");
                    growthSkip[glyph] = 2;
                    pc += 2;
                    break;
                }

                // Reflection: the Speak turns around — earlier glyphs are Spoken again in reverse order, and
                // everything after the Reflection is never Spoken.
                case RuneKind.Modifier when glyph.Runes[0] is ReflectionRune:
                {
                    await Activate(ctx, glyph, kept);
                    IReadOnlyList<Glyph> reflected = glyphs.Take(pc).Reverse().ToList();
                    Log(ctx, $"  {glyph} reflects; speaking {reflected.Count} glyph(s) in reverse, dropping the rest");
                    glyphs = reflected;
                    program = new RuneProgram(glyphs);
                    loops.Clear();
                    pc = 0;
                    break;
                }

                // Friendship: supportive runes after this one reach every player (Value > 0: for the whole Speak).
                case RuneKind.Modifier when glyph.Runes[0] is FriendshipRune friendship:
                    await Activate(ctx, glyph, kept);
                    ctx.ApplyFriendship(friendship.Value > 0 ? FriendshipScope.Rest : FriendshipScope.NextGlyph);
                    pc++;
                    break;

                case RuneKind.Modifier:
                    await Activate(ctx, glyph, kept);
                    pending.Add(glyph);
                    pc++;
                    break;

                case RuneKind.Flow when glyph.Runes[0] is LoopRune loop:
                {
                    await Activate(ctx, glyph, kept);
                    var end = program.MatchOf(pc);
                    var modGlyphs = TakeAll(pending);
                    var mods = AsModifiers(modGlyphs);
                    // Loop N runs the body once plus N extra times. Modifiers never change N (Loop isn't amplifiable).
                    var count = ctx.Listeners.Aggregate(loop.Value, (c, l) => l.ModifyLoopCount(ctx, loop, c));
                    var iterations = (count + 1) * (1 + mods.Sum(m => m.ExtraExecutions));
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
                        await Activate(ctx, glyph, kept);
                        pc = CloseInnermostLoop(loops, pc);
                    }
                    break;

                case RuneKind.Flow when glyph.Runes[0] is SealRune:
                    await Activate(ctx, glyph, kept);
                    await FizzlePending(ctx, pending, "sealed before it could apply");
                    var retained = glyphs.Skip(pc + 1).ToList();
                    Log(ctx, $"Sealed; retaining {retained.Count} glyph(s)");
                    return [..kept, ..retained];

                case RuneKind.Payload:
                {
                    var mods = loops.SelectMany(f => f.Mods).Concat(AsModifiers(TakeAll(pending))).ToList();
                    var executions = 1 + mods.Sum(m => m.ExtraExecutions);
                    for (var i = 0; i < executions && !ctx.ShouldStop; i++)
                    {
                        if (++payloads > MaxPayloadExecutions)
                        {
                            await Overload(ctx, glyphs, pc, "too many payloads");
                            return kept;
                        }
                        if (mods.Any(m => m.Voids))
                        {
                            await Fizzle(ctx, pc, glyph, "voided");
                            continue;
                        }
                        await Activate(ctx, glyph, kept);
                        if (!await ExecutePayloadGlyph(ctx, glyph, mods))
                            await Fizzle(ctx, pc, glyph, "nothing resolved");
                    }
                    ctx.ConsumeFriendship();
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
        return kept;
    }

    private static int CloseInnermostLoop(List<LoopFrame> loops, int pc)
    {
        var frame = loops[^1];
        if (--frame.Remaining > 0) return frame.Start;
        loops.RemoveAt(loops.Count - 1);
        return Math.Max(pc, frame.End) + 1;
    }

    private static async Task Activate(RuneContext ctx, Glyph glyph, List<Glyph> kept)
    {
        // Persist glyphs stay in the Incantation after being Spoken (Growth glyphs manage their own carry-over).
        if (glyph.Persistent && glyph.Runes[0] is not GrowthRune && !kept.Contains(glyph)) kept.Add(glyph);
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
            value = mods.Aggregate(value, (v, m) => m.ApplyTo(rune, v));
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
        // Persist modifiers don't fizzle when nothing follows: they stay in the Incantation instead (Dark Star's Void).
        var fizzled = TakeAll(pending).Where(g => !g.Persistent).ToList();
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

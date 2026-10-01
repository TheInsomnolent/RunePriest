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

    private sealed class GrowthState(int remaining)
    {
        public int Remaining { get; set; } = remaining;
        /// <summary>Fully ticked down this Speak: the delayed glyph now resolves normally.</summary>
        public bool Resolved { get; set; }
        public Glyph? KeptGrowth { get; set; }
        public Glyph? KeptGrown { get; set; }
    }

    /// <returns>Glyphs to keep for next turn (Persist glyphs, Growth carry-overs and everything after a Seal).</returns>
    public static async Task<IReadOnlyList<Glyph>> Run(RuneContext ctx, IReadOnlyList<Glyph> glyphs)
    {
        // Carry-over glyph -> the inscribed glyph it came from (unmapped kept glyphs are the inscribed ones).
        var origins = new Dictionary<Glyph, Glyph>();
        var kept = await Run(ctx, glyphs, origins);
        ctx.Preview?.Persisting.UnionWith(kept.Select(k => origins.GetValueOrDefault(k, k)));
        return kept;
    }

    private static async Task<IReadOnlyList<Glyph>> Run(RuneContext ctx, IReadOnlyList<Glyph> glyphs, Dictionary<Glyph, Glyph> origins)
    {
        var program = new RuneProgram(glyphs);
        var pending = new List<Glyph>();
        var loops = new List<LoopFrame>();
        // Persist glyphs and Growth carry-overs, in encounter order; returned so the buffer retains them.
        var kept = new List<Glyph>();
        // Per-Speak tick state of every Growth glyph (Growth ticks down on every trigger, so loops tick it repeatedly).
        var growthStates = new Dictionary<Glyph, GrowthState>();
        // Current halved carry-over per Diminish glyph; null once it has diminished away.
        var diminished = new Dictionary<Glyph, Glyph?>();
        // The last target glyph, until a payload uses it: a target immediately replaced (or never used) fizzles.
        Glyph? unusedTarget = null;
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
                    if (unusedTarget != null && unusedTarget != glyph && !unusedTarget.Persistent)
                        await Fizzle(ctx, pc, unusedTarget, "target never used");
                    unusedTarget = glyph;
                    await Activate(ctx, glyph, kept);
                    ctx.ApplyTarget(((TargetRune)glyph.Runes[0]).Mode);
                    pc++;
                    break;

                // Growth delays the following glyph: it ticks down once on every trigger (so a Loop ticks it once
                // per iteration) and keeps the glyph for next turn, doubled, instead of resolving it. Once ticked
                // down to 0 the Growth vanishes, and the glyph resolves on its next trigger.
                case RuneKind.Modifier when glyph.Runes[0] is GrowthRune growth:
                {
                    if (pc + 1 >= glyphs.Count)
                    {
                        await Fizzle(ctx, pc, glyph, "nothing to grow");
                        pc++;
                        break;
                    }
                    if (!growthStates.TryGetValue(glyph, out var state))
                        growthStates[glyph] = state = new GrowthState(growth.Value);
                    if (state.Resolved)
                    {
                        // Fully grown earlier this Speak: the delayed glyph resolves on this trigger too.
                        pc++;
                        break;
                    }
                    await Activate(ctx, glyph, kept);
                    if (state.Remaining <= 0)
                    {
                        // Ticked down earlier this Speak (a Loop): it resolves now instead of waiting for next turn.
                        state.Resolved = true;
                        if (state.KeptGrowth != null) kept.Remove(state.KeptGrowth);
                        if (state.KeptGrown != null) kept.Remove(state.KeptGrown);
                        state.KeptGrowth = state.KeptGrown = null;
                        Log(ctx, $"  {glyph} is fully grown; {glyphs[pc + 1]} resolves");
                        pc++;
                        break;
                    }
                    state.Remaining--;
                    var ticked = state.Remaining > 0 ? glyph.WithRunes(new GrowthRune(state.Remaining)) : null;
                    if (ticked != null) origins[ticked] = glyph;
                    if (state.KeptGrown == null)
                    {
                        state.KeptGrown = glyphs[pc + 1].Scaled(2);
                        origins[state.KeptGrown] = glyphs[pc + 1];
                        if (ticked != null) kept.Add(ticked);
                        kept.Add(state.KeptGrown);
                    }
                    else if (state.KeptGrowth != null)
                    {
                        if (ticked != null) kept[kept.IndexOf(state.KeptGrowth)] = ticked;
                        else kept.Remove(state.KeptGrowth);
                    }
                    state.KeptGrowth = ticked;
                    Log(ctx, $"  {glyph} ticks down to {state.Remaining}; keeping {state.KeptGrown} for next turn");
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

                // Clone: every trigger keeps a persistent copy of the following glyph for next turn.
                case RuneKind.Modifier when glyph.Runes[0] is CloneRune:
                    if (pc + 1 >= glyphs.Count)
                    {
                        if (!glyph.Persistent) await Fizzle(ctx, pc, glyph, "nothing to clone");
                        else await Activate(ctx, glyph, kept);
                        pc++;
                        break;
                    }
                    await Activate(ctx, glyph, kept);
                    var clone = glyphs[pc + 1].Copy().Persist();
                    origins[clone] = glyphs[pc + 1];
                    kept.Add(clone);
                    Log(ctx, $"  {glyph} clones {glyphs[pc + 1]}; the copy persists into next turn");
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
                    if (unusedTarget is { Persistent: false })
                        await Fizzle(ctx, -1, unusedTarget, "target never used");
                    var retained = glyphs.Skip(pc + 1).ToList();
                    Log(ctx, $"Sealed; retaining {retained.Count} glyph(s)");
                    return [..kept, ..retained];

                case RuneKind.Payload:
                {
                    // A Diminish glyph that already diminished away this Speak (looped) fizzles instead of resolving.
                    if (diminished.TryGetValue(glyph, out var carried) && carried == null)
                    {
                        await Fizzle(ctx, pc, glyph, "diminished away");
                        pc++;
                        break;
                    }
                    var mods = loops.SelectMany(f => f.Mods).Concat(AsModifiers(TakeAll(pending))).ToList();
                    var executions = 1 + mods.Sum(m => m.ExtraExecutions);
                    var voided = mods.Any(m => m.Voids);
                    for (var i = 0; i < executions && !ctx.ShouldStop; i++)
                    {
                        if (++payloads > MaxPayloadExecutions)
                        {
                            await Overload(ctx, glyphs, pc, "too many payloads");
                            return kept;
                        }
                        if (voided)
                        {
                            await Fizzle(ctx, pc, glyph, "voided");
                            continue;
                        }
                        await Activate(ctx, glyph, kept);
                        if (!await ExecutePayloadGlyph(ctx, glyph, mods))
                            await Fizzle(ctx, pc, glyph, "nothing resolved");
                    }
                    unusedTarget = null;
                    // Diminish persists with its value halved; below the threshold it fizzles away instead.
                    if (!voided && glyph.Runes.Any(r => r is DiminishRune))
                    {
                        var basis = carried ?? glyph;
                        var halved = basis.WithRunes(basis.Runes
                            .Select(r => r is DiminishRune ? new DiminishRune(r.Value / 2) : r).ToArray()).Persist();
                        if (halved.Runes.OfType<DiminishRune>().All(r => r.Value < DiminishRune.FizzleThreshold))
                        {
                            if (carried != null) kept.Remove(carried);
                            diminished[glyph] = null;
                            await Fizzle(ctx, pc, glyph, "diminished away");
                        }
                        else
                        {
                            origins[halved] = glyph;
                            if (carried != null) kept[kept.IndexOf(carried)] = halved;
                            else kept.Add(halved);
                            diminished[glyph] = halved;
                            Log(ctx, $"  {glyph} diminishes; keeping {halved} for next turn");
                        }
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
        if (unusedTarget is { Persistent: false })
            await Fizzle(ctx, -1, unusedTarget, "target never used");
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
        // Persist glyphs stay in the Incantation after being Spoken (Growth and Diminish manage their own carry-over).
        if (glyph.Persistent && glyph.Runes[0] is not GrowthRune && !glyph.Runes.Any(r => r is DiminishRune) &&
            !kept.Contains(glyph)) kept.Add(glyph);
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
        for (var i = 0; i < glyph.Runes.Count; i++)
        {
            var inscribed = (PayloadRune)glyph.Runes[i];
            var rune = ctx.Listeners.Aggregate(inscribed, (r, l) => l.ReplacePayload(ctx, r));
            var value = ctx.Listeners.Aggregate(rune.Value, (v, l) => l.ModifyRuneValue(ctx, rune, v));
            value = mods.Aggregate(value, (v, m) => m.ApplyTo(rune, v));

            if (ctx.Preview != null)
            {
                // Preview never resolves targets: that would advance the shared combat RNG.
                var target = ctx.PreviewTarget(rune, glyph);
                ctx.Preview.Show(glyph, i, inscribed.Modified(ctx.Owner, glyph, target, inscribed.Value));
                if (value <= 0) continue;
                ctx.Preview.Record(rune, value, rune.Modified(ctx.Owner, glyph, target, value));
                resolved = true;
                continue;
            }
            if (value <= 0) continue;

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

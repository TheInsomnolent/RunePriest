using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Speaks an Incantation left to right. Never throws on malformed programs: bad pieces fizzle and evaluation continues.
/// See docs/rune-system-design.md §5 for the semantics. Raises per-glyph events on <see cref="RuneContext.Buffer"/> for the UI.
/// </summary>
public static class RuneInterpreter
{
    public const int MaxPayloadExecutions = 60;
    public const int MaxSteps = 500;

    private static readonly StrikeRune TargetProbe = new(1);

    private sealed class LoopFrame(int start, int end, int remaining, List<ModifierRune> mods)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
        public int Remaining { get; set; } = remaining;
        public List<ModifierRune> Mods { get; } = mods;
    }

    /// <summary>
    /// One inscribed glyph's live state for a Speak. Void, Growth and Diminish rewrite slots as they run, so later
    /// loop passes and a Reflection see the changed Incantation.
    /// </summary>
    private sealed class Slot(Glyph glyph)
    {
        /// <summary>The inscribed glyph; the overhead UI and the forecast are keyed by it.</summary>
        public Glyph Origin { get; } = glyph;
        public Glyph Current { get; set; } = glyph;
        /// <summary>Voided, diminished away or a fully grown Growth: skipped for the rest of the Speak and never kept.</summary>
        public bool Consumed { get; set; }
        public bool Spoken { get; set; }
        /// <summary>Kept for next turn whatever its Persist state (ticking Growth, grown glyph, Diminish).</summary>
        public bool Carry { get; set; }
        /// <summary>The Growth delaying this glyph; while it lives, reaching this glyph does nothing.</summary>
        public Slot? GrownBy { get; set; }
        /// <summary>Persistent copies made by this Clone (with the inscribed glyph they copy), kept right after it.</summary>
        public List<(Glyph Glyph, Glyph Origin)> Clones { get; } = [];
    }

    /// <returns>Glyphs to keep for next turn (Persist glyphs, Growth/Diminish carry-overs and everything after a Seal).</returns>
    public static async Task<IReadOnlyList<Glyph>> Run(RuneContext ctx, IReadOnlyList<Glyph> glyphs)
    {
        var kept = await Run(ctx, glyphs.Select(g => new Slot(g)).ToList());
        ctx.Preview?.Persisting.UnionWith(kept.Select(k => k.Origin));
        return kept.Select(k => k.Glyph).ToList();
    }

    private static async Task<List<(Glyph Glyph, Glyph Origin)>> Run(RuneContext ctx, List<Slot> slots)
    {
        // The order being Spoken; a Reflection swaps in the earlier slots reversed (same slot objects).
        var tape = slots;
        var program = new RuneProgram(tape.Select(s => s.Current).ToList());
        var pending = new List<Slot>();
        var loops = new List<LoopFrame>();
        // The last target glyph, until a payload uses it: a target immediately replaced (or never used) fizzles.
        Slot? unusedTarget = null;
        int pc = 0, steps = 0, payloads = 0;

        Log(ctx, $"Speaking {slots.Count} glyph(s): {string.Join(" ", slots.Select(s => s.Current))}");

        while (!ctx.ShouldStop)
        {
            if (pc >= tape.Count)
            {
                if (loops.Count == 0) break;
                pc = CloseInnermostLoop(loops, pc);
                continue;
            }

            if (++steps > MaxSteps)
            {
                await Overload(ctx, tape, pc, "too many steps");
                return Kept(slots);
            }

            var slot = tape[pc];
            if (slot.Consumed)
            {
                pc++;
                continue;
            }
            var glyph = slot.Current;

            // A pending Void consumes the next glyph whatever it is (another Void included); targets and End Loops
            // are transparent. The Void stays, so on a later loop pass or Reflection it consumes again.
            if (!IsTransparentToVoid(glyph) && TakeVoid(pending) is { } voider)
            {
                slot.Consumed = true;
                Log(ctx, $"  {voider.Current} consumes {glyph}");
                await Fizzle(ctx, pc, slot, "voided");
                pc++;
                continue;
            }

            if (slot.GrownBy != null)
            {
                // Still growing (e.g. reached first after a Reflection): it waits.
                if (!slot.GrownBy.Consumed)
                {
                    pc++;
                    continue;
                }
                // Its Growth vanished earlier this Speak: it resolves now instead of next turn.
                slot.GrownBy = null;
                slot.Carry = false;
            }

            switch (glyph.Kind)
            {
                case null:
                    await Fizzle(ctx, pc, slot, "malformed glyph");
                    pc++;
                    break;

                case RuneKind.Target:
                    if (unusedTarget != null && unusedTarget != slot && !unusedTarget.Current.Persistent)
                        await Fizzle(ctx, pc, unusedTarget, "target never used");
                    unusedTarget = slot;
                    await Activate(ctx, slot);
                    ctx.ApplyTarget(((TargetRune)glyph.Runes[0]).Mode);
                    if (ctx.Preview != null)
                    {
                        // What the mode would pick for an enemy rune (hover highlight).
                        var (picked, random) = ctx.PreviewTargets(TargetProbe, glyph);
                        ctx.Preview.Target(slot.Origin, picked, random);
                    }
                    pc++;
                    break;

                // Growth delays the following glyph: every trigger (so every loop pass) ticks it down and doubles that
                // glyph in place (Overgrowth: triples), keeping it for next turn instead of resolving it. Ticked down
                // to 0 the Growth vanishes, and the glyph resolves on its next trigger.
                case RuneKind.Modifier when glyph.Runes[0] is GrowingRune growth:
                {
                    if (growth.Value <= 0)
                    {
                        slot.Consumed = true;
                        pc++;
                        break;
                    }
                    var next = NextLive(tape, pc + 1);
                    if (next < 0 || IsEndLoop(tape[next].Current))
                    {
                        await Fizzle(ctx, pc, slot, "nothing to grow");
                        pc++;
                        break;
                    }
                    await Activate(ctx, slot);
                    var grown = tape[next];
                    grown.Current = grown.Current.Scaled(growth.Factor);
                    grown.GrownBy = slot;
                    grown.Carry = true;
                    if (growth.Value > 1)
                    {
                        slot.Current = glyph.WithRunes(growth.WithValue(growth.Value - 1)!);
                        slot.Carry = true;
                    }
                    else
                    {
                        slot.Consumed = true;
                    }
                    Log(ctx, $"  {glyph} ticks down to {growth.Value - 1}; {grown.Current} keeps growing");
                    pc = next + 1;
                    break;
                }

                // Reflection: the Speak turns around — earlier glyphs (as they stand now) are Spoken again in reverse
                // order, unfinished loops stop looping, and everything after the Reflection is never Spoken.
                case RuneKind.Modifier when glyph.Runes[0] is ReflectionRune:
                {
                    await Activate(ctx, slot);
                    tape = tape.Take(pc).Where(s => !s.Consumed).Reverse().ToList();
                    Log(ctx, $"  {glyph} reflects; speaking {tape.Count} glyph(s) in reverse, dropping the rest");
                    program = new RuneProgram(tape.Select(s => s.Current).ToList());
                    loops.Clear();
                    pc = 0;
                    break;
                }

                // Friendship: supportive runes after this one reach every player (Value > 0: for the whole Speak).
                case RuneKind.Modifier when glyph.Runes[0] is FriendshipRune friendship:
                    await Activate(ctx, slot);
                    ctx.ApplyFriendship(friendship.Value > 0 ? FriendshipScope.Rest : FriendshipScope.NextGlyph);
                    pc++;
                    break;

                // Clone: every trigger keeps a persistent copy of the following glyph for next turn.
                case RuneKind.Modifier when glyph.Runes[0] is CloneRune:
                {
                    var next = NextLive(tape, pc + 1);
                    if (next < 0)
                    {
                        if (!glyph.Persistent) await Fizzle(ctx, pc, slot, "nothing to clone");
                        else await Activate(ctx, slot);
                        pc++;
                        break;
                    }
                    await Activate(ctx, slot);
                    var source = tape[next];
                    slot.Clones.Add((source.Current.Copy().Persist(), source.Origin));
                    Log(ctx, $"  {glyph} clones {source.Current}; the copy persists into next turn");
                    pc++;
                    break;
                }

                case RuneKind.Modifier:
                    await Activate(ctx, slot);
                    pending.Add(slot);
                    pc++;
                    break;

                case RuneKind.Flow when glyph.Runes[0] is LoopRune:
                {
                    await Activate(ctx, slot);
                    var end = program.MatchOf(pc);
                    var mods = AsModifiers(TakeAll(pending));
                    // A Loop runs its body twice; each Echo before it adds another two passes.
                    var iterations = 2 * (1 + mods.Sum(m => m.ExtraExecutions));
                    loops.Add(new LoopFrame(pc + 1, end, iterations, mods.Where(m => m.ExtraExecutions == 0).ToList()));
                    pc++;
                    break;
                }

                // Pending modifiers roll over to the next iteration's first glyph, or past the loop on the last one.
                // The End Loop of a consumed (voided) Loop stays and fizzles.
                case RuneKind.Flow when glyph.Runes[0] is EndLoopRune:
                {
                    var opener = program.MatchOf(pc);
                    if (loops.Count == 0 || opener == RuneProgram.StrayEnd || tape[opener].Consumed)
                    {
                        await Fizzle(ctx, pc, slot, "no open loop");
                        pc++;
                    }
                    else
                    {
                        await Activate(ctx, slot);
                        pc = CloseInnermostLoop(loops, pc);
                    }
                    break;
                }

                case RuneKind.Flow when glyph.Runes[0] is SealRune:
                {
                    await Activate(ctx, slot);
                    await FizzlePending(ctx, pending, "sealed before it could apply");
                    if (unusedTarget is { Current.Persistent: false })
                        await Fizzle(ctx, -1, unusedTarget, "target never used");
                    var retained = tape.Skip(pc + 1).Where(s => !s.Consumed).ToList();
                    Log(ctx, $"Sealed; retaining {retained.Count} glyph(s)");
                    return [..Kept(slots, retained), ..retained.Select(s => (s.Current, s.Origin))];
                }

                case RuneKind.Payload:
                {
                    var mods = loops.SelectMany(f => f.Mods).Concat(AsModifiers(TakeAll(pending))).ToList();
                    var executions = 1 + mods.Sum(m => m.ExtraExecutions);
                    for (var i = 0; i < executions && !slot.Consumed && !ctx.ShouldStop; i++)
                    {
                        if (++payloads > MaxPayloadExecutions)
                        {
                            await Overload(ctx, tape, pc, "too many payloads");
                            return Kept(slots);
                        }
                        await Activate(ctx, slot);
                        if (!await ExecutePayloadGlyph(ctx, slot, mods, slots))
                            await Fizzle(ctx, pc, slot, "nothing resolved");
                        await Diminish(ctx, pc, slot);
                    }
                    unusedTarget = null;
                    ctx.ConsumeFriendship();
                    pc++;
                    break;
                }

                default:
                    await Fizzle(ctx, pc, slot, "unknown rune");
                    pc++;
                    break;
            }
        }

        await FizzlePending(ctx, pending, "nothing left to empower");
        if (unusedTarget is { Current.Persistent: false })
            await Fizzle(ctx, -1, unusedTarget, "target never used");
        return Kept(slots);
    }

    /// <summary>Diminish halves in place after every execution, so later executions, loop passes and Reflections hit for less.</summary>
    private static async Task Diminish(RuneContext ctx, int pc, Slot slot)
    {
        var glyph = slot.Current;
        if (slot.Consumed || !glyph.Runes.Any(r => r is DiminishRune)) return;

        var halved = glyph.WithRunes(glyph.Runes
            .Select(r => r is DiminishRune ? new DiminishRune(r.Value / 2) : r).ToArray()).Persist();
        if (halved.Runes.OfType<DiminishRune>().All(r => r.Value < DiminishRune.FizzleThreshold))
        {
            slot.Consumed = true;
            await Fizzle(ctx, pc, slot, "diminished away");
            return;
        }
        slot.Current = halved;
        slot.Carry = true;
        Log(ctx, $"  {glyph} diminishes to {halved}");
    }

    /// <summary>Surviving slots that outlast the Speak, in inscription order; each Clone's copies follow it.</summary>
    private static List<(Glyph Glyph, Glyph Origin)> Kept(List<Slot> slots, ICollection<Slot>? exclude = null)
    {
        var kept = new List<(Glyph, Glyph)>();
        foreach (var slot in slots)
        {
            if (!slot.Consumed && (slot.Carry || (slot.Spoken && slot.Current.Persistent)) && exclude?.Contains(slot) != true)
                kept.Add((slot.Current, slot.Origin));
            kept.AddRange(slot.Clones);
        }
        return kept;
    }

    private static int NextLive(List<Slot> tape, int from)
    {
        for (var i = from; i < tape.Count; i++)
            if (!tape[i].Consumed) return i;
        return -1;
    }

    private static bool IsEndLoop(Glyph glyph) => glyph.Kind == RuneKind.Flow && glyph.Runes[0] is EndLoopRune;

    private static bool IsTransparentToVoid(Glyph glyph) => glyph.Kind == RuneKind.Target || IsEndLoop(glyph);

    private static Slot? TakeVoid(List<Slot> pending)
    {
        var index = pending.FindIndex(s => s.Current.Runes[0] is ModifierRune { Voids: true });
        if (index < 0) return null;
        var voider = pending[index];
        pending.RemoveAt(index);
        return voider;
    }

    private static int CloseInnermostLoop(List<LoopFrame> loops, int pc)
    {
        var frame = loops[^1];
        if (--frame.Remaining > 0) return frame.Start;
        loops.RemoveAt(loops.Count - 1);
        return Math.Max(pc, frame.End) + 1;
    }

    private static async Task Activate(RuneContext ctx, Slot slot)
    {
        // Spoken Persist glyphs stay in the Incantation (see Kept).
        slot.Spoken = true;
        if (ctx.IsPreview) return;
        ctx.MarkSpoken(slot.Origin);
        ctx.Buffer.NotifyActivated(slot.Origin);
        // Brief beat so each glyph reads visually, even ones with no game animation (targets, modifiers, flow).
        await Cmd.CustomScaledWait(0.05f, 0.2f);
    }

    /// <returns>Whether any rune in the glyph resolved (or would, in preview).</returns>
    private static async Task<bool> ExecutePayloadGlyph(RuneContext ctx, Slot slot, List<ModifierRune> mods, List<Slot> slots)
    {
        var glyph = slot.Current;
        var resolved = false;
        for (var i = 0; i < glyph.Runes.Count; i++)
        {
            var inscribed = (PayloadRune)glyph.Runes[i];
            // A Cleanse earlier in this glyph already removed it.
            if (inscribed is BloodRune && !slot.Current.Runes.Contains(inscribed)) continue;
            var rune = ctx.Listeners.Aggregate(inscribed, (r, l) => l.ReplacePayload(ctx, r));
            var value = ctx.Listeners.Aggregate(rune.Value, (v, l) => l.ModifyRuneValue(ctx, rune, v));
            value = mods.Aggregate(value, (v, m) => m.ApplyTo(rune, v));

            if (ctx.Preview != null)
            {
                // Preview never resolves targets: that would advance the shared combat RNG.
                var (previewTargets, random) = ctx.PreviewTargets(rune, glyph);
                ctx.Preview.Show(slot.Origin, i, PreviewValue(ctx, inscribed, glyph, previewTargets, inscribed.Value));
                if (value <= 0) continue;
                ctx.Preview.Target(slot.Origin, previewTargets, random);
                ctx.Preview.Record(rune, value, PreviewValue(ctx, rune, glyph, previewTargets, value));
                // Later runes see what this one applies (a Hex's Vulnerable), unless its target is still to be rolled.
                if (!random)
                    foreach (var target in previewTargets)
                    foreach (var power in rune.PreviewPowers)
                        ctx.Preview.Effects.Apply(target, power);
                if (rune is CleanseRune) PurgeBlood(ctx, slots);
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
            if (rune is CleanseRune) PurgeBlood(ctx, slots);
            foreach (var listener in ctx.Listeners)
                await listener.AfterPayload(ctx, rune, value, targets);
            if (ctx.ShouldStop) break;
        }
        return resolved;
    }

    /// <summary>
    /// Cleanse: strips every Blood rune from the Incantation for the rest of the Speak (and from what it keeps for next
    /// turn). A glyph left with no runes is gone; it is removed, not fizzled.
    /// </summary>
    private static void PurgeBlood(RuneContext ctx, List<Slot> slots)
    {
        static bool IsBlood(Rune r) => r is BloodRune;

        ctx.BloodCleansed = true;
        foreach (var slot in slots)
        {
            if (!slot.Consumed && slot.Current.Runes.Any(IsBlood))
            {
                var before = slot.Current;
                if (before.Only(r => !IsBlood(r)) is { } cleansed) slot.Current = cleansed;
                else slot.Consumed = true;
                Log(ctx, $"  Cleanse removes the Blood from {before}");
            }

            for (var i = slot.Clones.Count - 1; i >= 0; i--)
            {
                var (glyph, origin) = slot.Clones[i];
                if (!glyph.Runes.Any(IsBlood)) continue;
                if (glyph.Only(r => !IsBlood(r)) is { } cleansed) slot.Clones[i] = (cleansed, origin);
                else slot.Clones.RemoveAt(i);
            }
        }
    }

    /// <summary>A rune's value after game effects: the value every target agrees on, else the target-independent one.</summary>
    private static int PreviewValue(RuneContext ctx, PayloadRune rune, Glyph glyph, IReadOnlyList<Creature> targets, int value)
    {
        var effects = ctx.Preview?.Effects;
        var values = targets.Select(t => rune.Modified(ctx.Owner, glyph, t, value, effects)).Distinct().ToList();
        return values.Count == 1 ? values[0] : rune.Modified(ctx.Owner, glyph, null, value, effects);
    }

    private static List<Slot> TakeAll(List<Slot> pending)
    {
        var taken = pending.ToList();
        pending.Clear();
        return taken;
    }

    private static List<ModifierRune> AsModifiers(List<Slot> slots) => slots.Select(s => (ModifierRune)s.Current.Runes[0]).ToList();

    private static async Task FizzlePending(RuneContext ctx, List<Slot> pending, string reason)
    {
        // Persist modifiers don't fizzle when nothing follows: they stay in the Incantation instead (Dark Star's Void).
        var fizzled = TakeAll(pending).Where(s => !s.Current.Persistent).ToList();
        for (var i = 0; i < fizzled.Count; i++)
            await Fizzle(ctx, -1, fizzled[i], reason);
    }

    private static async Task Fizzle(RuneContext ctx, int index, Slot slot, string reason)
    {
        if (ctx.Preview != null)
        {
            ctx.Preview.Fizzles++;
            return;
        }

        Log(ctx, $"  #{index} {slot.Current} fizzles: {reason}");
        ctx.Fizzles++;
        ctx.Buffer.NotifyFizzled(slot.Origin);
        foreach (var listener in ctx.Listeners)
            await listener.AfterFizzle(ctx, slot.Current);
    }

    private static async Task Overload(RuneContext ctx, List<Slot> tape, int pc, string reason)
    {
        Log(ctx, $"Overload: {reason}, the rest fizzles");
        if (ctx.Preview != null) ctx.Preview.Overloaded = true;
        foreach (var slot in tape.Skip(pc).Where(s => !s.Consumed))
            await Fizzle(ctx, -1, slot, "overload");
    }

    private static void Log(RuneContext ctx, string message)
    {
        if (!ctx.IsPreview) MainFile.Logger.Info("[Rune] " + message);
    }
}

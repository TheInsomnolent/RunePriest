using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>Accumulated totals from a dry-run of the Incantation, after Strength, Weak, Vulnerable, Frail, etc.</summary>
/// <param name="effects">
/// What runes Spoken earlier will have done by the time these resolve; pass the same instance to chain dry-runs in
/// Speak order (co-op: teammates who Speak first).
/// </param>
public sealed class RunePreview(PreviewEffects? effects = null)
{
    private readonly Dictionary<string, (PayloadRune Rune, int Base, int Total, int Hits)> _totals = [];
    private readonly Dictionary<(Glyph, int), int> _shown = [];
    private readonly Dictionary<Glyph, (HashSet<Creature> Creatures, bool Random)> _targets = [];

    public PreviewEffects Effects { get; } = effects ?? new PreviewEffects();

    public int Fizzles { get; set; }
    public bool Overloaded { get; set; }

    /// <summary>Inscribed glyphs that (or whose carry-overs) stay in the Incantation after this Speak.</summary>
    public HashSet<Glyph> Persisting { get; } = [];
    public bool IsEmpty => _totals.Count == 0 && Fizzles == 0 && !Overloaded;

    /// <param name="value">Before game effects.</param>
    /// <param name="modified">After game effects (<see cref="PayloadRune.Modified"/>).</param>
    public void Record(PayloadRune rune, int value, int modified)
    {
        var (sample, total, modifiedTotal, hits) = _totals.GetValueOrDefault(rune.Key, (rune, 0, 0, 0));
        _totals[rune.Key] = (sample, total + value, modifiedTotal + modified, hits + 1);
    }

    /// <summary>An inscribed rune's own value after game effects, from the first time it resolves (overhead UI).</summary>
    public void Show(Glyph glyph, int runeIndex, int modified) => _shown.TryAdd((glyph, runeIndex), modified);

    public int? ShownValue(Glyph glyph, int runeIndex) => _shown.TryGetValue((glyph, runeIndex), out var v) ? v : null;

    /// <summary>Creatures an inscribed glyph would affect (every execution adds to the set).</summary>
    /// <param name="random">The target is rolled when Spoken; <paramref name="creatures"/> are the candidates.</param>
    public void Target(Glyph glyph, IEnumerable<Creature> creatures, bool random)
    {
        if (!_targets.TryGetValue(glyph, out var entry))
            entry = ([], false);
        entry.Creatures.UnionWith(creatures);
        _targets[glyph] = (entry.Creatures, entry.Random || random);
    }

    public (IReadOnlyCollection<Creature> Creatures, bool Random)? TargetsOf(Glyph glyph) =>
        _targets.TryGetValue(glyph, out var entry) ? (entry.Creatures, entry.Random) : null;

    /// <summary>One line per payload type; <c>Base</c> is the total before game effects.</summary>
    public IEnumerable<(PayloadRune Rune, int Base, int Total, int Hits)> Lines => _totals.Values;
}

/// <summary>
/// Powers that runes Spoken earlier in a dry-run will have applied (a Hex's Weak and Vulnerable), so later runes preview
/// against them: <c>[Hex 1][Strike 5]</c> shows the Strike as 7. They are detached copies — never added to a creature
/// and never raising hooks — that only feed damage previews. A creature that already has the power is left alone (Weak
/// and Vulnerable don't scale with stacks).
/// </summary>
public sealed class PreviewEffects
{
    // Owner has a private setter and the real apply path (ApplyInternal) mutates the creature, so set the field directly.
    private static readonly FieldInfo? OwnerField = AccessTools.Field(typeof(PowerModel), "_owner");

    private readonly List<PowerModel> _powers = [];

    public IReadOnlyList<PowerModel> Powers => _powers;

    public void Apply(Creature creature, PowerModel canonical)
    {
        var type = canonical.GetType();
        if (OwnerField == null || creature.IsDead || creature.Powers.Any(p => p.GetType() == type)) return;
        if (_powers.Any(p => p.GetType() == type && OwnerField.GetValue(p) == creature)) return;

        // Amount stays 0: setting it would notify the (real) owner creature. Weak/Vulnerable don't read it.
        var power = canonical.ToMutable();
        OwnerField.SetValue(power, creature);
        _powers.Add(power);
    }
}

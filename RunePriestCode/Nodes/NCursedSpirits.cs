using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// A player's Cursed Spirits, drawn like orbs: one <see cref="NSpirit"/> per spirit of their
/// <see cref="CursedSpiritsPower"/> (which is hidden from the power bar), fanned out in an arc over their head.
/// When a spirit attacks it flies at its target and back. Attached to every player <see cref="NCreature"/> by
/// <c>NCreatureRuneBufferPatch</c>; empty without spirits.
/// </summary>
public partial class NCursedSpirits : Node2D
{
    // The arc hugs the head, below the Incantation's first row.
    private const float SideMargin = 50f;
    private const float TopMargin = 22f;
    private static readonly float MaxStep = Mathf.DegToRad(26f);
    private static readonly float MaxSpread = Mathf.DegToRad(200f);

    private readonly List<NSpirit> _spirits = [];
    private NCreature _creatureNode = null!;

    public static NCursedSpirits Create(NCreature creatureNode) => new() { _creatureNode = creatureNode };

    public override void _EnterTree() => CursedSpiritsPower.SpiritStruck += OnSpiritStruck;

    public override void _ExitTree() => CursedSpiritsPower.SpiritStruck -= OnSpiritStruck;

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_creatureNode)) return;

        var creature = _creatureNode.Entity;
        var count = creature.CombatState != null ? creature.GetPowerAmount<CursedSpiritsPower>() : 0;
        Sync(Math.Max(0, count));

        var hitbox = _creatureNode.Hitbox.GetGlobalRect();
        GlobalPosition = hitbox.GetCenter();
        var radius = new Vector2(hitbox.Size.X / 2f + SideMargin, hitbox.Size.Y / 2f + TopMargin);
        var step = _spirits.Count > 1 ? Mathf.Min(MaxStep, MaxSpread / (_spirits.Count - 1)) : 0f;
        for (var i = 0; i < _spirits.Count; i++)
        {
            // Centred on straight up; the first spirit is on the left.
            var angle = -Mathf.Pi / 2f + (i - (_spirits.Count - 1) / 2f) * step;
            _spirits[i].Home = new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
    }

    private void Sync(int count)
    {
        while (_spirits.Count < count)
        {
            var spirit = NSpirit.Create(() => _creatureNode.Entity.GetPower<CursedSpiritsPower>());
            _spirits.Add(spirit);
            AddChild(spirit);
        }

        while (_spirits.Count > count)
        {
            var last = _spirits[^1];
            _spirits.RemoveAt(_spirits.Count - 1);
            last.Vanish();
        }
    }

    private void OnSpiritStruck(Creature owner, Creature target, int index)
    {
        if (!IsInstanceValid(_creatureNode) || owner != _creatureNode.Entity || _spirits.Count == 0) return;
        var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null || !IsInstanceValid(targetNode)) return;

        var last = targetNode.Hitbox.GetGlobalRect().GetCenter();
        _spirits[index % _spirits.Count].Strike(() =>
            IsInstanceValid(targetNode) ? last = targetNode.Hitbox.GetGlobalRect().GetCenter() : last);
    }
}

using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using RunePriest.RunePriestCode.Cards;

namespace RunePriest.RunePriestCode.Events;

/// <summary>
/// Base for rune-themed events. A player <see cref="Qualifies"/> when the event has something for them (e.g. a rune card
/// to target). The event is only allowed when <b>every</b> player qualifies — the game re-checks
/// <see cref="EventModel.IsAllowed"/> each time it pulls the next event, so it simply skips ahead otherwise — and
/// rune-specific options show their <c>_LOCKED</c> variant to a player who doesn't (each player sees their own options).
/// </summary>
public abstract class RuneEvent : CustomEventModel
{
    protected static ActModel[] Act1 => [ModelDb.Act<Overgrowth>(), ModelDb.Act<Underdocks>()];
    protected static ActModel[] Act2 => [ModelDb.Act<Hive>()];
    protected static ActModel[] Act3 => [ModelDb.Act<Glory>()];

    /// <summary>Whether the event has something for <paramref name="player"/>.</summary>
    protected abstract bool Qualifies(Player player);

    public override bool IsAllowed(IRunState runState) => runState.Players.All(Qualifies);

    /// <summary>Can make use of runes: a Rune Priest, or anyone who picked up a card that Inscribes.</summary>
    public static bool UsesRunes(Player player) =>
        player.Character is Character.RunePriest || player.Deck.Cards.Any(c => c is RuneCard);

    /// <summary>Vanilla event portrait (<c>images/events/&lt;name&gt;.png</c>) shown until this event has its own art.</summary>
    protected abstract string PlaceholderPortrait { get; }

    public override string CustomInitialPortraitPath
    {
        get
        {
            var own = $"{MainFile.ResPath}/images/events/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";
            return ResourceLoader.Exists(own) ? own : ImageHelper.GetImagePath($"events/{PlaceholderPortrait}.png");
        }
    }

    protected EventOption Choice(Func<Task> onChosen, string option, params IHoverTip[] tips) =>
        new(this, onChosen, InitialOptionKey(option), tips);

    /// <summary>A greyed-out option (<c>&lt;OPTION&gt;_LOCKED</c>) explaining why this player can't take it.</summary>
    protected EventOption Locked(string option) => new(this, null, InitialOptionKey(option + "_LOCKED"));

    /// <summary>Finishes the event on page <paramref name="page"/>'s description.</summary>
    protected void Finish(string page) => SetEventFinished(L10NLookup($"{Id.Entry}.pages.{page}.description"));
}

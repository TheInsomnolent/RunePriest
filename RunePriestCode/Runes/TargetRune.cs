namespace RunePriest.RunePriestCode.Runes;

public enum TargetMode
{
    Anchor,
    Seek,
    Nova,
    Chain,
    Cull,
    Mirror
}

/// <summary>Sets the target mode for every later payload until changed.</summary>
public sealed class TargetRune(TargetMode mode) : Rune(0)
{
    public TargetMode Mode { get; } = mode;
    public override RuneKind Kind => RuneKind.Target;
    public override string Key => Mode.ToString().ToUpperInvariant();
    public override bool ShowsValue => false;
}

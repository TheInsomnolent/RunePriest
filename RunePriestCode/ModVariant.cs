namespace RunePriest.RunePriestCode;

/// <summary>
/// Build variant, chosen by the csproj's <c>ModVariant</c> property (which defines <c>MOD_VARIANT_*</c>). Each variant
/// is a separate mod (own manifest id, assembly, root namespace, <c>res://</c> root and loc keys) so they can be
/// installed side by side. The build rewrites <c>RunePriest.RunePriestCode</c> namespaces and <c>RUNEPRIEST</c>
/// loc keys to match; see docs/build-variants.md.
/// </summary>
public static class ModVariant
{
    public const string BaseId = "RunePriest";

#if MOD_VARIANT_LOCAL
    public const string Name = "Local";
    public const string IdUpper = "RUNEPRIESTLOCAL";
#elif MOD_VARIANT_NIGHTLY
    public const string Name = "Nightly";
    public const string IdUpper = "RUNEPRIESTNIGHTLY";
#else
    public const string Name = "";
    public const string IdUpper = "RUNEPRIEST";
#endif

    public const string ModId = BaseId + Name;

    public static bool IsRelease => Name.Length == 0;
}

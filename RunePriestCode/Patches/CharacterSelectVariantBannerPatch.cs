using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace RunePriest.RunePriestCode.Patches;

/// <summary>
/// Labels the character select tile of non-release builds ("LOCAL" / "NIGHTLY") so side-by-side installed variants
/// can be told apart.
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
public static class CharacterSelectVariantBannerPatch
{
    private const string BannerName = $"{MainFile.ModId}VariantBanner";

    public static bool Prepare() => !ModVariant.IsRelease;

    [HarmonyPostfix]
    public static void AddBanner(NCharacterSelectButton __instance, CharacterModel character)
    {
        if (character is not Character.RunePriest || __instance.HasNode(BannerName)) return;
        __instance.AddChild(CreateBanner());
    }

    private static Control CreateBanner()
    {
        var banner = new PanelContainer { Name = BannerName, MouseFilter = Control.MouseFilterEnum.Ignore };
        banner.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = ModVariant.Name == "Nightly" ? new Color(0.78f, 0.38f, 0.08f, 0.92f) : new Color(0.12f, 0.55f, 0.42f, 0.92f),
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        });
        banner.AddChild(new Label
        {
            Text = ModVariant.Name.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            LabelSettings = new LabelSettings
            {
                FontSize = 18,
                FontColor = Colors.White,
                OutlineSize = 4,
                OutlineColor = new Color(0, 0, 0, 0.8f)
            }
        });
        banner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        return banner;
    }
}

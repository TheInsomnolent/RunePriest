using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using RunePriest.RunePriestCode.Character;
using RunePriest.RunePriestCode.Extensions;

namespace RunePriest.RunePriestCode.Potions;

[Pool(typeof(RunePriestPotionPool))]
public abstract class RunePriestPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}
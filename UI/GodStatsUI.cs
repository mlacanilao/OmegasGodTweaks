using System.Collections.Generic;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine;

namespace OmegasGodTweaks.UI;

internal static class GodStatsUI
{
    private const string GodStatsDropdownId = "GodStatsDropdown";
    private const string GodStatsTextId = "GodStatsText";
    private const string GodStatsNoStateTextId = "config.god_stats.no_state";
    private const string GodStatsJoinedTextId = "config.god_stats.joined";
    private const string GodStatsYesTextId = "config.god_stats.yes";
    private const string GodStatsNoTextId = "config.god_stats.no";
    private const string GodStatsPietyTextId = "config.god_stats.piety";
    private const string GodStatsPietyExpTextId = "config.god_stats.piety_exp";
    private const string GodStatsWorshipDaysTextId = "config.god_stats.worship_days";
    private const string GodStatsGiftRankTextId = "config.god_stats.gift_rank";
    private const string GodStatsApostleRewardsTextId = "config.god_stats.apostle_rewards";
    private const string GodStatsArtifactRewardsTextId = "config.god_stats.artifact_rewards";
    private const string GodStatsNoStateFallbackText = "No saved state for this god in the current save.";

    internal static bool Build(ModOptionController controller, OptionUIBuilder builder)
    {
        OptDropdown? dropdown = UIController.GetRequiredPreBuild<OptDropdown>(builder: builder, id: GodStatsDropdownId);
        OptLabel? statsText = UIController.GetRequiredPreBuild<OptLabel>(builder: builder, id: GodStatsTextId);
        if (dropdown == null || statsText == null)
        {
            return false;
        }

        statsText.Align = TextAnchor.UpperLeft;
        List<RevelationUI.ReligionOption> religionOptions = RevelationUI.BuildReligionOptions();
        if (religionOptions.Count <= 0)
        {
            UIController.SetDropdownOptions(
                dropdown: dropdown,
                texts: new[] { RevelationUI.GetNoReligionOptionsText(controller: controller) });
            statsText.Text = GetNoStateText(controller: controller);
            return true;
        }

        int currentIndex = GetDefaultGodStatsIndex(options: religionOptions);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        UIController.SetDropdownOptions(
            dropdown: dropdown,
            texts: RevelationUI.GetReligionOptionTexts(options: religionOptions),
            selectedIndex: currentIndex);
        UpdateGodStatsText(controller: controller, statsText: statsText, options: religionOptions, index: currentIndex);

        dropdown.OnValueChanged += index =>
        {
            UpdateGodStatsText(controller: controller, statsText: statsText, options: religionOptions, index: index);
        };

        return true;
    }

    private static int GetDefaultGodStatsIndex(IReadOnlyList<RevelationUI.ReligionOption> options)
    {
        string currentFaithId = EClass.core?.game?.player?.chara?.idFaith ?? string.Empty;
        return RevelationUI.GetReligionOptionIndex(options: options, id: currentFaithId);
    }

    private static void UpdateGodStatsText(
        ModOptionController controller,
        OptLabel statsText,
        IReadOnlyList<RevelationUI.ReligionOption> options,
        int index)
    {
        if (RevelationUI.TryGetReligionOptionId(options: options, index: index, id: out string godId) == false)
        {
            statsText.Text = GetNoStateText(controller: controller);
            return;
        }

        statsText.Text = GodStatsFormatter.Format(godId: godId, labels: BuildLabels(controller: controller));
    }

    private static GodStatsTextLabels BuildLabels(ModOptionController controller)
    {
        return new GodStatsTextLabels
        {
            NoState = GetNoStateText(controller: controller),
            Joined = controller.Tr(contentId: GodStatsJoinedTextId),
            Yes = controller.Tr(contentId: GodStatsYesTextId),
            No = controller.Tr(contentId: GodStatsNoTextId),
            Piety = controller.Tr(contentId: GodStatsPietyTextId),
            PietyExp = controller.Tr(contentId: GodStatsPietyExpTextId),
            WorshipDays = controller.Tr(contentId: GodStatsWorshipDaysTextId),
            GiftRank = controller.Tr(contentId: GodStatsGiftRankTextId),
            ApostleRewards = controller.Tr(contentId: GodStatsApostleRewardsTextId),
            ArtifactRewards = controller.Tr(contentId: GodStatsArtifactRewardsTextId)
        };
    }

    private static string GetNoStateText(ModOptionController controller)
    {
        string text = controller.Tr(contentId: GodStatsNoStateTextId);
        if (text == GodStatsNoStateTextId)
        {
            return GodStatsNoStateFallbackText;
        }

        return text;
    }
}

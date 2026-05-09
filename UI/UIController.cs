using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OmegasGodTweaks.UI;

internal static class UIController
{
    private const string AllowJoiningMultipleReligionsToggleId = "AllowJoiningMultipleReligionsToggle";
    private const string RemoveConversionPunishmentToggleId = "RemoveConversionPunishmentToggle";
    private const string RemoveAltarTakeoverPunishmentToggleId = "RemoveAltarTakeoverPunishmentToggle";
    private const string AllowOfferingsForJoinedNonCurrentGodsToggleId = "AllowOfferingsForJoinedNonCurrentGodsToggle";
    private const string RemoveOfferingCategoryRestrictionsToggleId = "RemoveOfferingCategoryRestrictionsToggle";
    private const string RemovePietyCapFromOfferingsToggleId = "RemovePietyCapFromOfferingsToggle";
    private const string RemoveOfferingWeightValueCapToggleId = "RemoveOfferingWeightValueCapToggle";
    private const string RemoveOfferingLevelBonusCapToggleId = "RemoveOfferingLevelBonusCapToggle";
    private const string RemoveOfferingOverflowWasteToggleId = "RemoveOfferingOverflowWasteToggle";
    private const string DisableHarvestQuestOfferingKarmaLossToggleId = "DisableHarvestQuestOfferingKarmaLossToggle";
    private const string AddPietyGainFromPrayerToggleId = "AddPietyGainFromPrayerToggle";
    private const string AllowMultiplePrayersPerDayToggleId = "AllowMultiplePrayersPerDayToggle";
    private const string ApplyPrayerPietyToJoinedGodsToggleId = "ApplyPrayerPietyToJoinedGodsToggle";
    private const string AllowPassivePrayerPietyGainToggleId = "AllowPassivePrayerPietyGainToggle";
    private const string AllowPrayerRewardChecksForJoinedGodsToggleId = "AllowPrayerRewardChecksForJoinedGodsToggle";
    private const string RepeatApostleRewardsToggleId = "RepeatApostleRewardsToggle";
    private const string RepeatArtifactRewardsToggleId = "RepeatArtifactRewardsToggle";
    private const string ApplyJoinedGodBonusesToggleId = "ApplyJoinedGodBonusesToggle";
    private const string RemoveFaithResistanceBonusCapToggleId = "RemoveFaithResistanceBonusCapToggle";
    private const string UnlockGodArtifactFactionEffectsToggleId = "UnlockGodArtifactFactionEffectsToggle";
    private const string AllowDuplicateGodArtifactsToggleId = "AllowDuplicateGodArtifactsToggle";
    private const string DisableEythSingleArtifactPurgeToggleId = "DisableEythSingleArtifactPurgeToggle";
    private const string DisableApostleInfightingToggleId = "DisableApostleInfightingToggle";
    private const string EnableJoinedGodRevelationRoutingToggleId = "EnableJoinedGodRevelationRoutingToggle";
    private const string ShowPietyFaithAfterOfferingToggleId = "ShowPietyFaithAfterOfferingToggle";
    private const string ShowPietyFaithAfterPrayerToggleId = "ShowPietyFaithAfterPrayerToggle";
    private const string PrayerPietyGainInputId = "PrayerPietyGainInput";
    private const string JoinedGodRevelationChanceInputId = "JoinedGodRevelationChanceInput";

    private static readonly string[] DescriptionLabelIds =
    {
        "config.joining.description",
        "config.offerings.description",
        "config.prayer.description",
        "config.rewards.description",
        "config.artifacts.description",
        "config.revelation.description",
        "config.god_stats.description"
    };

    public static void RegisterUI()
    {
        ModOptionController controller = ModOptionController.Register(guid: ModInfo.Guid, tooptipId: "mod.tooltip");
        if (controller == null)
        {
            OmegasGodTweaks.LogDebug(message: "Failed to register Mod Options controller.");
            return;
        }

        string assemblyLocation = Path.GetDirectoryName(path: Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        string xmlPath = Path.Combine(path1: assemblyLocation, path2: "OmegasGodTweaksConfig.xml");
        string xlsxPath = Path.Combine(path1: assemblyLocation, path2: "translations.xlsx");

        OmegasGodTweaksConfig.InitializeXmlPath(xmlPath: xmlPath);
        OmegasGodTweaksConfig.InitializeTranslationXlsxPath(xlsxPath: xlsxPath);

        if (File.Exists(path: OmegasGodTweaksConfig.XmlPath))
        {
            controller.SetPreBuildWithXml(xml: File.ReadAllText(path: OmegasGodTweaksConfig.XmlPath));
        }
        else
        {
            OmegasGodTweaks.LogDebug(message: $"Mod Options XML not found: {xmlPath}");
        }

        if (File.Exists(path: OmegasGodTweaksConfig.TranslationXlsxPath))
        {
            controller.SetTranslationsFromXslx(path: OmegasGodTweaksConfig.TranslationXlsxPath);
        }
        else
        {
            OmegasGodTweaks.LogDebug(message: $"Mod Options translations not found: {xlsxPath}");
        }

        RegisterEvents(controller: controller);
    }

    private static void RegisterEvents(ModOptionController controller)
    {
        controller.OnBuildUI += builder =>
        {
            AlignDescriptionLabels(builder: builder);

            BindToggle(builder: builder, id: AllowJoiningMultipleReligionsToggleId, entry: OmegasGodTweaksConfig.AllowJoiningMultipleReligions);
            BindToggle(builder: builder, id: RemoveConversionPunishmentToggleId, entry: OmegasGodTweaksConfig.RemoveConversionPunishment);
            BindToggle(builder: builder, id: RemoveAltarTakeoverPunishmentToggleId, entry: OmegasGodTweaksConfig.RemoveAltarTakeoverPunishment);
            BindToggle(builder: builder, id: AllowOfferingsForJoinedNonCurrentGodsToggleId, entry: OmegasGodTweaksConfig.AllowOfferingsForJoinedNonCurrentGods);
            BindToggle(builder: builder, id: RemoveOfferingCategoryRestrictionsToggleId, entry: OmegasGodTweaksConfig.RemoveOfferingCategoryRestrictions);
            BindToggle(builder: builder, id: RemovePietyCapFromOfferingsToggleId, entry: OmegasGodTweaksConfig.RemovePietyCapFromOfferings);
            BindToggle(builder: builder, id: RemoveOfferingWeightValueCapToggleId, entry: OmegasGodTweaksConfig.RemoveOfferingWeightValueCap);
            BindToggle(builder: builder, id: RemoveOfferingLevelBonusCapToggleId, entry: OmegasGodTweaksConfig.RemoveOfferingLevelBonusCap);
            BindToggle(builder: builder, id: RemoveOfferingOverflowWasteToggleId, entry: OmegasGodTweaksConfig.RemoveOfferingOverflowWaste);
            BindToggle(builder: builder, id: DisableHarvestQuestOfferingKarmaLossToggleId, entry: OmegasGodTweaksConfig.DisableHarvestQuestOfferingKarmaLoss);
            BindToggle(builder: builder, id: AddPietyGainFromPrayerToggleId, entry: OmegasGodTweaksConfig.AddPietyGainFromPrayer);
            BindToggle(builder: builder, id: AllowMultiplePrayersPerDayToggleId, entry: OmegasGodTweaksConfig.AllowMultiplePrayersPerDay);
            BindToggle(builder: builder, id: ApplyPrayerPietyToJoinedGodsToggleId, entry: OmegasGodTweaksConfig.ApplyPrayerPietyToJoinedGods);
            BindToggle(builder: builder, id: AllowPassivePrayerPietyGainToggleId, entry: OmegasGodTweaksConfig.AllowPassivePrayerPietyGain);
            BindToggle(builder: builder, id: AllowPrayerRewardChecksForJoinedGodsToggleId, entry: OmegasGodTweaksConfig.AllowPrayerRewardChecksForJoinedGods);
            BindToggle(builder: builder, id: RepeatApostleRewardsToggleId, entry: OmegasGodTweaksConfig.RepeatApostleRewards);
            BindToggle(builder: builder, id: RepeatArtifactRewardsToggleId, entry: OmegasGodTweaksConfig.RepeatArtifactRewards);
            BindToggle(builder: builder, id: ApplyJoinedGodBonusesToggleId, entry: OmegasGodTweaksConfig.ApplyJoinedGodBonuses);
            BindToggle(builder: builder, id: RemoveFaithResistanceBonusCapToggleId, entry: OmegasGodTweaksConfig.RemoveFaithResistanceBonusCap);
            BindToggle(builder: builder, id: UnlockGodArtifactFactionEffectsToggleId, entry: OmegasGodTweaksConfig.UnlockGodArtifactFactionEffects);
            BindToggle(builder: builder, id: AllowDuplicateGodArtifactsToggleId, entry: OmegasGodTweaksConfig.AllowDuplicateGodArtifacts);
            BindToggle(builder: builder, id: DisableEythSingleArtifactPurgeToggleId, entry: OmegasGodTweaksConfig.DisableEythSingleArtifactPurge);
            BindToggle(builder: builder, id: DisableApostleInfightingToggleId, entry: OmegasGodTweaksConfig.DisableApostleInfighting);
            BindToggle(builder: builder, id: EnableJoinedGodRevelationRoutingToggleId, entry: OmegasGodTweaksConfig.EnableJoinedGodRevelationRouting);
            BindToggle(builder: builder, id: ShowPietyFaithAfterOfferingToggleId, entry: OmegasGodTweaksConfig.ShowPietyFaithAfterOffering);
            BindToggle(builder: builder, id: ShowPietyFaithAfterPrayerToggleId, entry: OmegasGodTweaksConfig.ShowPietyFaithAfterPrayer);
            BindIntInput(builder: builder, id: PrayerPietyGainInputId, entry: OmegasGodTweaksConfig.PrayerPietyGain);
            BindIntInput(builder: builder, id: JoinedGodRevelationChanceInputId, entry: OmegasGodTweaksConfig.JoinedGodRevelationChance);

            if (RevelationUI.Build(controller: controller, builder: builder) == false)
            {
                return;
            }

            if (GodStatsUI.Build(controller: controller, builder: builder) == false)
            {
                return;
            }
        };
    }

    private static void AlignDescriptionLabels(OptionUIBuilder builder)
    {
        foreach (string textId in DescriptionLabelIds)
        {
            OptLabel? text = GetRequiredPreBuild<OptLabel>(builder: builder, id: textId);
            if (text == null)
            {
                continue;
            }

            text.Align = TextAnchor.UpperLeft;
        }
    }

    private static void BindToggle(OptionUIBuilder builder, string id, ConfigEntry<bool> entry)
    {
        OptToggle? toggle = GetRequiredPreBuild<OptToggle>(builder: builder, id: id);
        if (toggle == null)
        {
            return;
        }

        toggle.Checked = entry.Value;
        toggle.OnValueChanged += value =>
        {
            entry.Value = value;
        };
    }

    private static void BindIntInput(OptionUIBuilder builder, string id, ConfigEntry<int> entry)
    {
        OptInput? input = GetRequiredPreBuild<OptInput>(builder: builder, id: id);
        if (input == null)
        {
            return;
        }

        input.ContentType = InputField.ContentType.IntegerNumber;
        input.Text = entry.Value.ToString(provider: CultureInfo.InvariantCulture);
        input.OnValueChanged += value =>
        {
            if (int.TryParse(s: value, style: NumberStyles.Integer, provider: CultureInfo.InvariantCulture, result: out int parsed) == false)
            {
                return;
            }

            entry.Value = parsed;
        };
    }

    internal static void SetDropdownOptions(OptDropdown? dropdown, IEnumerable<string> texts, int selectedIndex = 0)
    {
        if (dropdown?.Base == null)
        {
            return;
        }

        List<string> optionTexts = texts.ToList();
        dropdown.Base.options.Clear();
        foreach (string text in optionTexts)
        {
            dropdown.Base.options.Add(item: new Dropdown.OptionData(text: text));
        }

        if (optionTexts.Count == 0)
        {
            dropdown.Value = 0;
        }
        else
        {
            dropdown.Value = Mathf.Clamp(value: selectedIndex, min: 0, max: optionTexts.Count - 1);
        }

        dropdown.Base.RefreshShownValue();
    }

    internal static T? GetRequiredPreBuild<T>(OptionUIBuilder builder, string id) where T : OptUIElement
    {
        T? element = builder.GetPreBuild<T>(id: id);
        if (element == null)
        {
            OmegasGodTweaks.LogDebug(message: $"Missing Mod Options prebuilt element: {id}");
        }

        return element;
    }
}

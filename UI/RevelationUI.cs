using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace OmegasGodTweaks.UI;

internal static class RevelationUI
{
    private const string RevelationModeDropdownId = "RevelationModeDropdown";
    private const string SelectedRevelationGodDropdownId = "SelectedRevelationGodDropdown";
    private const string NotInSourceTextId = "config.selected_revelation_god.not_in_source";
    private const string NotInSourceFallbackText = "{1} (not in source)";
    private const string NoReligionOptionsTextId = "config.selected_revelation_god.no_options";
    private const string NoReligionOptionsFallbackText = "No selectable gods found";
    private const string EythReligionId = "eyth";

    internal static bool Build(ModOptionController controller, OptionUIBuilder builder)
    {
        if (BindRevelationModeDropdown(
                controller: controller,
                builder: builder,
                entry: OmegasGodTweaksConfig.RevelationMode) == false)
        {
            return false;
        }

        if (BindReligionDropdown(
                controller: controller,
                builder: builder,
                id: SelectedRevelationGodDropdownId,
                entry: OmegasGodTweaksConfig.SelectedRevelationGod) == false)
        {
            return false;
        }

        return true;
    }

    private static bool BindRevelationModeDropdown(
        ModOptionController controller,
        OptionUIBuilder builder,
        ConfigEntry<RevelationMode> entry)
    {
        OptDropdown? dropdown = UIController.GetRequiredPreBuild<OptDropdown>(builder: builder, id: RevelationModeDropdownId);
        if (dropdown == null)
        {
            return false;
        }

        List<string> names = new List<string>(collection: Enum.GetNames(enumType: typeof(RevelationMode)));
        int currentIndex = names.IndexOf(item: entry.Value.ToString());
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        UIController.SetDropdownOptions(
            dropdown: dropdown,
            texts: names.Select(selector: name => controller.Tr(contentId: $"enum.revelation_mode.{name}")),
            selectedIndex: currentIndex);

        dropdown.OnValueChanged += index =>
        {
            if (index < 0 || index >= names.Count)
            {
                return;
            }

            if (Enum.TryParse(value: names[index: index], result: out RevelationMode parsed) == true)
            {
                entry.Value = parsed;
            }
        };

        return true;
    }

    private static bool BindReligionDropdown(
        ModOptionController controller,
        OptionUIBuilder builder,
        string id,
        ConfigEntry<string> entry)
    {
        OptDropdown? dropdown = UIController.GetRequiredPreBuild<OptDropdown>(builder: builder, id: id);
        if (dropdown == null)
        {
            return false;
        }

        List<ReligionOption> religionOptions = BuildReligionOptions();
        if (religionOptions.Count == 0)
        {
            UIController.SetDropdownOptions(dropdown: dropdown, texts: new[] { GetNoReligionOptionsText(controller: controller) });
            return true;
        }

        string selectedId = (entry.Value ?? string.Empty).Trim();
        int currentIndex = GetReligionOptionIndex(options: religionOptions, id: selectedId);
        if (currentIndex < 0 && string.IsNullOrWhiteSpace(value: selectedId) == false)
        {
            religionOptions.Insert(index: 0, item: new ReligionOption(id: selectedId, text: GetNotInSourceText(controller: controller, id: selectedId)));
            currentIndex = 0;
        }

        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        UIController.SetDropdownOptions(dropdown: dropdown, texts: GetReligionOptionTexts(options: religionOptions), selectedIndex: currentIndex);
        dropdown.OnValueChanged += index =>
        {
            if (index < 0 || index >= religionOptions.Count)
            {
                return;
            }

            entry.Value = religionOptions[index: index].Id;
        };

        return true;
    }

    internal static List<ReligionOption> BuildReligionOptions()
    {
        List<ReligionOption> religionOptions = new List<ReligionOption>();

        if (EClass.sources?.religions?.map == null)
        {
            return religionOptions;
        }

        foreach (KeyValuePair<string, SourceReligion.Row> pair in EClass.sources.religions.map)
        {
            SourceReligion.Row row = pair.Value;
            if (IsSelectableReligionRow(row: row) == false)
            {
                continue;
            }

            if (GetReligionOptionIndex(options: religionOptions, id: row.id) >= 0)
            {
                continue;
            }

            religionOptions.Add(item: CreateReligionOption(row: row));
        }

        religionOptions.Sort(comparison: CompareReligionOptions);
        return religionOptions;
    }

    private static bool IsSelectableReligionRow(SourceReligion.Row row)
    {
        if (row == null || string.IsNullOrWhiteSpace(value: row.id) == true)
        {
            return false;
        }

        if (string.Equals(a: row.id, b: EythReligionId, comparisonType: StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(value: row.type) == false &&
            string.Equals(a: row.type, b: "Religion", comparisonType: StringComparison.OrdinalIgnoreCase) == false)
        {
            return false;
        }

        Religion? religion = EClass.core?.game?.religions?.Find(id: row.id);
        if (religion?.IsEyth == true)
        {
            return false;
        }

        return true;
    }

    internal static string GetNoReligionOptionsText(ModOptionController controller)
    {
        string text = controller.Tr(contentId: NoReligionOptionsTextId);
        if (text == NoReligionOptionsTextId)
        {
            return NoReligionOptionsFallbackText;
        }

        return text;
    }

    private static string GetNotInSourceText(ModOptionController controller, string id)
    {
        string text = controller.Tr(contentId: NotInSourceTextId, args: id);
        if (text == NotInSourceTextId)
        {
            text = NotInSourceFallbackText.Replace(oldValue: "{1}", newValue: id);
        }

        return text;
    }

    private static ReligionOption CreateReligionOption(SourceReligion.Row row)
    {
        string displayName = GetReligionDisplayName(row: row);
        string text = row.id;
        if (string.IsNullOrWhiteSpace(value: displayName) == false)
        {
            text = $"{row.id} - {displayName}";
        }

        return new ReligionOption(id: row.id, text: text);
    }

    private static string GetReligionDisplayName(SourceReligion.Row row)
    {
        string displayName = row.GetName();
        if (string.IsNullOrWhiteSpace(value: displayName) == true)
        {
            displayName = row.name;
        }

        if (string.IsNullOrWhiteSpace(value: displayName) == true)
        {
            displayName = row.name_JP;
        }

        return displayName ?? string.Empty;
    }

    private static int CompareReligionOptions(ReligionOption left, ReligionOption right)
    {
        return string.Compare(strA: left.Text, strB: right.Text, comparisonType: StringComparison.CurrentCultureIgnoreCase);
    }

    internal static List<string> GetReligionOptionTexts(IReadOnlyList<ReligionOption> options)
    {
        List<string> texts = new List<string>();
        foreach (ReligionOption option in options)
        {
            texts.Add(item: option.Text);
        }

        return texts;
    }

    internal static int GetReligionOptionIndex(IReadOnlyList<ReligionOption> options, string id)
    {
        if (string.IsNullOrWhiteSpace(value: id) == true)
        {
            return -1;
        }

        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(a: options[index: i].Id, b: id, comparisonType: StringComparison.OrdinalIgnoreCase) == true)
            {
                return i;
            }
        }

        return -1;
    }

    internal static bool TryGetReligionOptionId(IReadOnlyList<ReligionOption> options, int index, out string id)
    {
        id = string.Empty;
        if (index < 0 || index >= options.Count)
        {
            return false;
        }

        id = options[index: index].Id;
        return true;
    }

    internal readonly struct ReligionOption
    {
        internal ReligionOption(string id, string text)
        {
            Id = id;
            Text = text;
        }

        internal string Id { get; }

        internal string Text { get; }
    }
}

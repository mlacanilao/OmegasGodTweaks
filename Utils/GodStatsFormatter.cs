using System;
using System.Globalization;

namespace OmegasGodTweaks;

internal static class GodStatsFormatter
{
    internal static string Format(string godId, GodStatsTextLabels labels)
    {
        if (TryGetState(godId: godId, state: out GodFaithState state) == false)
        {
            return labels.NoState;
        }

        string joinedText = labels.No;
        if (state.Joined == true)
        {
            joinedText = labels.Yes;
        }

        string[] lines =
        {
            FormatStatLine(label: labels.Joined, value: joinedText),
            FormatStatLine(label: labels.Piety, value: FormatPiety(state: state, labels: labels)),
            FormatStatLine(label: labels.WorshipDays, value: state.WorshipDays.ToString(provider: CultureInfo.InvariantCulture)),
            FormatStatLine(label: labels.GiftRank, value: state.GiftRank.ToString(provider: CultureInfo.InvariantCulture)),
            FormatStatLine(label: labels.ApostleRewards, value: state.ApostleRewardCount.ToString(provider: CultureInfo.InvariantCulture)),
            FormatStatLine(label: labels.ArtifactRewards, value: state.ArtifactRewardCount.ToString(provider: CultureInfo.InvariantCulture))
        };

        return string.Join(separator: Environment.NewLine, value: lines);
    }

    private static bool TryGetState(string godId, out GodFaithState state)
    {
        state = null!;
        if (IsSaveStateAvailable() == false)
        {
            return false;
        }

        FaithSaveModel current = FaithSaveData.Current;
        if (current.Gods == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(value: godId) == true)
        {
            return false;
        }

        if (current.Gods.TryGetValue(key: godId, value: out GodFaithState? savedState) == false)
        {
            return false;
        }

        if (savedState == null)
        {
            return false;
        }

        state = savedState;
        return true;
    }

    private static string FormatPiety(GodFaithState state, GodStatsTextLabels labels)
    {
        return state.PietyBase.ToString(provider: CultureInfo.InvariantCulture) +
               " (" +
               labels.PietyExp +
               ": " +
               state.PietyExp.ToString(provider: CultureInfo.InvariantCulture) +
               ")";
    }

    private static string FormatStatLine(string label, string value)
    {
        return label + ": " + value;
    }

    private static bool IsSaveStateAvailable()
    {
        try
        {
            if (EClass.core?.IsGameStarted != true)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(value: Game.id) == true)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class GodStatsTextLabels
{
    internal string NoState { get; set; } = string.Empty;

    internal string Joined { get; set; } = string.Empty;

    internal string Yes { get; set; } = string.Empty;

    internal string No { get; set; } = string.Empty;

    internal string Piety { get; set; } = string.Empty;

    internal string PietyExp { get; set; } = string.Empty;

    internal string WorshipDays { get; set; } = string.Empty;

    internal string GiftRank { get; set; } = string.Empty;

    internal string ApostleRewards { get; set; } = string.Empty;

    internal string ArtifactRewards { get; set; } = string.Empty;
}

using System;

namespace OmegasGodTweaks;

internal static class GameIOPatch
{
    internal static void SaveGamePostfix()
    {
        FaithSaveData.SaveCurrent();
    }

    internal static void PrepareSteamCloudPrefix(string id, string path)
    {
        if (EClass.core?.game == null)
        {
            return;
        }

        if (string.Equals(a: id, b: Game.id, comparisonType: StringComparison.Ordinal) == false)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value: path) == false)
        {
            return;
        }

        FaithSaveData.SaveCurrent();
    }
}

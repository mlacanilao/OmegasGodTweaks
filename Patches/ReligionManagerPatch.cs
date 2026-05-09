namespace OmegasGodTweaks;

internal static class ReligionManagerPatch
{
    internal static void OnLoadPostfix()
    {
        FaithSaveData.LoadCurrent();
        GodFaithStateService.ApplyLoadedState();
    }

    internal static void OnCreateGamePostfix()
    {
        FaithSaveData.ResetForNewGame();
    }
}

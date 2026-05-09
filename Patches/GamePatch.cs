namespace OmegasGodTweaks;

internal static class GamePatch
{
    internal static void LoadPostfix()
    {
        ElementContainerPatch.RefreshAppliedArtifactEffects();
    }
}

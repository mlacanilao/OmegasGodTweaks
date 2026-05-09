namespace OmegasGodTweaks;

internal static class FeatureTestLog
{
    internal static void Log(string feature, string detail)
    {
        OmegasGodTweaks.LogDebug(message: "[FeatureTest] " + feature + ": " + detail);
    }

    internal static string GetReligionId(Religion religion)
    {
        if (religion == null)
        {
            return "<null>";
        }

        return religion.id ?? "<empty>";
    }

    internal static string GetThingId(Thing thing)
    {
        if (thing == null)
        {
            return "<null>";
        }

        return thing.id ?? "<empty>";
    }
}

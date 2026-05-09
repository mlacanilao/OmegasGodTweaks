namespace OmegasGodTweaks;

internal static class ElementPatch
{
    internal static void IsActivePostfix(Element element, Card c, ref bool __result)
    {
        if (__result == true ||
            element == null ||
            c == null ||
            element.Value == 0 ||
            element.IsGlobalElement == false ||
            OmegasGodTweaksConfig.UnlockGodArtifactFactionEffects.Value == false)
        {
            return;
        }

        if (c.HasTag(tag: CTAG.godArtifact) == false)
        {
            return;
        }

        if (GodFaithStateService.IsJoinedGodId(godId: c.c_idDeity) == true)
        {
            __result = true;
            Thing? thing = c as Thing;
            FeatureTestLog.Log(
                feature: "Unlock God Artifact Faction Effects",
                detail: "enabled; Element.IsActive allowed global element=" +
                        element.id.ToString() +
                        ", thing=" +
                        FeatureTestLog.GetThingId(thing: thing));
        }
    }
}

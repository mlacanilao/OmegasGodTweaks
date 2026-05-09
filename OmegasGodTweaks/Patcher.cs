using System.Collections.Generic;
using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace OmegasGodTweaks;

internal static class Patcher
{
    internal static void PatchManualTargets(Harmony harmony)
    {
        PatchOfferingValueHelper(harmony: harmony);
        PatchAIIdleRunEnumerator(harmony: harmony);
    }

    private static void PatchOfferingValueHelper(Harmony harmony)
    {
        if (harmony == null)
        {
            return;
        }

        MethodInfo? target = ReligionPatch.FindOfferingValueHelper();
        if (target == null)
        {
            OmegasGodTweaks.LogError(message: "Religion.GetOfferingValue.SetValue manual patch skipped: local helper not found; category offering weight value cap remains vanilla.");
            return;
        }

        MethodInfo? transpiler = AccessTools.Method(type: typeof(ReligionPatch), name: nameof(ReligionPatch.GetOfferingValueSetValueTranspiler));
        if (transpiler == null)
        {
            OmegasGodTweaks.LogError(message: "Religion.GetOfferingValue.SetValue manual patch skipped: transpiler lookup failed.");
            return;
        }

        harmony.Patch(original: target, transpiler: new HarmonyMethod(method: transpiler));
    }

    private static void PatchAIIdleRunEnumerator(Harmony harmony)
    {
        if (harmony == null)
        {
            return;
        }

        MethodInfo? run = AccessTools.Method(type: typeof(AI_Idle), name: nameof(AI_Idle.Run));
        if (run == null)
        {
            OmegasGodTweaks.LogError(message: "AI_Idle.Run manual patch skipped: Run lookup failed.");
            return;
        }

        MethodInfo? moveNext = AccessTools.EnumeratorMoveNext(enumerator: run);
        if (moveNext == null)
        {
            OmegasGodTweaks.LogError(message: "AI_Idle.Run manual patch skipped: iterator MoveNext lookup failed.");
            return;
        }

        MethodInfo? transpiler = AccessTools.Method(type: typeof(AIIdlePatch), name: nameof(AIIdlePatch.RunTranspiler));
        if (transpiler == null)
        {
            OmegasGodTweaks.LogError(message: "AI_Idle.Run manual patch skipped: transpiler lookup failed.");
            return;
        }

        harmony.Patch(original: moveNext, transpiler: new HarmonyMethod(method: transpiler));
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(GameIO), methodName: nameof(GameIO.SaveGame))]
    internal static void GameIOSaveGamePostfix()
    {
        GameIOPatch.SaveGamePostfix();
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(GameIO), methodName: nameof(GameIO.PrepareSteamCloud))]
    internal static void GameIOPrepareSteamCloudPrefix(string id, string path)
    {
        GameIOPatch.PrepareSteamCloudPrefix(id: id, path: path);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(ReligionManager), methodName: nameof(ReligionManager.OnLoad))]
    internal static void ReligionManagerOnLoadPostfix()
    {
        ReligionManagerPatch.OnLoadPostfix();
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(Game), methodName: nameof(Game.Load))]
    internal static void GameLoadPostfix()
    {
        GamePatch.LoadPostfix();
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(ReligionManager), methodName: nameof(ReligionManager.OnCreateGame))]
    internal static void ReligionManagerOnCreateGamePostfix()
    {
        ReligionManagerPatch.OnCreateGamePostfix();
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.JoinFaith))]
    internal static void ReligionJoinFaithPrefix(Religion __instance, Chara c, Religion.ConvertType type, out bool __state)
    {
        __state = ReligionPatch.JoinFaithPrefix(religion: __instance, c: c, type: type);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.JoinFaith))]
    internal static void ReligionJoinFaithPostfix(Religion __instance, Chara c, Religion.ConvertType type, bool __state)
    {
        ReligionPatch.JoinFaithPostfix(religion: __instance, c: c, type: type, shouldClearFreshPiety: __state);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.LeaveFaith))]
    internal static bool ReligionLeaveFaithPrefix(Religion __instance, Chara c, Religion newFaith, Religion.ConvertType type)
    {
        return ReligionPatch.LeaveFaithPrefix(religion: __instance, c: c, newFaith: newFaith, type: type);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.Punish))]
    internal static bool ReligionPunishPrefix(Chara c)
    {
        return ReligionPatch.PunishPrefix(c: c);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.PunishTakeOver))]
    internal static bool ReligionPunishTakeOverPrefix(Chara c)
    {
        return ReligionPatch.PunishTakeOverPrefix(c: c);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.GetGiftRank))]
    internal static void ReligionGetGiftRankPrefix(Religion __instance)
    {
        ReligionPatch.GetGiftRankPrefix(religion: __instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.GetGiftRank))]
    internal static void ReligionGetGiftRankPostfix(Religion __instance, ref int __result)
    {
        ReligionPatch.GetGiftRankPostfix(religion: __instance, __result: ref __result);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.GetOfferingValue))]
    internal static IEnumerable<CodeInstruction> ReligionGetOfferingValueTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return ReligionPatch.GetOfferingValueTranspiler(instructions: instructions);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.GetOfferingValue))]
    internal static void ReligionGetOfferingValuePostfix(Religion __instance, Thing t, int num, ref int __result)
    {
        ReligionPatch.GetOfferingValuePostfix(religion: __instance, t: t, num: num, __result: ref __result);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.TryGetGift))]
    internal static bool ReligionTryGetGiftPrefix(Religion __instance)
    {
        return ReligionPatch.TryGetGiftPrefix(religion: __instance);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.TryGetGift))]
    internal static IEnumerable<CodeInstruction> ReligionTryGetGiftTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return ReligionPatch.TryGetGiftTranspiler(instructions: instructions);
    }

    [HarmonyFinalizer]
    [HarmonyPatch(declaringType: typeof(Religion), methodName: nameof(Religion.TryGetGift))]
    internal static Exception? ReligionTryGetGiftFinalizer(Religion __instance, Exception? __exception)
    {
        return ReligionPatch.TryGetGiftFinalizer(religion: __instance, exception: __exception);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(TraitAltar), methodName: nameof(TraitAltar.TrySetAct))]
    internal static void TraitAltarTrySetActPostfix(TraitAltar __instance, ActPlan p)
    {
        TraitAltarPatch.TrySetActPostfix(altar: __instance, p: p);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(TraitAltar), methodName: nameof(TraitAltar.CanOffer))]
    internal static void TraitAltarCanOfferPostfix(TraitAltar __instance, Card c, ref bool __result)
    {
        TraitAltarPatch.CanOfferPostfix(altar: __instance, c: c, __result: ref __result);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(TraitAltar), methodName: nameof(TraitAltar.OnOffer))]
    internal static bool TraitAltarOnOfferPrefix(TraitAltar __instance, Chara c, Thing t)
    {
        return TraitAltarPatch.OnOfferPrefix(altar: __instance, c: c, t: t);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(declaringType: typeof(TraitAltar), methodName: nameof(TraitAltar._OnOffer))]
    internal static IEnumerable<CodeInstruction> TraitAltarOnOfferCoreTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return TraitAltarPatch.OnOfferCoreTranspiler(instructions: instructions);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(TraitAltar), methodName: nameof(TraitAltar._OnOffer))]
    internal static void TraitAltarOnOfferCorePostfix(Chara c)
    {
        TraitAltarPatch.OnOfferCorePostfix(c: c);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(ActPray), methodName: nameof(ActPray.Pray))]
    internal static void ActPrayPrayPostfix(Chara c, bool passive)
    {
        ActPrayPatch.PrayPostfix(c: c, passive: passive);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(declaringType: typeof(ActPray), methodName: nameof(ActPray.TryPray))]
    internal static IEnumerable<CodeInstruction> ActPrayTryPrayTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return ActPrayPatch.TryPrayTranspiler(instructions: instructions);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(ActPray), methodName: nameof(ActPray.TryPray))]
    internal static void ActPrayTryPrayPrefix(Chara c, bool passive, out ActPrayPatch.TryPrayState __state)
    {
        __state = ActPrayPatch.TryPrayPrefix(c: c, passive: passive);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(ActPray), methodName: nameof(ActPray.TryPray))]
    internal static void ActPrayTryPrayPostfix(Chara c, bool passive, bool __result, ActPrayPatch.TryPrayState __state)
    {
        ActPrayPatch.TryPrayPostfix(c: c, passive: passive, __result: __result, state: __state);
    }

    [HarmonyFinalizer]
    [HarmonyPatch(declaringType: typeof(ActPray), methodName: nameof(ActPray.TryPray))]
    internal static Exception? ActPrayTryPrayFinalizer(ActPrayPatch.TryPrayState __state, Exception? __exception)
    {
        return ActPrayPatch.TryPrayFinalizer(state: __state, exception: __exception);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(Chara), methodName: nameof(Chara.RefreshFaithElement))]
    internal static void CharaRefreshFaithElementPostfix(Chara __instance)
    {
        CharaPatch.RefreshFaithElementPostfix(chara: __instance);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(declaringType: typeof(Chara), methodName: nameof(Chara.RefreshFaithElement))]
    internal static IEnumerable<CodeInstruction> CharaRefreshFaithElementTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return CharaPatch.RefreshFaithElementTranspiler(instructions: instructions);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(Element), methodName: nameof(Element.IsActive))]
    internal static void ElementIsActivePostfix(Element __instance, Card c, ref bool __result)
    {
        ElementPatch.IsActivePostfix(element: __instance, c: c, __result: ref __result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(ElementContainerFaction), methodName: nameof(ElementContainerFaction.IsEffective))]
    internal static void ElementContainerFactionIsEffectivePostfix(Thing t, ref bool __result)
    {
        ElementContainerPatch.FactionIsEffectivePostfix(t: t, __result: ref __result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(ElementContainerFaction), methodName: nameof(ElementContainerFaction.OnEquip), argumentTypes: new[] { typeof(Thing) })]
    internal static void ElementContainerFactionOnEquipPostfix(Thing t)
    {
        ElementContainerPatch.FactionOnEquipPostfix(t: t);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(ElementContainerFaction), methodName: nameof(ElementContainerFaction.OnUnequip), argumentTypes: new[] { typeof(Thing) })]
    internal static void ElementContainerFactionOnUnequipPrefix(Thing t, out bool __state)
    {
        ElementContainerPatch.FactionOnUnequipPrefix(t: t, __state: out __state);
    }

    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(ElementContainerFaction), methodName: nameof(ElementContainerFaction.OnUnequip), argumentTypes: new[] { typeof(Thing) })]
    internal static void ElementContainerFactionOnUnequipPostfix(Thing t, bool __state)
    {
        ElementContainerPatch.FactionOnUnequipPostfix(t: t, __state: __state);
    }

    [HarmonyFinalizer]
    [HarmonyPatch(declaringType: typeof(ElementContainerFaction), methodName: nameof(ElementContainerFaction.OnUnequip), argumentTypes: new[] { typeof(Thing) })]
    internal static Exception? ElementContainerFactionOnUnequipFinalizer(Exception? __exception)
    {
        return ElementContainerPatch.FactionOnUnequipFinalizer(exception: __exception);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(Card), methodName: nameof(Card.PurgeDuplicateArtifact))]
    internal static bool CardPurgeDuplicateArtifactPrefix(Thing af)
    {
        return CardPatch.PurgeDuplicateArtifactPrefix(artifact: af);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(declaringType: typeof(ButtonElement), methodName: nameof(ButtonElement.SetGrid))]
    internal static IEnumerable<CodeInstruction> ButtonElementSetGridTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return ButtonElementPatch.SetGridTranspiler(instructions: instructions);
    }

}

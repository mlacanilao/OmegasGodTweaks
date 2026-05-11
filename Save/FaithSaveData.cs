using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OmegasGodTweaks;

internal static class FaithSaveData
{
    private const string ChunkName = "OmegasGodTweaksFaithState";
    private const string LegacyFileName = "OmegasGodTweaksFaithState.json";
    private static FaithSaveModel current = new FaithSaveModel();
    private static bool eventsRegistered;
    private static bool loadFailed;

    internal static FaithSaveModel Current => current;

    private enum ContextLoadResult
    {
        Loaded,
        Missing,
        Failed
    }

    internal static void RegisterGameIOEvents()
    {
        if (eventsRegistered == true)
        {
            return;
        }

        eventsRegistered = true;
        BaseModManager.SubscribeEvent(eventId: EVENT.PostLoad, handler: OnPostLoad);
        BaseModManager.SubscribeEvent(eventId: EVENT.PreSave, handler: OnPreSave);
        BaseModManager.SubscribeEvent(eventId: EVENT.NewGame, handler: OnStartNewGame);
    }

    internal static void ResetForNewGame()
    {
        current = new FaithSaveModel();
        LogFeatureTestInfo(detail: "start-new reset received.");
    }

    internal static void LoadCurrent(GameIOContext? context)
    {
        if (context == null)
        {
            current = new FaithSaveModel();
            loadFailed = false;
            LogFeatureTestInfo(detail: "post-load event received with null context; source=empty.");
            return;
        }

        LogFeatureTestInfo(detail: "post-load event received.");
        ContextLoadResult contextLoadResult = TryLoadContext(context: context);
        if (contextLoadResult == ContextLoadResult.Loaded)
        {
            loadFailed = false;
            return;
        }

        if (contextLoadResult == ContextLoadResult.Missing)
        {
            ContextLoadResult legacyLoadResult = TryLoadLegacyJson(context: context);
            if (legacyLoadResult == ContextLoadResult.Loaded)
            {
                loadFailed = false;
                return;
            }

            if (legacyLoadResult == ContextLoadResult.Failed)
            {
                loadFailed = true;
                current = new FaithSaveModel();
                LogFeatureTestInfo(detail: "using empty in-memory state after failed legacy load; save will be skipped.");
                return;
            }
        }

        loadFailed = contextLoadResult == ContextLoadResult.Failed;
        current = new FaithSaveModel();
        LogFeatureTestInfo(detail: "loaded empty state; source=empty.");
    }

    internal static void SaveCurrent(GameIOContext? context)
    {
        if (context == null)
        {
            LogFeatureTestInfo(detail: "pre-save event received with null context; skipped save.");
            return;
        }

        if (loadFailed == true)
        {
            LogFeatureTestInfo(detail: "pre-save event received after failed load; skipped save.");
            return;
        }

        try
        {
            LogFeatureTestInfo(detail: "pre-save event received.");
            SnapshotCurrent();
            current.EnsureCollections();
            context.Save(chunkName: ChunkName, data: current, settings: null);
            LogFeatureTestInfo(
                detail: "saved context chunk; gods=" +
                        current.Gods.Count.ToString() +
                        ", appliedArtifacts=" +
                        current.AppliedJoinedArtifactIds.Count.ToString());
        }
        catch (Exception ex)
        {
            OmegasGodTweaks.LogDebug(message: $"Failed to save faith state context chunk: {ex}");
        }
    }

    internal static void SnapshotCurrent()
    {
        if (EClass.pc != null)
        {
            GodFaithStateService.SnapshotCurrentFaith(chara: EClass.pc, joined: true);
        }
    }

    internal static GodFaithState GetOrCreateState(string godId)
    {
        current.EnsureCollections();
        if (current.Gods.TryGetValue(key: godId, value: out GodFaithState? state) == true && state != null)
        {
            state.EnsureCollections();
            return state;
        }

        state = new GodFaithState();
        current.Gods[key: godId] = state;
        return state;
    }

    internal static bool HasState(string godId)
    {
        current.EnsureCollections();
        return current.Gods.ContainsKey(key: godId);
    }

    private static void OnPostLoad(object context)
    {
        if (context is not GameIOContext gameIOContext)
        {
            LoadCurrent(context: null);
            if (loadFailed == true)
            {
                return;
            }

            GodFaithStateService.ApplyLoadedState();
            return;
        }

        LoadCurrent(context: gameIOContext);
        if (loadFailed == true)
        {
            return;
        }

        GodFaithStateService.ApplyLoadedState();
    }

    private static void OnPreSave(object context)
    {
        if (context is not GameIOContext gameIOContext)
        {
            SaveCurrent(context: null);
            return;
        }

        SaveCurrent(context: gameIOContext);
    }

    private static void OnStartNewGame(object context)
    {
        loadFailed = false;
        ResetForNewGame();
    }

    private static ContextLoadResult TryLoadContext(GameIOContext context)
    {
        FileInfo? chunkFile;
        try
        {
            chunkFile = context.GetChunkFile(chunkName: ChunkName);
        }
        catch (Exception ex)
        {
            OmegasGodTweaks.LogDebug(message: $"Failed to locate faith state context chunk: {ex}");
            LogFeatureTestInfo(detail: "context chunk lookup failed; source=context.");
            return ContextLoadResult.Failed;
        }

        if (chunkFile == null || chunkFile.Exists == false)
        {
            LogFeatureTestInfo(detail: "context chunk missing; source=context.");
            return ContextLoadResult.Missing;
        }

        try
        {
            if (context.Load(chunkName: ChunkName, data: out FaithSaveModel loadedModel, settings: null) == false)
            {
                OmegasGodTweaks.LogDebug(message: "Faith state context chunk exists but did not load.");
                LogFeatureTestInfo(detail: "context chunk load failed; source=context.");
                return ContextLoadResult.Failed;
            }

            current = loadedModel ?? new FaithSaveModel();
            current.EnsureCollections();
            loadFailed = false;
            LogFeatureTestInfo(
                detail: "loaded state; source=context, gods=" +
                        current.Gods.Count.ToString() +
                        ", appliedArtifacts=" +
                        current.AppliedJoinedArtifactIds.Count.ToString());
            return ContextLoadResult.Loaded;
        }
        catch (Exception ex)
        {
            OmegasGodTweaks.LogDebug(message: $"Failed to load faith state context chunk: {ex}");
            LogFeatureTestInfo(detail: "context chunk load failed; source=context.");
            return ContextLoadResult.Failed;
        }
    }

    private static ContextLoadResult TryLoadLegacyJson(GameIOContext context)
    {
        string path = GetLegacyPath(context: context);
        if (string.IsNullOrWhiteSpace(value: path) == true || File.Exists(path: path) == false)
        {
            LogFeatureTestInfo(detail: "legacy JSON missing; source=legacy-json, path=" + path);
            return ContextLoadResult.Missing;
        }

        try
        {
            current = JsonConvert.DeserializeObject<FaithSaveModel>(value: File.ReadAllText(path: path)) ?? new FaithSaveModel();
            current.EnsureCollections();
            loadFailed = false;
            LogFeatureTestInfo(
                detail: "loaded state; source=legacy-json, path=" +
                        path +
                        ", gods=" +
                        current.Gods.Count.ToString() +
                        ", appliedArtifacts=" +
                        current.AppliedJoinedArtifactIds.Count.ToString());
            return ContextLoadResult.Loaded;
        }
        catch (Exception ex)
        {
            current = new FaithSaveModel();
            OmegasGodTweaks.LogDebug(message: $"Failed to load legacy faith state JSON: {ex}");
            LogFeatureTestInfo(detail: "legacy JSON load failed; source=legacy-json, path=" + path);
            return ContextLoadResult.Failed;
        }
    }

    private static string GetLegacyPath(GameIOContext context)
    {
        try
        {
            return context.GetFullPath(relativePath: LegacyFileName);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void LogFeatureTestInfo(string detail)
    {
        OmegasGodTweaks.LogDebug(message: "[FeatureTest] Save-Scoped Faith State: " + detail);
    }
}

internal sealed class FaithSaveModel
{
    public int Version { get; set; } = 1;

    public Dictionary<string, GodFaithState?> Gods { get; set; } = new Dictionary<string, GodFaithState?>();

    public HashSet<int> AppliedJoinedArtifactIds { get; set; } = new HashSet<int>();

    public Dictionary<int, Dictionary<int, int>?> AppliedJoinedArtifactBonuses { get; set; } =
        new Dictionary<int, Dictionary<int, int>?>();

    internal void EnsureCollections()
    {
        if (Gods == null)
        {
            Gods = new Dictionary<string, GodFaithState?>();
        }

        if (AppliedJoinedArtifactIds == null)
        {
            AppliedJoinedArtifactIds = new HashSet<int>();
        }

        if (AppliedJoinedArtifactBonuses == null)
        {
            AppliedJoinedArtifactBonuses = new Dictionary<int, Dictionary<int, int>?>();
        }

        List<string> invalidGodIds = new List<string>();
        foreach (KeyValuePair<string, GodFaithState?> pair in Gods)
        {
            if (pair.Value == null)
            {
                invalidGodIds.Add(item: pair.Key);
                continue;
            }

            pair.Value.EnsureCollections();
        }

        foreach (string godId in invalidGodIds)
        {
            Gods.Remove(key: godId);
        }

        List<int> invalidArtifactIds = new List<int>();
        foreach (KeyValuePair<int, Dictionary<int, int>?> pair in AppliedJoinedArtifactBonuses)
        {
            if (pair.Value == null)
            {
                invalidArtifactIds.Add(item: pair.Key);
            }
        }

        foreach (int artifactId in invalidArtifactIds)
        {
            AppliedJoinedArtifactBonuses.Remove(key: artifactId);
        }
    }
}

internal sealed class GodFaithState
{
    public bool Joined { get; set; }

    public int PietyBase { get; set; }

    public int PietyExp { get; set; }

    public int WorshipDays { get; set; }

    public int GiftRank { get; set; }

    public int ApostleRewardCount { get; set; }

    public int ArtifactRewardCount { get; set; }

    public Dictionary<string, int> RewardHistory { get; set; } = new Dictionary<string, int>();

    internal void EnsureCollections()
    {
        if (RewardHistory == null)
        {
            RewardHistory = new Dictionary<string, int>();
        }
    }
}

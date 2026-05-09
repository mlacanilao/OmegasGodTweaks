using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OmegasGodTweaks;

internal static class FaithSaveData
{
    private const string FileName = "OmegasGodTweaksFaithState.json";
    private static FaithSaveModel current = new FaithSaveModel();
    private static string loadedPath = string.Empty;

    internal static FaithSaveModel Current => current;

    internal static void ResetForNewGame()
    {
        current = new FaithSaveModel();
        loadedPath = GetCurrentPath();
        FeatureTestLog.Log(feature: "Save-Scoped Faith State", detail: "reset state for new game; path=" + loadedPath);
    }

    internal static void LoadCurrent()
    {
        string path = GetCurrentPath();
        loadedPath = path;
        if (string.IsNullOrWhiteSpace(value: path) == true || File.Exists(path: path) == false)
        {
            current = new FaithSaveModel();
            FeatureTestLog.Log(feature: "Save-Scoped Faith State", detail: "loaded empty state; path=" + path);
            return;
        }

        try
        {
            current = JsonConvert.DeserializeObject<FaithSaveModel>(value: File.ReadAllText(path: path)) ?? new FaithSaveModel();
            current.EnsureCollections();
            FeatureTestLog.Log(
                feature: "Save-Scoped Faith State",
                detail: "loaded state; path=" +
                        path +
                        ", gods=" +
                        current.Gods.Count.ToString() +
                        ", appliedArtifacts=" +
                        current.AppliedJoinedArtifactIds.Count.ToString());
        }
        catch (Exception ex)
        {
            current = new FaithSaveModel();
            OmegasGodTweaks.LogDebug(message: $"Failed to load save-scoped faith state: {ex}");
        }
    }

    internal static void SnapshotCurrent()
    {
        if (EClass.pc != null)
        {
            GodFaithStateService.SnapshotCurrentFaith(chara: EClass.pc, joined: true);
        }
    }

    internal static void SaveCurrent()
    {
        try
        {
            SnapshotCurrent();

            string path = GetCurrentPath();
            if (string.IsNullOrWhiteSpace(value: path) == true)
            {
                return;
            }

            string? directory = Path.GetDirectoryName(path: path);
            if (string.IsNullOrWhiteSpace(value: directory) == false)
            {
                Directory.CreateDirectory(path: directory!);
            }

            File.WriteAllText(path: path, contents: JsonConvert.SerializeObject(value: current, formatting: Formatting.Indented));
            loadedPath = path;
            FeatureTestLog.Log(
                feature: "Save-Scoped Faith State",
                detail: "saved state; path=" +
                        path +
                        ", gods=" +
                        current.Gods.Count.ToString() +
                        ", appliedArtifacts=" +
                        current.AppliedJoinedArtifactIds.Count.ToString());
        }
        catch (Exception ex)
        {
            OmegasGodTweaks.LogDebug(message: $"Failed to save faith state: {ex}");
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

    private static string GetCurrentPath()
    {
        try
        {
            if (EClass.core?.game == null || string.IsNullOrWhiteSpace(value: Game.id) == true)
            {
                return loadedPath;
            }

            return Path.Combine(path1: GameIO.pathCurrentSave, path2: FileName);
        }
        catch
        {
            return loadedPath;
        }
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

using System;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using OmegasGodTweaks.UI;

namespace OmegasGodTweaks;

internal static class ModInfo
{
    internal const string Guid = "omegaplatinum.elin.omegasgodtweaks";
    internal const string Name = "Omegas God Tweaks";
    internal const string Version = "1.1.0";
    internal const string ModOptionsGuid = "evilmask.elinplugins.modoptions";
}

[BepInPlugin(GUID: ModInfo.Guid, Name: ModInfo.Name, Version: ModInfo.Version)]
internal class OmegasGodTweaks : BaseUnityPlugin
{
    internal static OmegasGodTweaks? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        OmegasGodTweaksConfig.LoadConfig(config: Config);
        FaithSaveData.RegisterGameIOEvents();
        Harmony harmony = new Harmony(id: ModInfo.Guid);
        harmony.PatchAll(type: typeof(Patcher));
        Patcher.PatchManualTargets(harmony: harmony);

        if (HasModOptionsPlugin() == false)
        {
            return;
        }

        try
        {
            UIController.RegisterUI();
        }
        catch (Exception ex)
        {
            LogDebug(message: $"An error occurred during UI registration: {ex}");
        }
    }

    private static bool HasModOptionsPlugin()
    {
        try
        {
            foreach (object obj in ModManager.ListPluginObject)
            {
                if (obj is not BaseUnityPlugin plugin)
                {
                    continue;
                }

                if (plugin.Info.Metadata.GUID == ModInfo.ModOptionsGuid)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            LogDebug(message: $"Error while checking for Mod Options: {ex}");
            return false;
        }
    }

    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug(data: $"[{caller}] {message}");
    }

    internal static void LogInfo(object message)
    {
        Instance?.Logger.LogInfo(data: message);
    }

    internal static void LogError(object message)
    {
        Instance?.Logger.LogError(data: message);
    }
}

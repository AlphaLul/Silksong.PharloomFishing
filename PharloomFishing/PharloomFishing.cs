using BepInEx;
using HarmonyLib;

namespace PharloomFishing;

[BepInAutoPlugin(id: "alphalul.PharloomFishing", name: "Pharloom Fishing", version: "1.0.1")]
public partial class PharloomFishing : BaseUnityPlugin
{
    private static PharloomFishing _instance;
    private readonly Harmony _harmony = new(Id);

    private void Start()
    {
        _instance = this;
        DataManager.Initialize();
        if (!DataManager.Initialized) return;
        SaveManager.Initialize();

        _harmony.PatchAll();
    }

    public static void LogInfo(string message)
    {
        _instance.Logger.LogInfo(message);
    }
    
    public static void LogError(string message)
    {
        _instance.Logger.LogError(message);
    }
}
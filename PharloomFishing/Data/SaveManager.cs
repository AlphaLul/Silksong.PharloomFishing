using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace PharloomFishing;

public static class SaveManager
{
    public static SaveData CurrentSlotData = new();
    public static bool FailedToLoad;

    private static readonly string ModSavePath = Path.Combine(((DesktopPlatform)Platform.Current).saveDirPath, $"Mods/{PharloomFishing.Id}");
    
    public static void Initialize()
    {
        if (!Directory.Exists(ModSavePath)) Directory.CreateDirectory(ModSavePath);
    }

    private static string GetSaveSlotPath(int saveSlot)
    {
        return Path.Combine(ModSavePath, $"user{saveSlot}.dat");
    }

    public static void CreateSaveSlotData(int saveSlot)
    {
        if (File.Exists(GetSaveSlotPath(saveSlot))) return;
        
        SaveJson blankSaveJson = new(
            version: PharloomFishing.Version,
            hasFishingKit: false,
            hasSeenFishingKit: false,
            hasFishingSpotPins: false,
            crawState: CrawState.NotMet,
            inventoryData: [],
            logData: DataManager.Fishes.ToDictionary(
                kvp => kvp.Key,
                _ => new FishingLogEntryJson
                (
                    numCaught: 0,
                    numFed: 0,
                    largestCaught: 0f,
                    hasSeenEntry: false
                )
            )
        );
        string jsonText = JsonConvert.SerializeObject(blankSaveJson, Formatting.Indented);
        File.WriteAllText(GetSaveSlotPath(saveSlot), jsonText);
    }
    
    public static void LoadSaveSlotData(int saveSlot)
    {
        if (!File.Exists(GetSaveSlotPath(saveSlot)))
        {
            PharloomFishing.LogError($"Save slot {saveSlot} data not found, making new one");
            CreateSaveSlotData(saveSlot);
        }

        SaveJson saveJson;
        try
        {
            saveJson = JsonConvert.DeserializeObject<SaveJson>(File.ReadAllText(GetSaveSlotPath(saveSlot)));
        }
        catch (Exception e)
        {
            PharloomFishing.LogError(e.Message);
            CurrentSlotData = new SaveData();
            FailedToLoad = true;
            return;
        }
        
        CurrentSlotData = new SaveData
        {
            Version = saveJson.Version,
            HasFishingKit = saveJson.HasFishingKit,
            HasSeenFishingKit = saveJson.HasSeenFishingKit,
            HasFishingSpotPins = saveJson.HasFishingSpotPins,
            CrawState = (CrawState)saveJson.CrawState,
            Inventory = saveJson.InventoryData
                .Where(data => DataManager.Fishes.ContainsKey(data.ID)) //Ignore removed fishes
                .Select(data => new FishingLogCollectable
                (
                    fish: DataManager.Fishes[data.ID],
                    weight: data.Weight,
                    locked: data.Locked
                )).ToList(),
            LogEntries = DataManager.Fishes.ToDictionary(
                kvp => kvp.Value,
                kvp =>
                {
                    if (saveJson.LogData.TryGetValue(kvp.Key, out FishingLogEntryJson jsonEntry))
                        return new FishingLogEntry
                        (
                            fish: kvp.Value,
                            numCaught: jsonEntry.NumCaught,
                            numFed: jsonEntry.NumFed,
                            largestCaught: jsonEntry.LargestCaught,
                            hasSeenEntry: jsonEntry.HasSeenEntry
                        );
                    return new FishingLogEntry //Make entries for new fish that aren't in the save file
                    (
                        fish: kvp.Value,
                        numCaught: 0,
                        numFed: 0,
                        largestCaught: 0f,
                        hasSeenEntry: false
                    );
                }
            )
        };

        FailedToLoad = false;
        PharloomFishing.LogInfo($"Loaded save slot {saveSlot} data");
    }

    public static void WriteSaveSlotData(int saveSlot)
    {
        if (FailedToLoad) return;
        
        SaveJson saveJson = new(
            version: PharloomFishing.Version,
            hasFishingKit: CurrentSlotData.HasFishingKit,
            hasSeenFishingKit: CurrentSlotData.HasSeenFishingKit,
            hasFishingSpotPins: CurrentSlotData.HasFishingSpotPins,
            crawState: CurrentSlotData.CrawState,
            inventoryData: CurrentSlotData.Inventory.Select(fishItem => new FishingLogCollectableJson
            (
                id: fishItem.Fish.ID,
                weight: fishItem.Weight,
                locked: fishItem.Locked
            )).ToArray(),
            logData: CurrentSlotData.LogEntries.ToDictionary(
                kvp => kvp.Key.ID,
                kvp => new FishingLogEntryJson
                (
                    numCaught: kvp.Value.NumCaught,
                    numFed: kvp.Value.NumFed,
                    largestCaught: kvp.Value.LargestCaught,
                    hasSeenEntry: kvp.Value.HasSeenEntry
                )
            )
        );
        string jsonText = JsonConvert.SerializeObject(saveJson, Formatting.Indented);
        File.WriteAllText(GetSaveSlotPath(saveSlot), jsonText);
        PharloomFishing.LogInfo($"Saved save slot {saveSlot} data");
    }

    public static void DeleteSaveSlotData(int saveSlot)
    {
        if (File.Exists(GetSaveSlotPath(saveSlot))) PharloomFishing.LogInfo($"Deleted save slot {saveSlot} data");
        File.Delete(GetSaveSlotPath(saveSlot));
    }
}
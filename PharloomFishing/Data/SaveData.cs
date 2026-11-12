using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace PharloomFishing;

[JsonConverter(typeof(StringEnumConverter))]
public enum CrawState
{
    NotMet,
    GaveFishingKit
}

public class SaveData
{
    public string Version;
    public bool HasFishingKit, HasSeenFishingKit, HasFishingSpotPins;
    public CrawState CrawState;
    public List<FishingLogCollectable> Inventory = [];
    public Dictionary<Fish, FishingLogEntry> LogEntries = new();

    public int NumUnlockedFish()
    {
        return Inventory.Count(fish => !fish.Locked);
    }

    public int SellFish()
    {
        int total = 0;
        foreach (FishingLogCollectable? fish in Inventory.Where(fish => !fish.Locked).ToList())
        {
            int value = Mathf.RoundToInt
                (Mathf.Lerp(DataManager.Values[(int)fish.Fish.Rarity].Start, 
                    DataManager.Values[(int)fish.Fish.Rarity].End, 
                    fish.Weight));
            SaveManager.CurrentSlotData.LogEntries[fish.Fish].NumFed++;
            Inventory.Remove(fish);
            total += value;
        }
        return total;
    }
}

public class FishingLogCollectable(Fish fish, float weight, bool locked)
{
    public readonly Fish Fish = fish;
    public readonly float Weight = weight;
    public bool Locked = locked;
}

public class FishingLogEntry (Fish fish, int numCaught, int numFed, float largestCaught, bool hasSeenEntry)
{
    public readonly Fish Fish = fish;
    public int NumCaught = numCaught;
    public int NumFed = numFed;
    public float LargestCaught = largestCaught;
    public bool HasSeenEntry = hasSeenEntry;
}

public struct SaveJson(
    string version,
    bool hasFishingKit,
    bool hasSeenFishingKit,
    bool hasFishingSpotPins,
    CrawState crawState,
    FishingLogCollectableJson[] inventoryData,
    Dictionary<string, FishingLogEntryJson> logData)
{
    public readonly string Version = version;
    public readonly bool HasFishingKit = hasFishingKit, 
        HasSeenFishingKit = hasSeenFishingKit, 
        HasFishingSpotPins = hasFishingSpotPins;
    public readonly CrawState CrawState = crawState;
    public readonly FishingLogCollectableJson[] InventoryData = inventoryData;
    public readonly Dictionary<string, FishingLogEntryJson> LogData = logData;
}

public struct FishingLogCollectableJson(string id, float weight, bool locked)
{
    public readonly string ID = id;
    public readonly float Weight = weight;
    public readonly bool Locked = locked;
}

public struct FishingLogEntryJson(int numCaught, int numFed, float largestCaught, bool hasSeenEntry)
{
    public readonly int NumCaught = numCaught;
    public readonly int NumFed = numFed;
    public readonly float LargestCaught = largestCaught;
    public readonly bool HasSeenEntry = hasSeenEntry;
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PharloomFishing;

public class FishingSpot(
    string worldRoom,
    string mapRoom,
    Vector2 worldPos,
    Vector2 mapPos,
    bool facingRight,
    float rippleOffsetY,
    float rippleOffsetZ,
    Fish[] fishes)
{
    public readonly string WorldRoom = worldRoom, MapRoom = mapRoom;
    public readonly Vector2 WorldPos = worldPos, MapPos = mapPos;
    public readonly bool FacingRight = facingRight;
    public readonly Vector3 RippleOffset = new Vector3(0, rippleOffsetY, rippleOffsetZ);
    public readonly Fish[] Fishes = fishes;
    private Fish? _lastFishCaught;

    public (Fish, float, bool, bool) CatchFish(List<Fish> availableFishes, bool isQuickCatch)
    {
        Fish? fishCaught = null;
        if (_lastFishCaught != null && !availableFishes.Contains(_lastFishCaught)) _lastFishCaught = null;
        
        bool disallowLastFish = _lastFishCaught != null && Random.value > 0.5f;
        List<Fish> possibleFishes = disallowLastFish && _lastFishCaught != null
            ? [.. availableFishes.Where(fish => fish != _lastFishCaught)]
            : availableFishes;
        
        int[] normalProbabilities = disallowLastFish && _lastFishCaught?.Rarity == FishRarity.Common 
            ? DataManager.LastCommonProbabilities 
            : DataManager.Probabilities;
        int[] probabilities = isQuickCatch ? DataManager.QuickProbabilities : normalProbabilities;
        
        int totalProbability = possibleFishes.Sum(fish => probabilities[(int)fish.Rarity]);
        
        int roll = Random.Range(0, totalProbability);
        int cumulativeProbability = 0;
        
        foreach (Fish fish in possibleFishes)
        {
            cumulativeProbability += probabilities[(int)fish.Rarity];
            if (roll >= cumulativeProbability) continue;
            
            fishCaught = fish;
            break;
        }
        
        fishCaught ??= possibleFishes[0];
        _lastFishCaught = fishCaught;
        
        float fishWeight = Random.value;
        bool inventoryFull = SaveManager.CurrentSlotData.Inventory.Count >= DataManager.MaxInventory;
        if (!inventoryFull)
        {
            SaveManager.CurrentSlotData.Inventory.Add(new FishingLogCollectable
            (
                fish: fishCaught,
                weight: fishWeight,
                locked: false
            ));
        }
        SaveManager.CurrentSlotData.LogEntries[fishCaught].NumCaught++;
        FishingLogEntry logEntry = SaveManager.CurrentSlotData.LogEntries[fishCaught];
        bool newHeaviest = fishWeight > logEntry.LargestCaught;
        logEntry.LargestCaught = Mathf.Max(logEntry.LargestCaught, fishWeight);
        
        return (fishCaught, fishWeight, newHeaviest, inventoryFull);
    }
}

public struct FishingSpotJson(string worldRoom, string mapRoom, float[] worldPos, float[] mapPos, bool facingRight, float rippleOffsetY, float rippleOffsetZ, string[] fishIds)
{
    public readonly string WorldRoom = worldRoom, MapRoom = mapRoom;
    public readonly float[] WorldPos = worldPos, MapPos = mapPos;
    public readonly bool FacingRight = facingRight;
    public readonly float RippleOffsetY = rippleOffsetY;
    public readonly float RippleOffsetZ = rippleOffsetZ;
    public readonly string[] FishIds = fishIds;
}
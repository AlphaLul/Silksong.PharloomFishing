using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TeamCherry.Localization;
using TeamCherry.SharedUtils;
using UnityEngine;

namespace PharloomFishing;

public static class DataManager
{
    public const int MaxInventory = 42;
    public static readonly MinMaxInt[] Values = [
        new(5, 15),
        new(10, 25),
        new(20, 40),
        new(40, 100)
    ];
    public static readonly int[] Probabilities = [14, 9, 3, 1];
    public static readonly int[] LastCommonProbabilities = [19, 9, 3, 1];
    public static readonly int[] QuickProbabilities = [0, 4, 4, 2];
    public const float BaseQuickCatchWindow = 0.17f;
    public const float CatchWindow = 0.55f;

    public static readonly Dictionary<string, Fish> Fishes = new();
    public static readonly List<FishingSpot> FishingSpots = [];

    public static readonly CatchFishPopup CatchFishPopup = ScriptableObject.CreateInstance<CatchFishPopup>();
    public static readonly FishingKitCollectableItem FishingKitItem = ScriptableObject.CreateInstance<FishingKitCollectableItem>();
    
    public static bool Initialized = true;

    private static Dictionary<string, FishJson> _fishJson;
    private static FishingSpotJson[] _fishingSpotJson;
    private static Dictionary<string, int> _fishOrder;

    public static void Initialize()
    {
        try
        {
            string fishJsonText = ResourceUtil.LoadEmbeddedText($"{ResourceUtil.DataPath}fishes.json");
            _fishJson = JsonConvert.DeserializeObject<Dictionary<string, FishJson>>(fishJsonText) ?? throw new InvalidOperationException();
            string fishingSpotJsonText = ResourceUtil.LoadEmbeddedText($"{ResourceUtil.DataPath}fishingSpots.json");
            _fishingSpotJson = JsonConvert.DeserializeObject<FishingSpotJson[]>(fishingSpotJsonText) ?? throw new InvalidOperationException();
        }
        catch (Exception e)
        {
            PharloomFishing.LogError(e.Message);
            Initialized = false;
            return;
        }
        
        _fishOrder = _fishingSpotJson
            .SelectMany(fishingSpot => fishingSpot.FishIds)
            .Distinct()
            .Select((fishId, index) => new { fishId, index})
            .ToDictionary(x => x.fishId, x => x.index);
        
        ReadFishData();
        ReadFishingSpotData();

        FishingKitItem.icon = ResourceUtil.LoadEmbeddedSprite("fishing_kit", 100f, ResourceUtil.AssetType.UI);
        FishingKitItem.displayName = new LocalisedString("UI", "INV_NAME_FISHING_KIT");
        FishingKitItem.description = new LocalisedString("UI", "INV_DESC_FISHING_KIT");
        FishingKitItem.useResponses = [];
    }
    
    private static void ReadFishData()
    {
        foreach ((string key, FishJson value) in _fishJson
                     .OrderBy(kvp => kvp.Value.Rarity)
                     .ThenBy(kvp => _fishOrder[kvp.Key]))
        {
            Fish fish = new(
                id: key,
                name: new LocalisedString("Fishes", $"NAME_{key.ToUpper()}"),
                description: new LocalisedString("Fishes", $"DESC_{key.ToUpper()}"),
                rarity: value.Rarity,
                weight: new MinMaxFloat(value.Weight[0], value.Weight[1]),
                fishSprite: ResourceUtil.LoadEmbeddedSprite(key, 100f, ResourceUtil.AssetType.Fishes),
                logSprite: ResourceUtil.LoadEmbeddedSprite($"{key}_icon", 100f, ResourceUtil.AssetType.Fishes),
                catchClip: ResourceUtil.LoadEmbeddedAudioClipOrNull(key, ResourceUtil.AssetType.Fishes),
                condition: value.Condition
            );
            
            Fishes.Add(key, fish);
        }
    }

    private static void ReadFishingSpotData()
    {
        foreach (FishingSpotJson jsonEntry in _fishingSpotJson)
        {
            FishingSpot fishingSpot = new(
                worldRoom: jsonEntry.WorldRoom,
                mapRoom: jsonEntry.MapRoom,
                worldPos: new Vector2(jsonEntry.WorldPos[0], jsonEntry.WorldPos[1]),
                mapPos: new Vector2(jsonEntry.MapPos[0], jsonEntry.MapPos[1]),
                facingRight: jsonEntry.FacingRight,
                rippleOffsetY: jsonEntry.RippleOffsetY,
                rippleOffsetZ: jsonEntry.RippleOffsetZ,
                fishes: jsonEntry.FishIds.Select(id => Fishes[id]).ToArray()
            );
            
            FishingSpots.Add(fishingSpot);
        }
    }
}
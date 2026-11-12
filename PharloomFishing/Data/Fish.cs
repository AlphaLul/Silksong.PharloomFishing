using System;
using System.Globalization;
using GlobalSettings;
using TeamCherry.Localization;
using TeamCherry.SharedUtils;
using UnityEngine;

namespace PharloomFishing;

public class Fish(
    string id,
    LocalisedString name,
    LocalisedString description,
    FishRarity rarity,
    MinMaxFloat weight,
    Sprite fishSprite,
    Sprite logSprite,
    AudioClip? catchClip,
    string? condition)
{
    public readonly string ID = id;
    public readonly LocalisedString Name = name, Description = description;
    
    public readonly FishRarity Rarity = rarity;
    public readonly MinMaxFloat Weight = weight;
    
    public readonly Sprite FishSprite = fishSprite, LogSprite = logSprite;
    public readonly AudioClip? CatchClip = catchClip;
    public readonly string? Condition = condition;

    public Color GetRarityColor()
    {
        return Rarity switch
        {
            FishRarity.Common => UI.GetToolTypeColor(ToolItemType.Skill),
            FishRarity.Rare => UI.GetToolTypeColor(ToolItemType.Blue),
            FishRarity.Legendary => UI.GetToolTypeColor(ToolItemType.Yellow),
            FishRarity.Mythical => UI.GetToolTypeColor(ToolItemType.Red),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string GetWeightText(float t)
    {
        return Mathf.Lerp(Weight.Start, Weight.End, t).ToString("0.0#", CultureInfo.InvariantCulture);
    }
}

public struct FishJson(FishRarity rarity, float[] weight, string condition)
{
    public readonly FishRarity Rarity = rarity;
    public readonly float[] Weight = weight;
    public readonly string? Condition = condition;
}

public enum FishRarity
{
    Common,
    Rare,
    Legendary,
    Mythical
}
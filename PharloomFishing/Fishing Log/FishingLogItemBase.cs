using GlobalSettings;
using UnityEngine;

namespace PharloomFishing;

public abstract class FishingLogItemBase : InventoryItemUpdateable
{
    public Fish? fish;
    public GameObject emptyIcon;
    public SpriteRenderer fishIcon;

    public abstract bool ShouldDisplayFish();
    
    public override Color? CursorColor => fish?.GetRarityColor() ?? UI.GetToolTypeColor(ToolItemType.Skill);
}
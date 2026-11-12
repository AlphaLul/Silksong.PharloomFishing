using UnityEngine;

namespace PharloomFishing;

public class FishingLogEntryItem : FishingLogItemBase
{
    public SpriteRenderer frame;
    public GameObject mask;
    private FishingLogEntry _entry;

    public override string DisplayName => _entry.NumCaught == 0 ? string.Empty : fish?.Name ?? string.Empty;

    public override string Description => _entry.NumCaught == 0 ? string.Empty : fish?.Description ?? string.Empty;

    public override bool IsSeen
    {
        get => _entry.NumCaught == 0 || _entry.HasSeenEntry;
        set => _entry.HasSeenEntry = value;
    }
    public override bool ShouldDisplayFish()
    {
        return fish != null && _entry.NumCaught > 0;
    }

    public void SetupEntry(FishingLogEntry entry)
    {
        _entry = entry;
        Fish entryFish = entry.Fish;
        fish = entryFish;
        gameObject.name = $"Log Entry ({entryFish.Name})";
        isNew = _entry.NumCaught > 0 && !_entry.HasSeenEntry; //Some weird bug is setting isNew to true by default
        
        emptyIcon.SetActive(_entry.NumCaught == 0);
        fishIcon.gameObject.SetActive(_entry.NumCaught > 0);
        frame.gameObject.SetActive(_entry.NumCaught > 0);
        mask.SetActive(_entry.NumCaught > 0);
        
        fishIcon.sprite = entryFish.LogSprite;
        frame.color = entryFish.GetRarityColor();
        base.UpdateDisplay();
    }
}
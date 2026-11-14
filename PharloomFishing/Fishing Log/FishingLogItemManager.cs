using System.Collections.Generic;
using System.Linq;
using GlobalSettings;
using TeamCherry.Localization;
using TMProOld;
using UnityEngine;

namespace PharloomFishing;

public class FishingLogItemManager : InventoryItemManager, IInventoryPaneAvailabilityProvider
{
    public FishingLogEntryItem entryTemplate;
    public FishingLogCollectableItem collectableTemplate;
    public InventoryItemGrid logGrid, collectableGrid;
    public SpriteRenderer descriptionSpriteRenderer;
    public TextMeshPro rarityText, completionText, numCaughtText, largestCaughtText, controlText;
    public AudioEvent lockAudioEvent, unlockAudioEvent;
    
    private bool _isInventorySetup;
    
    public bool IsAvailable()
    {
        return SaveManager.CurrentSlotData.HasFishingKit;
    }

    private static FishingLogEntry[] GetEntries()
    {
        return !PlayerData.instance.ConstructedFarsight 
            ? SaveManager.CurrentSlotData.LogEntries.Values.Where(entry => entry.NumCaught > 0).ToArray()
            : SaveManager.CurrentSlotData.LogEntries.Values.ToArray();
    }

    private static FishingLogCollectable[] GetInventory()
    {
        FishingLogCollectable[] inventory = new FishingLogCollectable[DataManager.MaxInventory];
        int count = Mathf.Min(SaveManager.CurrentSlotData.Inventory.Count, DataManager.MaxInventory);
        for (int i = 0; i < count; i++)
        {
            inventory[i] = SaveManager.CurrentSlotData.Inventory[i];
        }

        return inventory;
    }

    public override void Awake()
    {
        base.Awake();
        GetComponent<InventoryPaneBase>().OnPaneStart += UpdatePane;
        SetDisplay(gameObject);
        UpdatePane();
    }

    private void UpdatePane()
    {
        UpdateEntryList();
        UpdateCollectableList();
        
        largestCaughtText.gameObject.SetActive(PlayerData.instance.ConstructedFarsight);
        completionText.gameObject.SetActive(PlayerData.instance.ConstructedFarsight);
        completionText.text =
            $"{SaveManager.CurrentSlotData.LogEntries.Values.Count(entry => entry.NumCaught > 0)} / {DataManager.Fishes.Count}";
    }

    private void UpdateEntryList()
    {
        List<FishingLogEntryItem> list = new List<FishingLogEntryItem>();
        foreach (Transform child in logGrid.transform)
        {
            FishingLogEntryItem item = child.GetComponent<FishingLogEntryItem>();
            if (item != null && item != entryTemplate)
            {
                list.Add(item);
            }
        }
        
        FishingLogEntry[] entries = GetEntries();
        while (list.Count < entries.Length)
        {
            FishingLogEntryItem newItem = Instantiate(entryTemplate, logGrid.transform);
            newItem.gameObject.SetActive(true);
            list.Add(newItem);
        }
        for (int i = 0; i < entries.Length; i++)
        {
            list[i].gameObject.SetActive(true);
            list[i].SetupEntry(entries[i]);
        }
        
        List<InventoryItemGrid.GridSection> gridSections =
        [
            new()
            {
                Items = list.ToList<InventoryItemSelectableDirectional>()
            }
        ];
        logGrid.Setup(gridSections);
    }

    private void UpdateCollectableList()
    {
        List<FishingLogCollectableItem> list = new List<FishingLogCollectableItem>(collectableGrid.transform.childCount);
        foreach (object obj in collectableGrid.transform)
        {
            FishingLogCollectableItem component = ((Transform)obj).GetComponent<FishingLogCollectableItem>();
            if (component != null)
            {
                list.Add(component);
            }
        }
        list.Remove(collectableTemplate);
        if (!Application.isPlaying || !_isInventorySetup)
        {
            _isInventorySetup = true;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                DestroyImmediate(list[i].gameObject);
            }
            list.Clear();
        }
        
        int j;
        FishingLogCollectable[] collectables = GetInventory();
        for (j = collectables.Length - list.Count; j > 0; j--)
        {
            FishingLogCollectableItem collectableItem = Instantiate(collectableTemplate, collectableGrid.transform);
            list.Add(collectableItem);
        }
        while (j < 0)
        {
            FishingLogCollectableItem collectableItem = list.Last();
            collectableItem.gameObject.SetActive(false);
            list.Remove(collectableItem);
            j++;
        }
        
        for (int i = 0; i < collectables.Length; i++)
        {
            list[i].gameObject.SetActive(true);
            list[i].SetupCollectable(collectables[i]);
        }

        List<InventoryItemGrid.GridSection> gridSections =
        [
            new()
            {
                Items = list.ToList<InventoryItemSelectableDirectional>()
            }
        ];
        collectableGrid.Setup(gridSections);
    }

    public override void InstantScroll()
    {
        if (CurrentSelected as FishingLogEntryItem == null) return;
        logGrid.ScrollTo(CurrentSelected, true);
    }

    public override void SetDisplay(GameObject selectedGameObject)
    {
        base.SetDisplay(selectedGameObject);
        descriptionSpriteRenderer.gameObject.SetActive(false);
        rarityText.text = string.Empty;
        numCaughtText.text = string.Empty;
        largestCaughtText.text = string.Empty;
    }

    public override void SetDisplay(InventoryItemSelectable selectable)
    {
        base.SetDisplay(selectable);
        FishingLogItemBase? item = selectable as FishingLogItemBase;
        if (item == null) return;
        
        FishingLogCollectableItem? collectableItem = item as FishingLogCollectableItem;
        if (collectableItem != null)
        {
            controlText.text = collectableItem.locked 
                ? new LocalisedString("UI", "CTRL_UNLOCK") 
                : new LocalisedString("UI", "CTRL_LOCK");
            controlText.transform.parent.gameObject.SetActive(collectableItem.fish != null);
        }
        else
        {
            controlText.text = new LocalisedString("UI", "CTRL_NEXT");
            controlText.transform.parent.gameObject.SetActive(true);
        }
        
        if (!item.ShouldDisplayFish()) return;
        if (item.fish == null) return;
        
        descriptionSpriteRenderer.gameObject.SetActive(true);
        descriptionSpriteRenderer.sprite = item.fish.FishSprite;
        rarityText.text = new LocalisedString("UI", $"RARITY_{item.fish.Rarity.ToString().ToUpper()}");
        rarityText.color = item.fish.GetRarityColor();

        LocalisedString numCaughtFormat = new("UI", "FISH_NUM_CAUGHT");
        LocalisedString heaviestCaughtFormat = new("UI", "FISH_LARGEST_CAUGHT");
        FishingLogEntry entry = SaveManager.CurrentSlotData.LogEntries[item.fish];
        numCaughtText.text = $"{string.Format(numCaughtFormat, entry.NumCaught)}";
        Color largestColor = entry.LargestCaught >= 1 ? UI.MaxItemsTextColor : Color.white;
        largestCaughtText.text =
            $"{string.Format(heaviestCaughtFormat, entry.Fish.GetWeightText(entry.LargestCaught), ColorUtility.ToHtmlStringRGB(largestColor))}";
    }
    
    public override bool SubmitButtonSelected()
    {
        if (IsActionsBlocked) return false;

        InventoryItemSelectable? currentSelectable = CurrentSelected;
        if (currentSelectable as FishingLogEntryItem != null)
        {
            InventoryItemSelectable nextSelectable;
            if (currentSelectable == logGrid.GetLast())
                nextSelectable = logGrid.GetFirst();
            else if (currentSelectable.GetNextSelectable(SelectionDirection.Right) as FishingLogEntryItem)
                nextSelectable = currentSelectable.GetNextSelectable(SelectionDirection.Right);
            else
            {
                nextSelectable = currentSelectable.GetNextSelectable(SelectionDirection.Down);
                while (nextSelectable.GetNextSelectable(SelectionDirection.Left) as FishingLogEntryItem)
                {
                    nextSelectable = nextSelectable.GetNextSelectable(SelectionDirection.Left);
                }
            }
            
            SetSelected(nextSelectable, null);
            PlayMoveSound();
            return true;
        }
        return base.SubmitButtonSelected();
    }

    //isSubmitHeld seems to be broken so I have to do this instead to prevent spamming lock unlock while holding
    private void Update()
    {
        if (IsActionsBlocked) return;
        
        HeroActions inputActions = ManagerSingleton<InputHandler>.Instance.inputActions;
        Platform.MenuActions menuAction = Platform.Current.GetMenuAction(inputActions);
        if (menuAction != Platform.MenuActions.Submit) return;
        
        FishingLogCollectableItem? collectableItem = CurrentSelected as FishingLogCollectableItem;
        if (collectableItem == null) return;

        if (!collectableItem.ToggleLock()) return;
        if (collectableItem.locked)
        {
            lockAudioEvent.SpawnAndPlayOneShot(Audio.DefaultUIAudioSourcePrefab, transform.position);
            controlText.text = new LocalisedString("UI", "CTRL_UNLOCK");
        }
        else
        {
            unlockAudioEvent.SpawnAndPlayOneShot(Audio.DefaultUIAudioSourcePrefab, transform.position);
            controlText.text = new LocalisedString("UI", "CTRL_LOCK");
        }
    }
}
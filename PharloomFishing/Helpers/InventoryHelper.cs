using System;
using TeamCherry.Localization;
using TeamCherry.NestedFadeGroup;
using TMProOld;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace PharloomFishing;

public static class InventoryHelper
{
    public const string FishInventoryName = "PharloomFishing_Fish Inventory";
    
    public static void MakeFishingLogPane(InventoryPaneList paneList)
    {
        //Make new pane
        GameObject journalObject = paneList.transform.Find("Journal").gameObject;
        InventoryPane pane = Instantiate(journalObject, paneList.transform).GetComponent<InventoryPane>();
        pane.displayName = new LocalisedString("UI", "PANE_FISHING_LOG");
        JournalItemManager oldItemManager = pane.GetComponent<JournalItemManager>();
        FishingLogItemManager itemManager = pane.gameObject.AddComponent<FishingLogItemManager>();
        itemManager.cursor = oldItemManager.cursor;
        itemManager.cursorParentOverride = oldItemManager.cursorParentOverride;
        itemManager.cursorPrefab = oldItemManager.cursorPrefab;
        itemManager.defaultSelectables = oldItemManager.defaultSelectables;
        itemManager.leftMostSelectables = oldItemManager.leftMostSelectables;
        itemManager.rightMostSelectables = oldItemManager.rightMostSelectables;
        itemManager.nameText = oldItemManager.nameText;
        itemManager.logGrid = oldItemManager.itemList;
        
        itemManager.controlText = pane.transform.Find("Next Action/Text").GetComponent<TextMeshPro>();
        GameObject textCompletionObject = pane.transform.Find("Text Completion").gameObject;
        GameObject textEncounteredObject = textCompletionObject.transform.Find("Text Encountered").gameObject;
        textCompletionObject.transform.SetLocalPositionX(12.62f);
        Destroy(textCompletionObject.transform.Find("Amount").gameObject);
        Destroy(textCompletionObject.transform.Find("Total").gameObject);
        Destroy(textEncounteredObject.transform.Find("Amount").gameObject);
        Destroy(textEncounteredObject.transform.Find("Total").gameObject);
        itemManager.largestCaughtText = textCompletionObject.GetComponent<TextMeshPro>();
        itemManager.numCaughtText = textEncounteredObject.GetComponent<TextMeshPro>();
        
        MapMarkerMenu mapMarkerMenu = paneList.transform.Find("Map").GetComponentInChildren<MapMarkerMenu>();
        itemManager.lockAudioEvent = new AudioEvent
        {
            Clip = mapMarkerMenu.placeClip,
            PitchMin = 1f,
            PitchMax = 1f,
            Volume = 1f
        };
        itemManager.unlockAudioEvent = new AudioEvent
        {
            Clip = mapMarkerMenu.removeClip,
            PitchMin = 1f,
            PitchMax = 1f,
            Volume = 1f
        };
        
        pane.availabilityProvider = itemManager;
        
        //Make item grids
        GameObject logParent = pane.transform.Find("Enemy List Parent").gameObject;
        InventoryItemGrid logGrid = logParent.GetComponentInChildren<InventoryItemGrid>();
        logGrid.RowSplit = 2;
        logGrid.ItemOffset = new Vector2(1.9f, -1.9f);
        logGrid.transform.SetLocalPositionX(-0.4f);
        logGrid.itemManager = itemManager;
        ScrollView logScrollView = logGrid.GetComponent<ScrollView>();
        logScrollView.viewBounds.center = new Vector2(-5.3f, -7.7f);
        logScrollView.viewBounds.extents = new Vector2(3.13f, 5.2f);
        
        InventoryItemGrid collectableGrid = Instantiate(paneList.transform.Find("Inv/Equipment"), pane.transform).GetComponent<InventoryItemGrid>();
        Destroy(collectableGrid.GetComponent<ScrollView>());
        collectableGrid.gameObject.name = FishInventoryName;
        collectableGrid.RowSplit = 7;
        collectableGrid.ItemOffset = new Vector2(1.85f, -2f);
        collectableGrid.transform.SetLocalPosition2D(-3.14f, 2.3f);
        Destroy(collectableGrid.transform.Find("Consumables Divider").gameObject);
        Destroy(collectableGrid.transform.Find("Relics Divider").gameObject);
        itemManager.collectableGrid = collectableGrid;
        
        //Link grids
        logGrid.selectables[3].Selectables = [collectableGrid];
        logGrid.nextPages[3].Selectables = [collectableGrid];
        collectableGrid.selectables[2].Selectables = [logGrid];
        collectableGrid.nextPages[2].Selectables = [logGrid];
        itemManager.rightMostSelectables = [collectableGrid];
        
        //New description panel from tools pane
        GameObject toolDescriptionPanel = paneList.transform.Find("Tools/Tool Group/Tool Description Panel").gameObject;
        GameObject newDescriptionPanel = Instantiate(toolDescriptionPanel, pane.transform);
        newDescriptionPanel.transform.SetLocalPositionX(1.2f);
        newDescriptionPanel.GetComponentInChildren<RectTransform>().sizeDelta = new Vector2(6.5f, 8f);
        itemManager.descriptionLayout = newDescriptionPanel.GetComponentInChildren<VerticalLayoutGroup>();
        itemManager.nameText = newDescriptionPanel.transform.Find("Layout Stack/Text Name").GetComponent<TextMeshPro>();
        itemManager.descriptionText = newDescriptionPanel.transform.Find("Layout Stack/Text Desc").GetComponent<TextMeshPro>();
        itemManager.rarityText = newDescriptionPanel.transform.Find("Layout Stack/Icon Spacer/Text Amount").GetComponent<TextMeshPro>();
        itemManager.descriptionSpriteRenderer = newDescriptionPanel.transform.Find("Layout Stack/Icon Spacer/Tool Icon").GetComponent<SpriteRenderer>();
        itemManager.descriptionText.fontSize = 5f;
        itemManager.rarityText.GetComponent<TextContainer>().pivot = new Vector2(0.5f, 0.6f);
        itemManager.rarityText.fontStyle = FontStyles.Bold;
        
        //Clean up description panel
        Destroy(newDescriptionPanel.transform.Find("Divider").gameObject);
        Destroy(newDescriptionPanel.transform.Find("Button Prompt").gameObject);
        Destroy(newDescriptionPanel.transform.Find("Button Prompts").gameObject);
        Destroy(newDescriptionPanel.transform.Find("Currency Counters").gameObject);
        Destroy(newDescriptionPanel.transform.Find("Socket Unlock Desc Extra").gameObject);
        Destroy(newDescriptionPanel.transform.Find("Unlocker Item Break Flash").gameObject);
        Transform completionText = newDescriptionPanel.transform.Find("Text Completion").transform;
        completionText.transform.parent = logParent.transform;
        completionText.SetLocalPosition2D(-8f, -15.385f);
        itemManager.completionText = completionText.gameObject.GetComponent<TextMeshPro>();
        
        //Make entry template
        JournalEntryItem oldEntryItem = oldItemManager.templateItem;
        FishingLogEntryItem templateEntryItem = oldEntryItem.gameObject.AddComponent<FishingLogEntryItem>();
        itemManager.entryTemplate = templateEntryItem;
        templateEntryItem.FallbackSelectables = oldEntryItem.FallbackSelectables;
        templateEntryItem.nextPages = oldEntryItem.nextPages;
        templateEntryItem.newDot = oldEntryItem.newDot;
        templateEntryItem.fishIcon = oldEntryItem.iconSprite;
        templateEntryItem.emptyIcon = oldEntryItem.emptyIcon;
        templateEntryItem.frame = oldEntryItem.standardFrame.GetComponent<SpriteRenderer>();
        templateEntryItem.mask = templateEntryItem.transform.Find("Mask").gameObject;
        Destroy(oldEntryItem.completeFrame);
        Destroy(oldEntryItem);
        
        //Make collectable slot template
        InventoryItemCollectable oldCollectableItem = collectableGrid.transform.Find("Template Collectable Item").GetComponent<InventoryItemCollectable>();
        FishingLogCollectableItem templateCollectableItem = oldCollectableItem.gameObject.AddComponent<FishingLogCollectableItem>();
        itemManager.collectableTemplate = templateCollectableItem;
        templateCollectableItem.GetComponent<BoxCollider2D>().size = new Vector2(1.3f, 1.3f);
        templateCollectableItem.gameObject.SetActive(false);
        templateCollectableItem.FallbackSelectables = oldCollectableItem.FallbackSelectables;
        templateCollectableItem.nextPages = oldCollectableItem.nextPages;
        templateCollectableItem.lockIndicator = oldCollectableItem.newDot;
        SpriteRenderer lockSpriteRenderer = templateCollectableItem.lockIndicator.GetComponent<SpriteRenderer>();
        lockSpriteRenderer.sprite = ResourceUtil.LoadEmbeddedSprite("lock_indicator", 100f, ResourceUtil.AssetType.UI);
        lockSpriteRenderer.color = Color.white;
        Vector2 lockPosition = templateCollectableItem.lockIndicator.transform.localPosition;
        templateCollectableItem.lockIndicator.transform.SetLocalPosition2D(lockPosition.x - 0.06f, lockPosition.y - 0.06f);
        templateCollectableItem.lockIndicator.SetActive(false);
        templateCollectableItem.fishIcon = oldCollectableItem.spriteRenderer;
        templateCollectableItem.fishIcon.transform.SetScale2D(new Vector2(0.9f, 0.9f));
        templateCollectableItem.emptyIcon = Instantiate(templateEntryItem.emptyIcon, templateCollectableItem.transform);
        templateCollectableItem.weightText = oldCollectableItem.amountText;
        templateCollectableItem.weightText.fontSize = 3.5f;
        Destroy(oldCollectableItem);
        
        //Clean up new pane
        Destroy(oldItemManager);
        Destroy(pane.transform.Find("Text Desc").gameObject);
        Destroy(pane.transform.Find("Text Name").gameObject);
        Destroy(pane.transform.Find("Enemy Sprite").gameObject);
        Destroy(pane.transform.Find("Notes Desc").gameObject);
        Transform leftDivider = pane.transform.Find("divider").transform;
        leftDivider.SetLocalPositionX(-4.56f);
        Transform rightDivider = pane.transform.Find("divider (1)").transform;
        rightDivider.SetLocalPositionX(10.2f);
        Transform upArrow = pane.transform.Find("Arrow Up").transform;
        upArrow.SetLocalPositionX(-6.73f);
        Transform downArrow = pane.transform.Find("Arrow Down").transform;
        downArrow.SetLocalPositionX(-6.73f);
        Transform topGradient = pane.transform.Find("Gradient Top").transform;
        topGradient.SetScaleX(10f);
        topGradient.SetLocalPositionX(-7.2f);
        Transform bottomGradient = pane.transform.Find("Gradient Bottom").transform;
        bottomGradient.SetScaleX(10f);
        bottomGradient.SetLocalPositionX(-7.2f);
        Transform light = pane.transform.Find("Light").transform;
        light.SetLocalPosition2D(13.78f, -3.25f);
        light.SetScale2D(new Vector2(1.25f, 1f));
        light.GetComponent<NestedFadeGroupSpriteRenderer>()?.AlphaSelf = 0.5f;
        
        pane.gameObject.SetActive(false);
        pane.gameObject.name = "Fishing Log";
        pane.listIcon = ResourceUtil.LoadEmbeddedSprite("fishing_log_icon", 100f, ResourceUtil.AssetType.UI);
        
        //Add to array of panes
        Array.Resize(ref paneList.panes, paneList.panes.Length + 1);
        paneList.panes[^1] = paneList.panes[^2];
        paneList.panes[^2] = pane;
    }
}
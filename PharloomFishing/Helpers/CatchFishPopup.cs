using GlobalSettings;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace PharloomFishing;

public class CatchFishPopup : SavedItem, ICollectableUIMsgItem
{
    private Sprite _fishSprite;
    
    private readonly LocalisedString _popupFormat = new LocalisedString("UI", "POPUP_CATCH_FISH");
    private readonly LocalisedString _fullText = new LocalisedString("UI", "POPUP_INVENTORY_FULL");

    public void ShowPopup(Fish fish, float weight, bool newLargest, bool inventoryFull)
    {
        _fishSprite = fish.FishSprite;
        CollectableUIMsg popup = CollectableUIMsg.Spawn(this);
        
        popup.nameText.text = PopupText(fish, weight, newLargest, inventoryFull);
        LayoutElement popupLayout = popup.transform.Find("Text Container").GetComponent<LayoutElement>();
        popupLayout.minWidth = 7.5f;
        popupLayout.minHeight = 1.7f;
    }

    private string PopupText(Fish fish, float weight, bool newLargest, bool inventoryFull)
    {
        string nameHex = ColorUtility.ToHtmlStringRGB(fish.GetRarityColor());
        string fullText = inventoryFull ? _fullText : string.Empty;
        string fullHex = ColorUtility.ToHtmlStringRGB(UI.MaxItemsTextColor);
        string weightHex = newLargest ? fullHex : ColorUtility.ToHtmlStringRGB(Color.white);
        
        return string.Format(_popupFormat, fish.Name, fish.GetWeightText(weight), fullText, nameHex, weightHex, fullHex, "80%");
    }
    
    public override void Get(bool showPopup = true) { }
    public override bool CanGetMore() => true;
    public Object GetRepresentingObject() => this;
    public Sprite GetUIMsgSprite() => _fishSprite;
    public string GetUIMsgName() => string.Empty;
    public float GetUIMsgIconScale() => 1.1f;
}
using System;
using TeamCherry.Localization;
using UnityEngine;
using static UnityEngine.Object;

namespace PharloomFishing;

public static class FishingSpotHelper
{
    private static readonly Func<bool> MapCondition = () => SaveManager.CurrentSlotData.HasFishingSpotPins;

    private static readonly Sprite MapIcon = ResourceUtil.LoadEmbeddedSprite("pin_fish", 64f, ResourceUtil.AssetType.UI);
    
    public static void MakeFishingSpot(FishingSpot fishingSpot)
    {
        GameObject fishingSpotObject = new("FishingSpot");
        fishingSpotObject.transform.position = fishingSpot.WorldPos;
        fishingSpotObject.transform.SetPositionZ(0.006f);
        
        int direction = fishingSpot.FacingRight ? 1 : -1;
        
        //Prompt Marker
        GameObject promptMarkerObject = new("Prompt Marker");
        promptMarkerObject.transform.SetParent(fishingSpotObject.transform);
        promptMarkerObject.transform.localPosition = new Vector3(0f, 4.2f, 0f);
        
        //Collider
        BoxCollider2D collider = fishingSpotObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(3.5f, 0.7f);
        collider.offset = new Vector2(-1.2f * direction, 0f);
            
        //Interactable
        FishingSpotInteractable interactable = fishingSpotObject.AddComponent<FishingSpotInteractable>();
        interactable.promptMarker = promptMarkerObject.transform;
        interactable.talkPosition = fishingSpot.FacingRight ? NPCControlBase.TalkPositions.Left : NPCControlBase.TalkPositions.Right;
        interactable.targetDistance = 0;
        interactable.heroAnimation = HeroTalkAnimation.AnimationTypes.LookHalfDown;
        interactable.outsideRangeBehaviour = NPCControlBase.OutsideRangeBehaviours.ForceToDistance;
        interactable.priority = InteractPriority.LowPriority;
        if (!SaveManager.CurrentSlotData.HasFishingKit) interactable.Deactivate(false);
        
        //Graphics
        fishingSpotObject.AddComponent<DebugDrawColliderRuntime>().type = DebugDrawColliderRuntime.ColorType.None;
        Vector3 rayOrigin = fishingSpotObject.transform.position + new Vector3(2.5f * direction, 0f, 0f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, 5f, 1 << LayerMask.NameToLayer("Water Surface"));
        GameObject rippleObject = new("Ripple");
        float animationOffset = Mathf.Lerp(0f, 0.35f, Mathf.InverseLerp(1.6f, 3f, Vector2.Distance(rayOrigin, hit.point)));
        rippleObject.transform.SetParent(fishingSpotObject.transform);
        rippleObject.transform.position = new Vector3(hit.point.x, hit.point.y + fishingSpot.RippleOffset.y, 0.006f + fishingSpot.RippleOffset.z);
        rippleObject.AddComponent<SpriteRenderer>();
        
        interactable.Initialize(fishingSpot, hit.collider.gameObject.GetComponentInParent<SurfaceWaterRegion>(), rippleObject.transform, animationOffset);
    }
    
    public static void MakeFishingSpotPin(GameMap gameMap, FishingSpot fishingSpot)
    {
        GameObject pinObject = new("Fishing Spot Pin", typeof(SpriteRenderer), typeof(MapPin), typeof(DeactivateIfModDataFalse));
        DontDestroyOnLoad(pinObject);
        pinObject.layer = LayerMask.NameToLayer("UI");

        pinObject.GetComponent<SpriteRenderer>().sprite = MapIcon;
        pinObject.GetComponent<MapPin>().parentScene = gameMap.transform.Find(fishingSpot.MapRoom).gameObject.GetComponent<GameMapScene>();
        pinObject.transform.SetParent(pinObject.GetComponent<MapPin>().parentScene.transform);
            
        pinObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        pinObject.transform.localPosition = fishingSpot.MapPos;
        pinObject.transform.SetLocalPositionZ(-2.51f);
        
        pinObject.GetComponent<DeactivateIfModDataFalse>().SetCondition(MapCondition);
    }

    public static void MakeFishingSpotKey(InventoryMapManager mapManager)
    {
        GameObject keyEntryTemplate = mapManager.transform.Find("Map Key/Keys/Key Bell Beast").gameObject;
        GameObject keyEntryObject = Instantiate(keyEntryTemplate, keyEntryTemplate.transform.parent);
        keyEntryObject.name = "Fishing Spot Key Entry";
        
        keyEntryObject.GetComponentInChildren<SpriteRenderer>().sprite = MapIcon;
        Destroy(keyEntryObject.GetComponentInChildren<DeactivateIfPlayerdataFalse>());
        keyEntryObject.GetComponentInChildren<SetTextMeshProGameText>().Text = new LocalisedString("UI", "KEY_FISHING_SPOT");
        
        keyEntryObject.AddComponent<DeactivateIfModDataFalse>().SetCondition(MapCondition);
    }

    public static void AdjustMap(GameMap gameMap)
    {
        gameMap.transform.Find("Crawl/Area Name")?.SetLocalPositionX(8.31f);
    }
}
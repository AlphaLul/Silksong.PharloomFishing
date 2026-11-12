using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MonoMod.Utils;
using Newtonsoft.Json;
using TeamCherry.Localization;
using UnityEngine;

namespace PharloomFishing;

[HarmonyPatch]
internal class Patches
{
    //Load modded localizations
    [HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
    [HarmonyPostfix]
    public static void Language_DoSwitchPostfix()
    {
        string languageCode = Language.CurrentLanguage().ToString();
        string? jsonText = ResourceUtil.LoadEmbeddedTextOrNull($"{ResourceUtil.LanguagesPath}{languageCode}.json") ??
                           ResourceUtil.LoadEmbeddedTextOrNull($"{ResourceUtil.LanguagesPath}EN.json");
        if (jsonText == null)
        {
            PharloomFishing.LogError($"Unable to load {languageCode} localization");
            return;
        }
        
        Dictionary<string, Dictionary<string, string>>? moddedSheets = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonText);
        if (moddedSheets == null) return;
        
        foreach ((string key, Dictionary<string, string> value) in moddedSheets)
        {
            if (Language._currentEntrySheets.ContainsKey(key))
            {
                Language._currentEntrySheets[key].AddRange(moddedSheets[key]);
            }
            else
            {
                Language._currentEntrySheets[key] = value;
            }
        }
    }
    
    //Spawn fishing spot map pins and key
    [HarmonyPatch(typeof(GameMap), nameof(GameMap.SetMapManager))]
    [HarmonyPostfix]
    private static void GameMap_SetMapManagerPostfix(GameMap __instance)
    {
        FishingSpotHelper.MakeFishingSpotKey(__instance.mapManager);

        foreach (FishingSpot data in DataManager.FishingSpots)
        {
            FishingSpotHelper.MakeFishingSpotPin(__instance, data);
        }

        FishingSpotHelper.AdjustMap(__instance);
    }
    
    //Spawn fishing spot and craw npc
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnNextLevelReady))]
    [HarmonyPostfix]
    private static void GameManager_OnNextLevelReadyPostfix(GameManager __instance)
    {
        if (SaveManager.FailedToLoad) return;
        string sceneName = __instance.sceneName;
        if (sceneName == "Greymoor_21")
            CrawNPCHelper.MakeCrawNpc();
        foreach (FishingSpot data in DataManager.FishingSpots.Where(data => data.WorldRoom == sceneName))
        {
            FishingSpotHelper.MakeFishingSpot(data);
        }
    }
    
    //Add fishing kit to inventory
    [HarmonyPatch(typeof(CollectableItemManager), nameof(CollectableItemManager.GetCollectedItems))]
    [HarmonyPostfix]
    private static void CollectableItemManager_GetCollectedItemsPostfix(List<CollectableItem> __result)
    {
        if (SaveManager.CurrentSlotData.HasFishingKit && !__result.Contains(DataManager.FishingKitItem))
            __result.Add(DataManager.FishingKitItem);
    }
    
    //Prevent saving fishing kit data to vanilla save file
    [HarmonyPatch(typeof(CollectableItem), nameof(CollectableItem.SaveData), MethodType.Setter)]
    [HarmonyPrefix]
    private static bool CollectableItem_SetSaveDataPrefix(CollectableItem __instance)
    {
        return __instance is not FishingKitCollectableItem;
    }
    
    //Load whether fishing kit has been seen from mod save
    [HarmonyPatch(typeof(CollectableItem), nameof(CollectableItem.IsSeen), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool CollectableItem_GetIsSeenPrefix(CollectableItem __instance, ref bool __result)
    {
        if (__instance is not FishingKitCollectableItem) return true;
        __result = SaveManager.CurrentSlotData.HasSeenFishingKit;
        return false;
    }
    
    //Save whether fishing kit has been seen to mod save
    [HarmonyPatch(typeof(CollectableItem), nameof(CollectableItem.IsSeen), MethodType.Setter)]
    [HarmonyPrefix]
    private static bool CollectableItem_SetIsSeenPrefix(CollectableItem __instance, bool value)
    {
        if (__instance is not FishingKitCollectableItem) return true;
        SaveManager.CurrentSlotData.HasSeenFishingKit = value;
        return false;
    }
    
    //Override collection behavior for fishing kit item
    [HarmonyPatch(typeof(CollectableItemBasic), nameof(CollectableItem.OnCollected))]
    [HarmonyPrefix]
    private static bool CollectableItemBasic_OnCollectedPrefix(CollectableItemBasic __instance)
    {
        if (__instance is not FishingKitCollectableItem) return true;
        SaveManager.CurrentSlotData.HasFishingKit = true;
        SaveManager.CurrentSlotData.HasFishingSpotPins = true;
        return false;
    }

    //Create fishing log inventory pane
    [HarmonyPatch(typeof(GameCameras), nameof(GameCameras.Awake))]
    [HarmonyPostfix]
    private static void GameCameras_AwakePostfix(GameCameras __instance)
    {
        InventoryPaneList paneList =
            __instance.transform.Find("HudCamera/In-game/Inventory").GetComponent<InventoryPaneList>();
        InventoryHelper.MakeFishingLogPane(paneList);
    }
    
    //Skip enforcing inventory pane list size, allowing for a modded pane
    [HarmonyPatch(typeof(InventoryPaneList), nameof(InventoryPaneList.OnValidate))]
    [HarmonyPrefix]
    private static bool InventoryPaneList_OnValidatePrefix()
    {
        return false;
    }
    
    //Offset every other grid row in the fishing log inventory
    [HarmonyPatch(typeof(InventoryItemGrid), nameof(InventoryItemGrid.PositionGridItems), typeof(List<InventoryItemSelectableDirectional>), typeof(float))]
    [HarmonyPrefix]
    private static bool InventoryItemGrid_PositionGridItemsPrefix(InventoryItemGrid __instance, 
        List<InventoryItemSelectableDirectional> childItems, float yOffset)
    {
        if (__instance.gameObject.name != InventoryHelper.FishInventoryName) return true;
        int rowSplit = __instance.RowSplit;
        Vector2 itemOffset = __instance.ItemOffset;
        for (int i = 0; i < childItems.Count; i++)
        {
            int row = i / rowSplit;
            int column = i % rowSplit;
            float xOffset = row % 2 == 0 ? itemOffset.x/2f : 0f;
            childItems[i].transform.SetLocalPosition2D(__instance.GridOffset + new Vector2(itemOffset.x * column + xOffset, itemOffset.y * row + yOffset));
        }
        return false;
    }
    
    //Make shortcut still lead to map
    [HarmonyPatch(typeof(InventoryPaneList), nameof(InventoryPaneList.GetPane), typeof(InventoryPaneList.PaneTypes))]
    [HarmonyPrefix]
    private static bool InventoryPaneList_GetPanePrefix(InventoryPaneList __instance, ref InventoryPane __result, InventoryPaneList.PaneTypes paneTypes)
    {
        if (paneTypes != InventoryPaneList.PaneTypes.Map) return true;
        __result = __instance.GetPane(5);
        return false;
    }
    
    [HarmonyPatch(typeof(PlayMakerFSM), nameof(PlayMakerFSM.Awake))]
    [HarmonyPostfix]
    private static void PlayMakerFSM_AwakePostfix(PlayMakerFSM __instance)
    {
        GameObject? owner = __instance.gameObject;
        if (owner == null) return;
        
        if (!owner.name.StartsWith("Inventory", StringComparison.OrdinalIgnoreCase)) return;
        if (!string.Equals(__instance.FsmName, "Inventory Control", StringComparison.OrdinalIgnoreCase)) return;

        FsmState? state = __instance.fsm.GetState("Map");
        if (state == null) return;
        SetCurrentInventoryPane? action = state.Actions.OfType<SetCurrentInventoryPane>().FirstOrDefault();
        action?.PaneIndex = 5;
    }
    
    [HarmonyPatch(typeof(PlayMakerFSM), nameof(PlayMakerFSM.SendEvent))]
    [HarmonyPrefix]
    private static void PlayMakerFSM_SendEventPrefix(PlayMakerFSM __instance, string eventName)
    {
        GameObject? owner = __instance.gameObject;
        if (owner == null) return;
        
        if (!owner.name.StartsWith("Inventory", StringComparison.OrdinalIgnoreCase)) return;
        if (!string.Equals(__instance.FsmName, "Inventory Control", StringComparison.OrdinalIgnoreCase)) return;
        if (!string.Equals(eventName, "MOVE PANE TO", StringComparison.OrdinalIgnoreCase)) return;
        
        FsmInt targetPaneIndex = __instance.FsmVariables.FindFsmInt("Target Pane Index");
        if (targetPaneIndex == null) return;
        if(targetPaneIndex.value == 4) targetPaneIndex.value = 5;
    }

    //Saving patches
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartNewGame))]
    [HarmonyPostfix]
    private static void GameManager_StartNewGamePostfix(GameManager __instance)
    {
        SaveManager.DeleteSaveSlotData(__instance.profileID);
        SaveManager.CreateSaveSlotData(__instance.profileID);
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SetLoadedGameData), typeof(SaveGameData), typeof(int))]
    [HarmonyPostfix]
    private static void GameManager_SetLoadedGameDataPostfix(int saveSlot)
    {
        SaveManager.LoadSaveSlotData(saveSlot);
    }
    
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveGame), typeof(int), typeof(System.Action<bool>), typeof(bool), typeof(AutoSaveName))]
    [HarmonyPostfix]
    private static void GameManager_SaveGamePostfix(int saveSlot)
    {
        SaveManager.WriteSaveSlotData(saveSlot);
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ClearSaveFile))]
    [HarmonyPostfix]
    private static void GameManager_ClearSaveFilePostfix(int saveSlot)
    {
        SaveManager.DeleteSaveSlotData(saveSlot);
    }
}
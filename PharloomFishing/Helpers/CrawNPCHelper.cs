using UnityEngine;

namespace PharloomFishing;

public static class CrawNPCHelper
{
    public static void MakeCrawNpc()
    {
        //Scene modification
        GameObject backgroundRoot = GameObject.Find("gre_mid_wall_bound (9)");
        backgroundRoot?.transform.Find("_0016_grey_").gameObject.SetActive(false);
        backgroundRoot?.transform.Find("_0017_grey_").gameObject.SetActive(false);
        GameObject lump1 = GameObject.Find("_0039_grey_lumpy (57)");
        GameObject lump2 = GameObject.Find("_0039_grey_lumpy (58)");
        lump1?.transform.SetLocalPositionX(74f);
        lump2?.transform.SetLocalPositionX(77.2f);

        GameObject thread = GameObject.Find("thread_memory");
        GameObject corpse = GameObject.Find("corpses_pilgrim_spinner_0009_1");
        GameObject collectable = GameObject.Find("Collectable Item Pickup");
        bool isRelicCollected = true;
        if (PlayerData.instance.Relics.GetData("Bone Record Greymoor_flooded_corridor").IsCollected)
        {
            thread?.SetActive(false);
            corpse?.SetActive(false);
        }
        else
        {
            thread?.transform.Translate(-1.5f, 0f, 0f);
            corpse?.transform.Translate(-1.5f, -0.1f, 0f);
            collectable?.transform.Translate(-1.5f, 0f, 0f);
            isRelicCollected = false;
        }
        GameObject.Find("greymoor_break_rags")?.transform.Find("long")?.gameObject.SetActive(false);
        
        //Spawn craw
        GameObject crawObject = new("Hungry Craw NPC");
        crawObject.transform.SetPosition2D(71f, 8f);
        
        GameObject promptMarkerObject = new("Prompt Marker");
        promptMarkerObject.transform.SetParent(crawObject.transform);
        promptMarkerObject.transform.localPosition = new Vector3(0.3f, 4.65f, 0f);
        
        BoxCollider2D collider = crawObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(6f, 0.5f);
        
        CrawNPC npc = crawObject.AddComponent<CrawNPC>();
        npc.promptMarker = promptMarkerObject.transform;
        npc.talkPosition = NPCControlBase.TalkPositions.Left;
        npc.targetDistance = 2.3f;
        npc.heroAnimation = HeroTalkAnimation.AnimationTypes.LookHalfUp;
        npc.outsideRangeBehaviour = NPCControlBase.OutsideRangeBehaviours.ForceToDistance;
        
        GameObject rendererObject = new("Craw Renderer");
        rendererObject.transform.SetParent(crawObject.transform);
        rendererObject.transform.localPosition = new Vector3(1.8f, 1.65f, 1f);
        SpriteRenderer crawRenderer = rendererObject.AddComponent<SpriteRenderer>();
        crawRenderer.flipX = true;
        
        GameObject roarObject = new("Roar");
        roarObject.transform.SetParent(crawObject.transform);
        roarObject.transform.localPosition = new Vector3(1.5f, 2.5f, 0f);

        GameObject peckObject = new("Peck");
        peckObject.transform.SetParent(crawObject.transform);
        peckObject.transform.localPosition = new Vector3(-1f, 2.1f, 0f);
        SpriteRenderer peckRenderer = peckObject.AddComponent<SpriteRenderer>();
        peckRenderer.flipX = true;
        
        NPCSpeakingAudio crawSpeakingAudio = crawObject.AddComponent<NPCSpeakingAudio>();
        crawSpeakingAudio.Table = ScriptableObject.CreateInstance<RandomAudioClipTable>();
        crawSpeakingAudio.Table.clips =
        [
            new RandomAudioClipTable.ProbabilityAudioClip()
            {
                Clip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_talk_1", ResourceUtil.AssetType.Craw),
                Probability = 1
            },
            new RandomAudioClipTable.ProbabilityAudioClip()
            {
                Clip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_talk_2", ResourceUtil.AssetType.Craw),
                Probability = 1
            },
            new RandomAudioClipTable.ProbabilityAudioClip()
            {
                Clip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_talk_3", ResourceUtil.AssetType.Craw),
                Probability = 1
            },
            new RandomAudioClipTable.ProbabilityAudioClip()
            {
                Clip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_talk_4", ResourceUtil.AssetType.Craw),
                Probability = 1
            },
            new RandomAudioClipTable.ProbabilityAudioClip()
            {
                Clip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_talk_5", ResourceUtil.AssetType.Craw),
                Probability = 1
            },
            new RandomAudioClipTable.ProbabilityAudioClip()
            {
                Clip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_talk_6", ResourceUtil.AssetType.Craw),
                Probability = 1
            }
        ];
        crawSpeakingAudio.npc = npc;
        
        crawObject.AddComponent<DebugDrawColliderRuntime>().type = DebugDrawColliderRuntime.ColorType.None;
        
        if (!isRelicCollected)
        {
            collider.enabled = false;
            CollectableItemPickup? collectablePickup = collectable?.GetComponent<CollectableItemPickup>();
            collectablePickup?.OnPickupEnd.AddListener(npc.DoRoar);
        }
        
        npc.Initialize(isRelicCollected, collider, crawRenderer, peckRenderer, roarObject.transform);
    }
}
using System;
using System.Collections;
using GlobalSettings;
using HutongGames.PlayMaker;
using TeamCherry.Localization;
using UnityEngine;

namespace PharloomFishing;

public class CrawNPC : NPCControlBase
{
    private bool _isRelicCollected, _wasFedThisVisit;
    private BoxCollider2D _collider;
    
    private SpriteRenderer _crawRenderer, _peckRenderer;
    private readonly Sprite[] _idleAnimation = ResourceUtil.LoadEmbeddedAnimation("Idle", 5, 64, ResourceUtil.AssetType.Craw);
    private readonly Sprite[] _eatAnimation = ResourceUtil.LoadEmbeddedAnimation("Eat", 6, 64, ResourceUtil.AssetType.Craw);
    private readonly Sprite[] _enterAnimation = ResourceUtil.LoadEmbeddedAnimation("Enter", 2, 64, ResourceUtil.AssetType.Craw);
    private readonly Sprite[] _giveKitAnimation = ResourceUtil.LoadEmbeddedAnimation("GiveKit", 3, 64, ResourceUtil.AssetType.Craw);
    private readonly Sprite[] _peckAnimation = ResourceUtil.LoadEmbeddedAnimation("Peck", 4, 110, ResourceUtil.AssetType.Craw);
    private Coroutine _idleCoroutine;
    
    private Transform _roarTransform;
    private HeroController _hc;

    private readonly AudioEvent _crawAudioEvent = new AudioEvent
    {
        Volume = 1.5f
    };
    private readonly AudioClip _roarClip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_roar", ResourceUtil.AssetType.Craw);
    private readonly AudioClip _giveKitClip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_give_kit", ResourceUtil.AssetType.Craw);
    private readonly AudioClip _eatSmallClip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_eat_small", ResourceUtil.AssetType.Craw);
    private readonly AudioClip _eatBigClip = ResourceUtil.LoadEmbeddedAudioClip("hungry_craw_eat_big", ResourceUtil.AssetType.Craw);
    private readonly AudioClip _placeFishClip = ResourceUtil.LoadEmbeddedAudioClip("place_fish");
    
    private readonly LocalisedString _feedPrompt = new("Hungry Craw", "HUNGRY_CRAW_FEED");
    
    public override bool AutoEnd => false;
    public override bool AutoCallEndAction => false;
    public override bool AllowMovePlayer => true;
    
    public void Initialize(bool isRelicCollected, BoxCollider2D collider, SpriteRenderer crawRenderer, SpriteRenderer peckRenderer, Transform roarTransform)
    {
        _isRelicCollected = isRelicCollected;
        _collider = collider;
        _crawRenderer = crawRenderer;
        _peckRenderer = peckRenderer;
        _roarTransform = roarTransform;
    }

    public override void Start()
    {
        base.Start();
        _hc = HeroController.instance;
        _crawRenderer.enabled = _isRelicCollected;
        _peckRenderer.enabled = false;
        
        if (_isRelicCollected)
            _idleCoroutine = StartCoroutine(PlayIdleAnimation());
        if (SaveManager.CurrentSlotData.CrawState == CrawState.GaveFishingKit && !SaveManager.CurrentSlotData.HasFishingKit)
        {
            CollectableItemPickup pickup = Instantiate(Gameplay.CollectableItemPickupPrefab, _roarTransform.position, Quaternion.identity);
            pickup.SetItem(DataManager.FishingKitItem);
            pickup.flingDirection = CollectableItemPickup.FlingDirection.Left;
            pickup.fling = true;
        }
    }

    public override void OnStartDialogue()
    {
        DisableInteraction();
        DialogueBox._instance.hudFSM.SendEventSafe("OUT");
        ShowTitle();
        DoConversation();
    }

    private void EndInteraction()
    {
        DialogueBox.EndConversation();
        HideTitle();
        CallEndAction();
        EnableInteraction();
    }

    private void DoConversation()
    {
        Action end = EndInteraction;
        string currentConversationKey;
        switch (SaveManager.CurrentSlotData.CrawState)
        {
            case CrawState.NotMet:
                currentConversationKey = _isRelicCollected ? "HUNGRY_CRAW_MEET" : "HUNGRY_CRAW_MEET_CORPSE";
                end = GiveFishingKit;
                break;
            case CrawState.GaveFishingKit:
                if (!SaveManager.CurrentSlotData.HasFishingKit)
                {
                    currentConversationKey = "HUNGRY_CRAW_TAKE_KIT";
                    break;
                }
                if (SaveManager.CurrentSlotData.NumUnlockedFish() > 0)
                {
                    currentConversationKey = _wasFedThisVisit ? "HUNGRY_CRAW_WAS_FED_HAS_MORE" : "HUNGRY_CRAW_ASK_FEED";
                    end = ShowYesNoBox;
                    break;
                }
                currentConversationKey = _wasFedThisVisit ? "HUNGRY_CRAW_WAS_FED" : "HUNGRY_CRAW_REPEAT";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        DialogueBox.StartConversation(new LocalisedString("Hungry Craw", currentConversationKey), this, false, DialogueBox.DisplayOptions.Default, end);
    }

    private void ShowYesNoBox()
    {
        Action yes = () => StartCoroutine(YesSelectedRoutine());
        Action no = EndInteraction;
        
        DialogueYesNoBox.Open(yes, no, false, string.Format(_feedPrompt, SaveManager.CurrentSlotData.NumUnlockedFish()));
    }

    private IEnumerator YesSelectedRoutine()
    {
        yield return new WaitForSeconds(0.25f);
        yield return FeedRoutine();
        yield return new WaitForSeconds(1f);
        int value = SaveManager.CurrentSlotData.SellFish();
        _hc.AddGeo(value);
        _wasFedThisVisit = true;
        EndInteraction();
    }
    
    private void ShowTitle()
    {
        GameObject titleObject = ManagerSingleton<AreaTitle>.Instance.gameObject;
        PlayMakerFSM fsm = ActionHelpers.GetGameObjectFsm(titleObject, "Area Title Control");
        titleObject.SetActive(false);
        fsm.FsmVariables.FindFsmBool("Visited").Value = true;
        fsm.FsmVariables.FindFsmBool("NPC Title").Value = true;
        fsm.FsmVariables.FindFsmBool("Display Right").Value = true;
        fsm.FsmVariables.FindFsmString("Area Event").Value = "HUNGRY_CRAW";
        titleObject.SetActive(true);
    }

    private void HideTitle()
    {
        GameObject titleObject = ManagerSingleton<AreaTitle>.Instance.gameObject;
        ActionHelpers.GetGameObjectFsm(titleObject, "Area Title Control").SendEventSafe("NPC TITLE DOWN");
    }

    private IEnumerator FeedRoutine()
    {
        //Hornet animation
        tk2dSpriteAnimator animator = _hc.GetComponent<tk2dSpriteAnimator>();
        HeroAnimationController heroAnim = _hc.GetComponent<HeroAnimationController>();
        animator.Play(heroAnim.GetClip("Collect Normal 1"));
        yield return new WaitForSeconds(0.25f);
        _crawAudioEvent.SetAndSpawnOneShot(_placeFishClip, transform.position);
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(animator.PlayAnimWait(heroAnim.GetClip("Collect Normal 3")));
        HeroTalkAnimation.PlayLookAnimation(HeroTalkAnimation.AnimationTypes.LookHalfUp, false);
        
        //Craw animation
        yield return PlayEatAnimation();
    }
    
    public void DoRoar() => StartCoroutine(RoarRoutine());
    private IEnumerator RoarRoutine()
    {
        GameObject roarEmitter = GameManager.instance.gameCams.transform.Find("Roar Wave Emitter").gameObject;
        roarEmitter.transform.position = _roarTransform.position;
        
        PlayMakerFSM.FindFsmOnGameObject(_hc.gameObject, "Roar and Wound States")
            .FsmVariables.FindFsmFloat("Push Strength").Value = 0f;
        
        FSMUtility.SendEventToGameObject(roarEmitter, "START");
        FSMUtility.SendEventToGameObject(_hc.gameObject, "ROAR ENTER FORCED");
        _crawAudioEvent.SetAndSpawnOneShot(_roarClip, transform.position);

        yield return new WaitForSeconds(2f);
        
        FSMUtility.SendEventToGameObject(roarEmitter, "END");
        
        yield return new WaitForSeconds(1f);
        yield return PlayEnterAnimation();
        yield return new WaitForSeconds(0.35f);
        
        FSMUtility.SendEventToGameObject(_hc.gameObject, "ROAR EXIT");
        _collider.enabled = true;
    }
    
    public void GiveFishingKit() => StartCoroutine(GiveFishingKitRoutine());
    private IEnumerator GiveFishingKitRoutine()
    {
        DialogueBox.EndConversation(false);
        _crawAudioEvent.SetAndSpawnOneShot(_giveKitClip, transform.position);
        yield return PlayGiveKitAnimation();
        _collider.enabled = false;
        EndInteraction();
        
        CollectableItemPickup pickup = Instantiate(Gameplay.CollectableItemPickupPrefab, _roarTransform.position, Quaternion.identity);
        pickup.SetItem(DataManager.FishingKitItem);
        pickup.flingDirection = CollectableItemPickup.FlingDirection.Left;
        pickup.fling = true;
        SaveManager.CurrentSlotData.CrawState = CrawState.GaveFishingKit;
        
        yield return new WaitForSeconds(2f);
        _collider.enabled = true;
    }
    
    private IEnumerator PlayIdleAnimation()
    {
        int i = 0;
        while (true)
        {
            _crawRenderer.sprite = _idleAnimation[i];
            i = (i + 1) % _idleAnimation.Length;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator PlayEatAnimation()
    {
        bool bigEat = SaveManager.CurrentSlotData.NumUnlockedFish() >= 15;
        if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
        for (int i = 0; i < _eatAnimation.Length; i++)
        {
            _crawRenderer.sprite = _eatAnimation[i];
            if (i >= 4) yield return new WaitForSeconds(0.085f);
            else yield return new WaitForSeconds(0.05f);

            if (i != 0) continue;
            _crawAudioEvent.SetAndSpawnOneShot(bigEat ? _eatBigClip : _eatSmallClip, transform.position);
            StartCoroutine(PlayPeckAnimation());
        }
        _idleCoroutine = StartCoroutine(PlayIdleAnimation());
        yield return new WaitForSeconds(bigEat? 0.9f : 0.73f);
        yield return PlayEnterAnimation(!bigEat);
    }

    private IEnumerator PlayEnterAnimation(bool skipFirstFrame = false)
    {
        if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
        _crawRenderer.enabled = true;
        foreach (Sprite frame in _enterAnimation)
        {
            if (skipFirstFrame)
            {
                skipFirstFrame = false;
                continue;
            }
            _crawRenderer.sprite = frame;
            yield return new WaitForSeconds(0.1f);
        }
        _idleCoroutine = StartCoroutine(PlayIdleAnimation());
    }
    
    private IEnumerator PlayGiveKitAnimation()
    {
        if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
        _crawRenderer.enabled = true;
        foreach (Sprite frame in _giveKitAnimation)
        {
            _crawRenderer.sprite = frame;
            yield return new WaitForSeconds(0.1f);
        }
        _idleCoroutine = StartCoroutine(PlayIdleAnimation());
    }

    private IEnumerator PlayPeckAnimation()
    {
        _peckRenderer.enabled = true;
        foreach (Sprite frame in _peckAnimation)
        {
            _peckRenderer.sprite = frame;
            yield return new WaitForSeconds(0.04f);
        }
        _peckRenderer.enabled = false;
    }
}
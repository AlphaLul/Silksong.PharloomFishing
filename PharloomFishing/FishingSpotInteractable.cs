using System.Collections;
using System.Collections.Generic;
using GlobalEnums;
using TeamCherry.Localization;
using TeamCherry.SharedUtils;
using UnityEngine;

namespace PharloomFishing;

public class FishingSpotInteractable : NPCControlBase
{
    private InputHandler _inputHandler;
    private HeroController _hc;
    private CameraController _cc;
    private AudioClip _readyVoiceClip, _catchVoiceClip;

    private readonly AudioEvent _fishAudioEvent = AudioEvent.Default;
    private readonly AudioClip[] _catchImpactClips = [
        ResourceUtil.LoadEmbeddedAudioClip("catch_impact_1"), 
        ResourceUtil.LoadEmbeddedAudioClip("catch_impact_2")
    ];
    private readonly AudioClip _quickCatchClip = ResourceUtil.LoadEmbeddedAudioClip("quick_catch");
    private readonly AudioClip _defaultFishCatchClip = ResourceUtil.LoadEmbeddedAudioClip("default", ResourceUtil.AssetType.Fishes);

    private FishingSpot _fishingSpot;
    private readonly List<Fish> _availableFishes = [];
    private SurfaceWaterRegion _waterRegion;
    
    private Transform _rippleTransform;
    private readonly Sprite[] _rippleAnimation = ResourceUtil.LoadEmbeddedAnimation("Ripple", 6, 64f);
    private float _animationOffset;
    
    private bool _waitingForInput, _fishing, _reactionWindowOpen, _isQuickCatch;
    private float _quickCatchLeeway;
    private Coroutine? _minigameRoutine;
    private CatchFishPopup _catchFishPopup;

    private readonly MinMaxFloat _rippleWindow = new(5f, 7f);

    private readonly ControlReminder.SingleConfig _readyPrompt = new()
    {
        DisappearEvent = "",
        PlayerDataBool = "",
        FadeInDelay = 0f,
        FadeInTime = 1f,
        FadeOutTime = 0.5f,
        Text = new LocalisedString("UI", "CTRL_READY"),
        Prompt = new LocalisedString("", ""),
        Button = HeroActionButton.MENU_SUPER,
        DisappearOnButtonPress = false
    };
    private readonly ControlReminder.SingleConfig _leavePrompt = new()
    {
        DisappearEvent = "",
        PlayerDataBool = "",
        FadeInDelay = 0f,
        FadeInTime = 1f,
        FadeOutTime = 0.5f,
        Text = new LocalisedString("UI", "CTRL_LEAVE"),
        Prompt = new LocalisedString("", ""),
        Button = HeroActionButton.MENU_CANCEL,
        DisappearOnButtonPress = false
    };
    
    private readonly ControlReminder.SingleConfig _catchPrompt = new()
    {
        DisappearEvent = "",
        PlayerDataBool = "",
        FadeInDelay = 0f,
        FadeInTime = 1f,
        FadeOutTime = 0.5f,
        Text = new LocalisedString("UI", "CTRL_CATCH"),
        Prompt = new LocalisedString("", ""),
        Button = HeroActionButton.MENU_SUPER,
        DisappearOnButtonPress = false
    };
    
    public override bool AutoEnd => false;
    public override bool AutoCallEndAction => false;
    public override bool AllowMovePlayer => true;

    public override string InteractLabelDisplay => "FISH";

    public void Initialize(FishingSpot fishingSpot, SurfaceWaterRegion waterRegion, Transform rippleTransform, float animationOffset)
    {
        _fishingSpot = fishingSpot;
        _waterRegion = waterRegion;
        _rippleTransform = rippleTransform;
        _animationOffset = animationOffset;

        foreach (Fish fish in fishingSpot.Fishes)
        {
            if (fish.Condition == null)
            {
                _availableFishes.Add(fish);
                continue;
            }
            if (PlayerData._instance.GetBool(fish.Condition))
                _availableFishes.Add(fish);
        }
    }
    
    public override void Start()
    {
        base.Start();
        
        _catchFishPopup = DataManager.CatchFishPopup;
        _hc = HeroController.instance;
        _cc = GameManager.instance.cameraCtrl;
        _readyVoiceClip = _hc.configs[4].NormalSlash.GetComponent<AudioSource>().clip;
        _catchVoiceClip = _hc.configs[4].DownSlash.GetComponent<AudioSource>().clip;
        _inputHandler = GameManager.instance.inputHandler;
        
        StartCoroutine(PlayRippleAnimation(_rippleTransform.GetComponent<SpriteRenderer>()));
        StartCoroutine(RippleSplashRoutine());
        _hc.OnTakenDamage += OnTakenDamage;
    }

    public void OnDestroy()
    {
        _hc.OnTakenDamage -= OnTakenDamage;
    }

    public override void OnStartDialogue()
    {
        DisableInteraction();
        DialogueBox._instance.hudFSM.SendEventSafe("OUT");
        ShowInitialPrompts();
    }

    public override void OnEndDialogue()
    {
        _waitingForInput = false;
        _fishing = false;
        DialogueBox._instance.hudFSM.SendEventSafe("IN");
        if (_minigameRoutine != null)
        {
            StopCoroutine(_minigameRoutine);
            _minigameRoutine = null;
        }
        
        _cc.StopFreeze();
        EnableInteraction();
        _hc.StartAnimationControlToIdle();
    }

    private void ShowInitialPrompts()
    {
        ControlReminder.PushSingle(_readyPrompt);
        ControlReminder.PushSingle(_leavePrompt);
        ControlReminder.ShowPushed();
        _waitingForInput = true;
    }

    private void ShowReelPrompt()
    {
        ControlReminder.PushSingle(_catchPrompt);
        ControlReminder.ShowPushed();
        _waitingForInput = true;
        _fishing = true;
    }

    private void Update()
    {
        if (!_waitingForInput) return;

        HeroActions inputActions = _inputHandler.inputActions;
        switch (Platform.Current.GetMenuAction(inputActions, true))
        {
            case Platform.MenuActions.Super:
                SuperSelected();
                return;
            case Platform.MenuActions.Cancel:
                CancelSelected();
                return;
            default:
                return;
        }
    }

    private void SuperSelected()
    {
        PromptSelected();
        if (!_fishing)
            _minigameRoutine = StartCoroutine(MinigameRoutine());
        else
            _fishing = false;
    }

    private void CancelSelected()
    {
        if (_fishing) return;
        PromptSelected();
        Invoke(nameof(EndDialogue), 0f); //Invoke on next frame to avoid pausing if player pressed escape to end
    }

    private IEnumerator RippleSplashRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_rippleWindow.GetRandomValue());
            _waterRegion.DoSplashInSmall(_rippleTransform, new Vector3(0, 0.3f, 0));

            if (!_fishing) continue;
            
            _reactionWindowOpen = true;
            _isQuickCatch = true;
            float quickCatchWindow = Mathf.Min(DataManager.BaseQuickCatchWindow + _quickCatchLeeway, DataManager.CatchWindow);
            
            yield return new WaitForSeconds(quickCatchWindow);
            _isQuickCatch = false;
            _quickCatchLeeway += 0.0075f;
            
            yield return new WaitForSeconds(DataManager.CatchWindow - DataManager.BaseQuickCatchWindow);
            _reactionWindowOpen = false;
        }
    }

    private IEnumerator MinigameRoutine()
    {
        _hc.gruntAudioTable.PlayOneShot(_hc.audioSource);
        _readyVoiceClip.PlayOnSource(_hc.audioSource);
        _hc.animCtrl.Play("Taunt");
        yield return new WaitForSeconds(_hc.animCtrl.GetCurrentClipDuration() - 0.1f);
        _hc.animCtrl.Play("Taunt Idle");
        
        ShowReelPrompt();
        _waitingForInput = true;
        _fishing = true;

        yield return new WaitUntil(() => !_fishing);

        if (_reactionWindowOpen)
        {
            (Fish fishCaught, float weight, bool newHeaviest, bool inventoryFull) = _fishingSpot.CatchFish(_availableFishes, _isQuickCatch);
            if (_isQuickCatch)
            {
                _fishAudioEvent.SetAndSpawnOneShot(_quickCatchClip, _rippleTransform.position);
                _quickCatchLeeway = 0f;
            }
            _fishAudioEvent.SetAndSpawnOneShot(_catchImpactClips[Random.Range(0, _catchImpactClips.Length)], _rippleTransform.position, 0.85f, 1.15f);
            _fishAudioEvent.SetAndSpawnOneShot(fishCaught.CatchClip ?? _defaultFishCatchClip, _rippleTransform.position);
                
            yield return CatchFishAnimation();
            _catchFishPopup.ShowPopup(fishCaught, weight, newHeaviest, inventoryFull);
        }
        else yield return CatchFishAnimation();
        
        yield return new WaitForSeconds(0.5f);
        ShowInitialPrompts();
    }

    private IEnumerator CatchFishAnimation()
    {
        _hc.attackAudioTable.PlayOneShot(_hc.audioSource);
        _catchVoiceClip.PlayOnSource(_hc.audioSource);
        
        _hc.animCtrl.Play("DownSpike Burst");
        Vector3 cachedPosition = _hc.transform.position;
        _cc.FreezeInPlace();
        _hc.transform.position = Vector3.Lerp(_hc.transform.position, _rippleTransform.position, _animationOffset);
        _waterRegion.DoSplashIn(_rippleTransform, new Vector3(0, 1, 0), true);
        yield return new WaitForSeconds(_hc.animCtrl.GetCurrentClipDuration() - 0.1f);
        _hc.transform.position = cachedPosition;
        _cc.StopFreeze();
        
        _hc.animCtrl.Play("Downspike Recovery");
        yield return new WaitForSeconds(_hc.animCtrl.GetCurrentClipDuration() - 0.1f);
        _hc.animCtrl.Play("ChallengeStrongToIdle");
        yield return new WaitForSeconds(_hc.animCtrl.GetCurrentClipDuration() - 0.1f);
        HeroTalkAnimation.PlayLookAnimation(HeroTalkAnimation.AnimationTypes.LookHalfDown, false);
    }

    private void PromptSelected()
    {
        ControlReminder._instance.HideAll(false, false);
        _waitingForInput = false;
    }

    private void OnTakenDamage()
    {
        ControlReminder._instance.HideAll(true, false);
        EndDialogue();
    }

    private IEnumerator PlayRippleAnimation(SpriteRenderer renderer)
    {
        int i = 0;
        while (true)
        {
            renderer.sprite = _rippleAnimation[i];
            i = (i + 1) % _rippleAnimation.Length;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
using System.Collections;
using System.Globalization;
using GlobalSettings;
using TeamCherry.Localization;
using TeamCherry.NestedFadeGroup;
using TMProOld;
using UnityEngine;

namespace PharloomFishing;

public class FishingLogCollectableItem : FishingLogItemBase
{
    public float weight;
    public TextMeshPro weightText;
    public bool locked;
    public GameObject lockIndicator;
 
    private FishingLogCollectable? _collectable;
    private NestedFadeGroupSpriteRenderer _fadeGroup;
    private Vector2 _fishIconInitialPosition;
    private Vector2 _lockIndicatorInitialPosition;
    private bool _doJitter;
    private const float JitterStrength = 0.05f;
    private const float JitterDuration = 0.075f;
    private const float LockFadeTime = 0.02f;
    private const float UnlockFadeTime = 0.1f;
    
    private readonly LocalisedString _weightFormat = new("UI", "FISH_WEIGHT");
    private readonly Vector2 _fadeOutScale = new(0.2f, 0.2f);
    private readonly Vector2 _fadeInScale = new(0.58f, 0.58f);
    private readonly Vector2 _fullScale = new(0.65f, 0.65f);

    public override string DisplayName => fish?.Name ?? string.Empty;

    public override string Description
    {
        get
        {
            if (fish == null)
            {
                return string.Empty;
            }
            string hex = weight >= 1f 
                ? ColorUtility.ToHtmlStringRGB(UI.MaxItemsTextColor)
                : ColorUtility.ToHtmlStringRGB(Color.white);
            return fish.Description + string.Format(_weightFormat, fish.GetWeightText(weight), hex, "110%");
        }
    }

    public override bool IsSeen
    {
        get => true;
        set { }
    }
    
    public override bool ShouldDisplayFish()
    {
        return fish != null;
    }

    public override void Awake()
    {
        _fadeGroup = lockIndicator.GetComponent<NestedFadeGroupSpriteRenderer>();
        _fishIconInitialPosition = fishIcon.transform.localPosition;
        _lockIndicatorInitialPosition = lockIndicator.transform.localPosition;
        base.Awake();
    }

    public void SetupCollectable(FishingLogCollectable? collectable)
    {
        emptyIcon.SetActive(collectable == null);
        fishIcon.gameObject.SetActive(collectable != null);
        
        _collectable = collectable;
        if (collectable == null)
        {
            fish = null;
            weightText.text = string.Empty;
            locked = false;
            lockIndicator.SetActive(false);
            base.UpdateDisplay();
            return;
        }
        
        fish = collectable.Fish;
        weight = collectable.Weight;
        weightText.text = fish.GetWeightText(weight);
        weightText.color = weight >= 1f ? UI.MaxItemsTextColor : Color.white;
        locked = collectable.Locked;
        lockIndicator.SetActive(true);
        _fadeGroup.FadeTo(locked ? 1f : 0f, 0f, isRealtime: true);
        lockIndicator.transform.ScaleTo(this, locked ? _fullScale : _fadeOutScale, 0f, isRealtime: true);
        fishIcon.sprite = fish?.FishSprite;
        base.UpdateDisplay();
    }

    public bool ToggleLock()
    {
        if (fish == null) return false;
        locked = !locked;
        _collectable?.Locked = locked;
        float fadeTime = locked ? LockFadeTime : UnlockFadeTime;
        if (locked)
        {
            lockIndicator.transform.localScale = _fadeInScale;
            _doJitter = true;
            StartCoroutine(StopJitter());
        }
        _fadeGroup.FadeTo(locked ? 1f : 0f, fadeTime, isRealtime: true);
        lockIndicator.transform.ScaleTo(this, locked ? _fullScale : _fadeOutScale, fadeTime, isRealtime: true);
        return true;
    }

    private void Update()
    {
        if (!_doJitter) return;
        Vector2 offset1 = Random.insideUnitCircle * JitterStrength;
        Vector2 offset2 = Random.insideUnitCircle * JitterStrength/2;
        fishIcon.transform.localPosition = _fishIconInitialPosition + new Vector2(offset1.x, offset1.y);
        lockIndicator.transform.localPosition = _lockIndicatorInitialPosition + new Vector2(offset2.x, offset2.y);
    }

    private IEnumerator StopJitter()
    {
        yield return new WaitForSecondsRealtime(JitterDuration);
        _doJitter = false;
        fishIcon.transform.localPosition = _fishIconInitialPosition;
        lockIndicator.transform.localPosition = _lockIndicatorInitialPosition;
    }
}
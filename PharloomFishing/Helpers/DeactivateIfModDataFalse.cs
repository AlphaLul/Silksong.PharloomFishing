using System;
using UnityEngine;

namespace PharloomFishing;

public class DeactivateIfModDataFalse : MonoBehaviour, GameMapPinLayout.IEvaluateHook
{
    private void Start()
    {
        _hasStarted = true;
        ForceEvaluate();
    }

    private void OnEnable()
    {
        if (!_hasStarted) return;
        ForceEvaluate();
    }

    public void ForceEvaluate()
    {
        if (_gm == null) _gm = GameManager.instance;
        if (_condition()) return;
        if (_component != null)
        {
            _component.enabled = false;
            return;
        }
        gameObject.SetActive(false);
    }

    public void SetCondition(Func<bool> condition)
    {
        _condition = condition;
    }

    public void SetComponent(MonoBehaviour component)
    {
        _component = component;
    }

    private Func<bool> _condition = () => false;
    private MonoBehaviour _component;
    private bool _hasStarted;
    private GameManager _gm;
}
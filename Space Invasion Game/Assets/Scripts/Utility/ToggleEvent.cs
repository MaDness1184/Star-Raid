using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
struct ToggleEvent
{
    [SerializeField] private UnityEvent _OnTurnOn;
    [SerializeField] private UnityEvent _OnTurnOff;

    public UnityEvent OnTurnOn { get { return _OnTurnOn; } }
    public UnityEvent OnTurnOff { get { return _OnTurnOff; } }
}

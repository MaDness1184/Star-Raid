using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : EntityStatus
{
    [Header("Settings")]
    [SerializeField] private int maxHP = 10;

    [Header("Debugs")]
    [SyncVar(hook = nameof(HandleHPChange))]
    [SerializeField] protected int _currentHP;

    protected virtual void HandleHPChange(int oldHP, int newHP)
    {
        //UpdateUI
        
    }

    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
            _currentHP = maxHP;
    }

    [Server]
    protected override void DealDamage(int damage)
    {
        
        if(_currentHP <= 0)
        {
            Debug.Log(gameObject.name + " died");
        }
        else
        {
            _currentHP -= damage;
            Debug.Log(gameObject.name + " HP = " + _currentHP);
        }
    }
}

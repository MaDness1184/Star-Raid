using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStatus : NetworkBehaviour
{
    [Header("Entity Settings")]
    [SerializeField] protected readonly int maxHP = 10;
    [SerializeField] private HostilityType hostility;

    [Header("Entity Debug")]
    [SyncVar(hook = nameof(HandleHPChange))]
    [SerializeField] protected int internalCurrentHP;

    protected virtual void HandleHPChange(int oldHP, int newHP)
    {
        //UpdateUI
    }

    [Command(requiresAuthority = false)]
    public void CmdDealDamage(int damage) 
    {
        DealDamage(damage);
    }

    [Server]
    protected virtual void DealDamage(int damage)
    {

    }

    public int GetCurrentHP()
    {
        return internalCurrentHP;
    }

    public HostilityType GetHostility()
    {
        return hostility;
    }
}

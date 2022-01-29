using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStatus : NetworkBehaviour
{
    [Header("Entity Settings")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private HostilityType hostility;
    [SerializeField] protected GameObject[] deathVfxs;

    [Header("Entity Debug")]
    [SyncVar(hook = nameof(HandleHPChange))]
    [SerializeField] protected int internalCurrentHP;

    protected virtual void HandleHPChange(int oldHP, int newHP)
    {
        //UpdateUI
    }

    [Command(requiresAuthority = false)]
    public void CmdDealDamage(int damage, NetworkIdentity dealerIdentity) 
    {
        //TODO: Pool enemy for less bug
        //Current bug: enemy is destroyed before all pellet of shotgun can finish dealing damage
        // leading to object not found for command
        DealDamage(damage, dealerIdentity);
    }

    [Server]
    protected virtual void DealDamage(int damage, NetworkIdentity dealerIdentity)
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

    protected int GetMaxHP()
    {
        return maxHP;
    }

    public override void OnStopClient()
    {
        foreach (GameObject vfxGO in deathVfxs)
        {
            GameObject go = Instantiate(vfxGO, transform.position, Quaternion.identity);
        }
    }

    /*[ClientRpc]
    protected void RPCSpawnDeathVFXs()
    {
        foreach (GameObject vfxGO in deathVfxs)
        {
            GameObject go = Instantiate(vfxGO, transform.position, Quaternion.identity);
        }  
    }*/
}


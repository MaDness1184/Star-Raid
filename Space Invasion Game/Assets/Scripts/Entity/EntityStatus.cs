using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStatus : NetworkBehaviour
{
    [Header("Entity Specific Settings")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private HostilityType hostility;
    [SerializeField] private ParticleSystem onDamagedParticle;
    [SerializeField] protected GameObject[] onDespawnVfxs;

    //[Header("Entity Debug")]
    [SyncVar(hook = nameof(HandleHPChange))]
    protected int internalCurrentHP;

    protected virtual void HandleHPChange(int oldHP, int newHP)
    {
        //UpdateUI
    }

    [Command(requiresAuthority = false)]
    public void CmdDealDamage(int damage, NetworkIdentity perpetratorIdentity) 
    {
        //TODO: Pool enemy for less bug
        //Current bug: enemy is destroyed before all pellet of shotgun can finish dealing damage
        // leading to object not found for command
        DealDamage(damage, perpetratorIdentity);
    }

    [Server]
    protected virtual void DealDamage(int damage, NetworkIdentity perpetratorIdentity)
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
        foreach (GameObject vfxGO in onDespawnVfxs)
        {
            GameObject go = Instantiate(vfxGO, transform.position, Quaternion.identity);
        }
    }

    [Server]
    protected void PlayOnDamagedParticle()
    {
        RpcPlayOnDamagedParticle();
    }

    [ClientRpc]
    private void RpcPlayOnDamagedParticle()
    {
        onDamagedParticle.Play();
    }
}


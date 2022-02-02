using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStatus : NetworkBehaviour
{
    [Header("Entity Settings")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private HostilityType hostility;
    [SerializeField] private ParticleSystem onDamagedParticle;
    [SerializeField] protected GameObject[] deathVfxs;

    [Header("Entity Debug")]
    [SyncVar(hook = nameof(HandleHPChange))]
    [SerializeField] protected int internalCurrentHP;

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

    /*[Command(requiresAuthority = false)]
    public void CmdKnockBack(float force, Vector3 source)
    {
        RpcKnockBack(force, source);
    }

    [ClientRpc]
    protected virtual void RpcKnockBack(float force, Vector3 source)
    {
        if (!hasAuthority) return;

        Debug.Log("Knocking back entity");

        Vector3 direction = source - transform.position;
        GetComponent<Rigidbody2D>().AddForce(direction * force);
    }*/

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

    /*[ClientRpc]
    protected void RPCSpawnDeathVFXs()
    {
        foreach (GameObject vfxGO in deathVfxs)
        {
            GameObject go = Instantiate(vfxGO, transform.position, Quaternion.identity);
        }  
    }*/
}


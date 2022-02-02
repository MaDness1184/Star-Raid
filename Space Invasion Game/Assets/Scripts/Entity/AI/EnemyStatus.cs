using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatus : EntityStatus
{
    [Header("Enemy Settings")]
    [SerializeField] private bool dummy = false;
    [SerializeField] private float baseKnockbackForce = 300f;

    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            internalCurrentHP = GetMaxHP();
            GameManager.instance.NotifyEnemySpawn();
        }  
    }

    [Server]
    protected override void DealDamage(int damage, NetworkIdentity perpetratorIdentity)
    {
        PlayOnDamagedParticle();
        if (dummy) return;

        internalCurrentHP -= damage;

        if (internalCurrentHP <= 0)
        {
            internalCurrentHP = 0;
            GameManager.instance.NotifyEnemyDeSpawn(perpetratorIdentity);
            //RPCSpawnDeathVFXs();
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            KnockBack(damage * baseKnockbackForce, perpetratorIdentity.transform.position);
        }
    }

    [Server]
    private void KnockBack(float force, Vector3 source)
    {
        Vector3 direction = transform.position - source;
        GetComponent<Rigidbody2D>().AddForce(direction * force);
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

        
        
    }*/
}

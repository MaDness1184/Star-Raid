using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatus : EntityStatus
{
    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            internalCurrentHP = maxHP;
            Hive.instance.NotifyEnemySpawn();
        }  
    }

    [Server]
    protected override void DealDamage(int damage)
    {

        if (internalCurrentHP <= 0)
        {
            Hive.instance.NotifyEnemyDeSpawn();
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            internalCurrentHP -= damage;
            //Debug.Log(gameObject.name + " HP = " + _currentHP);
        }
    }
}

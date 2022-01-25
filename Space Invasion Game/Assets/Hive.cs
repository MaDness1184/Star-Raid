using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hive : NetworkBehaviour
{
    static public Hive instance;

    [Header("Setting")]
    [SerializeField] private int spawnLimit = 10;
    [SerializeField] private float spawnCdr = 0.5f;

    [Header("Required Components")]
    [SerializeField] GameObject enemy;

    [Header("Debugs")]
    [SerializeField] private int population;
    [SerializeField] private Transform target;

    private float nextSpawn;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(instance);
    }

    [ServerCallback]
    // Update is called once per frame
    void Update()
    {
        if(Time.time > nextSpawn && population <= spawnLimit)
        {
            nextSpawn += spawnCdr;
            GameObject enemyGO = Instantiate(enemy);
            NetworkServer.Spawn(enemyGO);
        }
    }

    [Server]
    public void NotifyEnemySpawn()
    {
        population++;
    }

    [Server]
    public void NotifyEnemyDeSpawn()
    {
        population--;
    }

    [Server]
    public Transform GetTarget()
    {
        return target;
    }
}

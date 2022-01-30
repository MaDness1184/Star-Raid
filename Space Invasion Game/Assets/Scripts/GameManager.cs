using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    static public GameManager instance;

    [Header("Setting")]
    [SerializeField] private int spawnLimit = 10;
    [SerializeField] private float spawnCdr = 0.5f;

    [Header("Required Components")]
    [SerializeField] GameObject enemy;

    [Header("Debugs")]
    [SerializeField] private int population; //TODO: Add real population control using Enum
    [SerializeField] private Transform target;

    private Dictionary<NetworkIdentity, int> playerAggros = new Dictionary<NetworkIdentity, int>();

    private float nextSpawn;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(instance);
    }

    [Server]
    public void AddNewPlayer(NetworkIdentity identity)
    {
        if (playerAggros.ContainsKey(identity)) return;

        playerAggros.Add(identity, Random.Range(0, 1));

        UpdateAggroTarget();
    }

    [ServerCallback]
    // Update is called once per frame
    void Update()
    {
        if(Time.time > nextSpawn && population < spawnLimit)
        {
            nextSpawn += spawnCdr;
            GameObject enemyGO = Instantiate(enemy) as GameObject;
            //Can't put enemy under the hive because nested Network Idendity is not allowed
            //Check this one out https://mirror-networking.gitbook.io/docs/components/network-identity
            //enemyGO.transform.SetParent(transform); // Put the enemy under the hive
            NetworkServer.Spawn(enemyGO);
        }
    }

    [Server]
    public void NotifyEnemySpawn()
    {
        population++;
    }

    [Server]
    public void NotifyEnemyDeSpawn(NetworkIdentity dealerIdentity)
    {
        if(playerAggros.ContainsKey(dealerIdentity))
            playerAggros[dealerIdentity]++;

        //Debug.Log(dealerIdentity.name + " aggo = " + playerAggros[dealerIdentity]);

        UpdateAggroTarget();

        population--;
    }

    [Server]
    private void UpdateAggroTarget()
    {
        target = playerAggros.Aggregate((x, y) => x.Value > y.Value ? x : y).Key.transform;
        //Debug.Log("Aggro = " + target);
    }

    [Server]
    public void PullAggroToTarget(NetworkIdentity identity)
    {
        playerAggros[identity] *= 2;
    }

    [Server]
    public void NotifyPlayerDown(NetworkIdentity identity)
    {

    }


    [Server]
    public Transform GetTarget()
    {
        return target;
    }
}

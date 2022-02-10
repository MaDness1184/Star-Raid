using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : NetworkBehaviour
{
    static public GameManager instance;

    [Header("Setting")]
    [SerializeField] private bool canSpawn = true;
    [SerializeField] private int spawnLimit = 10;
    [SerializeField] private int poolLimit = 10;
    [SerializeField] private float spawnCdr = 0.5f;
    [SerializeField] private float spawnDelay = 5f;

    [Header("Required Components")]
    [SerializeField] GameObject enemy;

    [Header("Debugs")]
    [SerializeField] private int population; //TODO: Add real population control using Enum
    [SerializeField] private Transform target;

    [SerializeField]
    private static List<Vector3> enemySpawnPoints = new List<Vector3>();

    [SerializeField]
    private List<EnemyStatus> enemyStatusPool = new List<EnemyStatus>();
    //[SerializeField] private Transform[] enemySpawnPoints;

    private Dictionary<NetworkIdentity, int> playerAggros = new Dictionary<NetworkIdentity, int>();

    private float nextSpawn;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(instance);
    }

    private void Start()
    {
        if (!isServer) return;

        nextSpawn = Time.time + spawnDelay;

        poolLimit = spawnLimit;
        GameObject enemyDropletGO;
        EnemyStatus enemyDropletStatus;

        for (int i = 0; i < poolLimit; i++)
        {
            enemyDropletGO = Instantiate(enemy, transform);
            NetworkServer.Spawn(enemyDropletGO);

            enemyDropletStatus = enemyDropletGO.GetComponent<EnemyStatus>();
            enemyDropletStatus.DeSpawnToPool();
            enemyStatusPool.Add(enemyDropletStatus);
        }

        //DebugConsole.Log("enemySpawnPoints.Count = " + enemySpawnPoints.Count);
    }

    EnemyStatus enemyStatus;
    [ServerCallback]
    // Update is called once per frame
    void Update()
    {
        if (!canSpawn) return;

        if (Time.time > nextSpawn && population < spawnLimit)
        {
            nextSpawn = Time.time + spawnCdr;

            for (int i = 0; i < poolLimit; i++)
            {
                if (!enemyStatusPool[i].isSpawned)
                    enemyStatus = enemyStatusPool[i];
            }

            if (enemyStatus == null || enemyStatus.isSpawned) return;

            enemyStatus.SpawnFromPool(enemySpawnPoints.Count == 0 ? Vector3.zero :
                enemySpawnPoints[Random.Range(0, enemySpawnPoints.Count)]);
            population++; // NotifyEnemySpawn()
        }
    }

    #region Enemy Spawn
    public static void RegisterEnemySpawnPoint(Vector3 position)
    {
        enemySpawnPoints.Add(position);
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

    #endregion

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
        playerAggros[identity] = 1;
        UpdateAggroTarget();
    }

    [Server]
    public void OnNewPlayerAdded(NetworkIdentity identity)
    {
        if (playerAggros.ContainsKey(identity)) return;

        playerAggros.Add(identity, Random.Range(0, 1));

        UpdateAggroTarget();
    }

    [Server]
    public Transform GetTarget()
    {
        return target;
    }

    #region Context Menu

    [Server]
    public void EnableEnemySpawn( bool enable)
    {
        canSpawn = enable;
    }

    [Server]
    public void SetSpawnLimit(int limit)
    {
        if (limit > spawnLimit) return;

        spawnLimit = limit;
        poolLimit = limit;
    }

    #endregion
}

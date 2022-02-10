using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameManager.RegisterEnemySpawnPoint(transform.position);
    }
}

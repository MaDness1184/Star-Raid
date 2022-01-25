using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hive : NetworkBehaviour
{
    static public Hive hive;

    [Header("Setting")]
    [SerializeField] private int spawnLimit = 10;
    [SerializeField] private float spawnCdr = 0.5f;

    [Header("Required Components")]
    [SerializeField] GameObject enemy;

    private void Awake()
    {
        if (hive == null)
            hive = this;
        else
            Destroy(hive);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
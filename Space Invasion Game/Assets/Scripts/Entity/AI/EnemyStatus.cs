using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class EnemyStatus : EntityStatus
{
    [Header("Enemy Specific Settings")]
    [SerializeField] private bool dummy = false;
    [SerializeField] private float baseKnockbackForce = 300f;

    private Rigidbody2D rb2D;
    private SpriteRenderer[] spriteRenderers;
    private Light2D[] lights;

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();

        rb2D = GetComponent<Rigidbody2D>();
        rb2D.simulated = false;

        lights = GetComponentsInChildren<Light2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (isServer)
            ResetCurrentHP();

        if (isClient)
            ClientEnableEntity(isSpawned);

    }

    [Server]
    public void SpawnFromPool(Vector3 position)
    {
        EntityTeleportAndSpawn(position);
        ResetCurrentHP();
    }

    [Server]
    public void DeSpawnToPool()
    {
        EntityDeSpawn();
        SetCurrentHP(0);
    }

    [Client]
    protected override void ClientEnableEntity(bool enable)
    {
        rb2D.simulated = enable;
        if (rb2D.bodyType != RigidbodyType2D.Static)
        {
            rb2D.velocity = Vector3.zero;
            rb2D.angularVelocity = 0;
        }

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            spriteRenderer.color = enable ? Color.white : Color.clear;

        foreach (Light2D light in lights)
            light.enabled = enable;
    }

    [Server]
    public override void RecieveDamage(int damage, NetworkIdentity perpetratorIdentity)
    {
        if (currentHP <= 0) return;

        RpcPlayOnDamagedSFXs();

        if (dummy) return;

        ModifyCurrentHP(-damage);

        if (currentHP <= 0) // Death
        {
            RpcPlayOnDespawnVFXs(transform.position);
            RpcPlayOnDeSpawnSFXs();

            DropLoot();
            DeSpawnToPool();
            
            GameManager.instance.NotifyEnemyDeSpawn(perpetratorIdentity);
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
}

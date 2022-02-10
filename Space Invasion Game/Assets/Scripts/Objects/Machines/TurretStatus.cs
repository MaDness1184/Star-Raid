using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class TurretStatus : Machinery
{
    private SpriteRenderer[] spriteRenderers;
    private Light2D[] lights;
    private Collider2D[] collider2Ds;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        lights = GetComponentsInChildren<Light2D>();
        collider2Ds = GetComponentsInChildren<Collider2D>();
    }

    private void Start()
    {
        if (isServer)
            ResetCurrentHP();

        if (isClient)
            ClientEnableEntity(isSpawned);
    }

    [Server]
    public void Pickup()
    {
        EntityDeSpawn();
    }

    [Server]
    public void Dropdown(Vector3 position)
    {
        EntityTeleportAndSpawn(position);
    }

    [Client]
    protected override void ClientEnableEntity(bool enable)
    {
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            spriteRenderer.color = enable ? Color.white : Color.clear;

        foreach (Light2D light in lights)
            light.enabled = enable;

        foreach (Collider2D collider in collider2Ds)
            collider.enabled = enable;
    }

    [Server]
    public void Teleport(Vector3 position)
    {
        transform.position = position;
    }
}

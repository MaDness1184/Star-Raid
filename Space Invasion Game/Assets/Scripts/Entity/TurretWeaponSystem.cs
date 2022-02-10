using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretWeaponSystem : EntityWeaponSystem
{
    [Header("Turret Specific Settings")]
    [SerializeField] private float scanCdr = 0.1f;
    [SerializeField] private float turretRotationSpeed = 15f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Turret Debugs")]
    [SerializeField] private Transform target;

    private float nextShoot;
    private float nextScan;
    private float closetSqrDistance;
    private Transform closetTransform;

    private Vector2 hitPointCache; // TODO: delete when finish debugging


    [ServerCallback]
    private void Update()
    {
        if (target == null || !controllable) return;
        Shoot();
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (!controllable) return;

        ScanForTarget();
        Aim();
    }

    [Server]
    private void ScanForTarget()
    {
        if (Time.time < nextScan) return;
        nextScan = Time.time + scanCdr;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position,
            currentWeapon.range, enemyLayer);

        if (hitColliders.Length <= 0)
        {
            target = null;
            return;
        }

        closetSqrDistance =
            (transform.position - hitColliders[0].transform.position).sqrMagnitude;

        foreach (Collider2D collider in hitColliders)
        {
            if (collider.isTrigger)
                continue;

            if (collider.TryGetComponent<EntityStatus>(out EntityStatus entityStatus))
            {
                if (collider.transform == closetTransform ||
                    entityStatus.GetHostility() == HostilityType.Friendly ||
                    entityStatus.GetHostility() == HostilityType.Neutral)
                    continue;

                var sqrDistance = (transform.position - collider.transform.position).sqrMagnitude;

                if (sqrDistance <= closetSqrDistance)
                {
                    closetSqrDistance = sqrDistance;
                    closetTransform = collider.transform;
                }
            }
        }

        if (closetTransform != null)
            target = closetTransform;
    }

    [Server]
    private void Aim()
    {
        if (target == null) return;

        Vector2 direction = target.position - azimuth.position;
        direction.Normalize();

        float rotZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //head.rotation = Quaternion.Lerp(head.rotation, Quaternion.Euler(0f, 0f, rotZ), 0.5f);
        azimuth.rotation = Quaternion.RotateTowards(azimuth.rotation,
            Quaternion.Euler(0f, 0f, rotZ), turretRotationSpeed);
    }

    [Server]
    private void Shoot()
    {
        if (Time.time < nextShoot) return;

        nextShoot = Time.time + currentWeapon.primaryCdr;
        ServerShootProjectiles();
    }

    private void OnDrawGizmos()
    {
        if (hitPointCache != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(muzzle.position, hitPointCache);
        }
    }
}

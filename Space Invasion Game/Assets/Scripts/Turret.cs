using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : Machinery
{
    [Header("Turret Specific Settings")]
    [SerializeField] private ProjectileWeapon weaponModel;
    [SerializeField] private float scanCdr = 0.1f;
    [SerializeField] private float turretRotationSpeed = 15f;

    [Header("Required Components")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask entityLayer;
    [SerializeField] private LayerMask projectileBlockLayer;

    private Animator animator;

    private float nextShoot;
    private float nextScan;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }


    [ServerCallback]
    void Update()
    {
        if (target == null) return;
        Shoot();
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        ScanForTarget();
        Aim();
    }


    Transform closetTransform;
    float closetSqrDistance;

    private void ScanForTarget()
    {
        if (Time.time < nextScan) return;
        nextScan = Time.time + scanCdr;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position,
            weaponModel.range, entityLayer);

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

            if(collider.TryGetComponent<EntityStatus>(out EntityStatus entityStatus))
            {
                if (collider.transform == closetTransform || 
                    entityStatus.GetHostility() == GetHostility())
                    continue;

                var sqrDistance = (transform.position - collider.transform.position).sqrMagnitude;

                if (sqrDistance <= closetSqrDistance)
                {
                    closetSqrDistance = sqrDistance;
                    closetTransform = collider.transform;
                }
            }
        }

        if(closetTransform != null)
            target = closetTransform;
    }

    private void Aim()
    {
        if (target == null) return;

        Vector2 direction = target.position - head.position;
        direction.Normalize();

        float rotZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //head.rotation = Quaternion.Lerp(head.rotation, Quaternion.Euler(0f, 0f, rotZ), 0.5f);
        head.rotation = Quaternion.RotateTowards(head.rotation,
            Quaternion.Euler(0f, 0f, rotZ), turretRotationSpeed);
    }

    Vector2 targetDirection;
    Vector2 hitPointCache;
    private void Shoot()
    {
        if (Time.time < nextShoot) return;

        Quaternion spreadRot = Quaternion.Euler(0, 0, Random.Range(-weaponModel.spread,
                weaponModel.spread));

        /*if (Physics2D.Raycast(transform.position, spreadRot * transform.right, weaponModel.range,
                projectileBlockLayer))
                return; // make sure turret don't shoot at wall*/

        hitPointCache = Physics2D.Raycast(transform.position, spreadRot * transform.right).point;

        targetDirection = target.position - transform.position;
        if (targetDirection.sqrMagnitude < (weaponModel.range * weaponModel.range))
        {
            nextShoot = Time.time + weaponModel.primaryCdr;
            animator.SetTrigger("shootTrigger");

            RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 0.1f,
                targetDirection, weaponModel.range);

            if (hits.Length <= 0)
            {
                RpcBulletVFX(spreadRot, VfxType.Spark); // hit nothing in range
                return; // continue
            }

            foreach (RaycastHit2D hit in hits)
            {
                //DebugConsole.Log("name = " + hit.collider.name + ", is trigger = " + hit.collider.isTrigger);

                if (hit.collider.tag == "Interactive")
                    continue;

                if (hit.collider.isTrigger)
                    continue;

                if (hit.collider.TryGetComponent<EntityStatus>(out EntityStatus entityStatus))
                {
                    HostilityType hitHostility = entityStatus.GetHostility();
                    if (hitHostility == HostilityType.Hostile ||
                        hitHostility == HostilityType.Neutral)
                    {
                        RpcBulletVFX(spreadRot, VfxType.NonSpark);

                        hitPointCache = hit.point;
                        entityStatus.CmdDealDamage(weaponModel.damage, netIdentity);
                        if (weaponModel.ammoType != AmmoType.Penetration)
                            break; 
                    }
                }
                else
                {
                    RpcBulletVFX(spreadRot, VfxType.Spark);
                    break; // player hit a wall before hitting an enemy
                }
            }

        }
    }

    #region Bullet VFX

    /*[Command]
    private void CmdBulletVFX(Quaternion spreadRot, VfxType vfxType)
    {
        RpcBulletVFX(spreadRot, vfxType);
    }*/

    [ClientRpc]
    private void RpcBulletVFX(Quaternion spreadRot, VfxType vfxType)
    {
        GameObject vfx = weaponModel.sparkProjectileVfx;
        if (vfxType == VfxType.NonSpark)
            vfx = weaponModel.nonSparkProjectileVfx;

        GameObject bullet = Instantiate(vfx, muzzle.position, head.rotation * spreadRot);

        ParticleSystem bulletPS = bullet.GetComponent<ParticleSystem>();
        var main = bulletPS.main;

        var velocity = bulletPS.velocityOverLifetime;
        //Debug.Log((currentWeapon.range) / velocity.x.constant);

        main.startLifetime = (weaponModel.range + 1) / velocity.x.constant;
    }

    #endregion

    // Add this method to the script where the turret's animator componet is
    [Server]
    private void UpdateAnimationDirection(Vector2 newDirection)
    {
        animator.SetFloat("directionX", newDirection.x);
        animator.SetFloat("directionY", newDirection.y);
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

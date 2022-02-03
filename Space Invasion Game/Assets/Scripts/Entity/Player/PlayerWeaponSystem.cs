using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponSystem : NetworkBehaviour
{
    [Header("Required Components")]
    [SerializeField] private Transform arm;
    [SerializeField] private Transform hand;
    [SerializeField] private LayerMask projectileBlockLayer;
    [SerializeField] private GameObject normalBulletParticle;
    [SerializeField] private ProjectileWeapon[] projectileWeapons;

    [Header("Debugs")]
    [SerializeField] [SyncVar]
    private int currentWeaponIndex;
    private ProjectileWeapon currentWeapon;
    private PlayerInventory playerInventory;
    private Animator animator;

    private bool spraying;
    private float nextPrimaryShootable;
    private bool reloading;
    private bool controllable = true;

    private readonly SyncList<int> currentAmmoCounts = new SyncList<int>();

    private void Start()
    {
        playerInventory = GetComponent<PlayerInventory>();
        animator = GetComponent<Animator>();

        currentWeaponIndex = 0;
        currentWeapon = projectileWeapons[0];

        if (!isServer) return;

        for (int i = 0; i < projectileWeapons.Length; i++)
        {
            currentAmmoCounts.Add(projectileWeapons[i].magazineSize);
        }
    }

    [ClientCallback]
    private void Update()
    {
        if (!hasAuthority) return;

        if (!NetworkClient.ready || currentAmmoCounts.Count <= 0) return;

        PlayerUI.instance.updateText(currentWeapon.name + " "
            + currentAmmoCounts[currentWeaponIndex]
            + "/" + currentWeapon.magazineSize
            + "\n" + playerInventory.AmmoCountToString());
    }

    #region Weapon Selection

    [ClientCallback]
    public void WeaponSelection(InputAction.CallbackContext context)
    {
        if (!hasAuthority) return;

        if (context.performed)
        {
            int hotkeyNum = int.Parse(context.control.name) - 1;
            if (hotkeyNum < projectileWeapons.Length)
            {
                CmdSwitchWeapon(hotkeyNum);

                currentWeaponIndex = hotkeyNum;
                currentWeapon = projectileWeapons[hotkeyNum];
                spraying = false;
            }

            reloading = false;
        }
    }

    [Command]
    private void CmdSwitchWeapon(int weaponSlot)
    {
        currentWeaponIndex = weaponSlot;
    }

    #endregion

    #region Primary Attack

    [Client]
    private bool PrimaryAmmoCheck()
    {
        if (reloading) return false;

        if (currentAmmoCounts[currentWeaponIndex] <= 0)
        {
            StartCoroutine(Reload());
            return false;
        }
        else
        {
            CmdSpendAmmo();
            return true;
        }
    }

    [Command]
    private void CmdSpendAmmo()
    {
        currentAmmoCounts[currentWeaponIndex]--;
    }

    [Client]
    public void PrimaryPerformed()
    {
        if (!PrimaryAmmoCheck() || !controllable) return;
        
        if (currentWeapon.automatic)
        {
            spraying = true;
            StartCoroutine(AutomaticShoot());
        }
        else
        {
            if (Time.time < nextPrimaryShootable) return;
            nextPrimaryShootable = Time.time + currentWeapon.primaryCdr;

            ShootProjectiles();
        }
    }

    [Client]
    public void PrimaryReleased()
    {
        spraying = false;
    }

    #endregion

    #region Reload

    [Client]
    IEnumerator Reload()
    {
        reloading = true;

        playerInventory.CmdSpendAmmo(currentWeapon.ammoType, currentWeapon.magazineSize);
        yield return new WaitForSeconds(currentWeapon.reloadTime);

        reloading = false;
    }

    [Server]
    public void RpcReload(int amount)
    {
        currentAmmoCounts[currentWeaponIndex] = amount;
    }

    #endregion

    #region Projectile Shooting Mechanisim

    [Client]
    IEnumerator AutomaticShoot()
    {
        while (spraying && PrimaryAmmoCheck())
        {
            ShootProjectiles();
            yield return new WaitForSeconds(currentWeapon.primaryCdr);
        }
    }

    Vector2 hitPointCache;

    [Client]
    private void ShootProjectiles()
    {
        if (!controllable) return;

        // Character VFX
        animator.SetTrigger("shootTrigger");
        // TODO: gun sound here

        for (int i = 0; i < currentWeapon.pelletPerShot; i++)
        {
            Quaternion spreadRot = Quaternion.Euler(0, 0, Random.Range(-currentWeapon.spread,
                currentWeapon.spread));

            hitPointCache = Physics2D.Raycast(hand.position, spreadRot * hand.right).point;
            //CmdBulletVFX(spreadRot);

            /*if (Physics2D.Raycast(hand.position, spreadRot * hand.right, currentWeapon.range,
                projectileBlockLayer))
                continue;*/

            RaycastHit2D[] hits = Physics2D.RaycastAll(hand.position, spreadRot * hand.right,
                currentWeapon.range);

            if (hits.Length <= 0)
            {
                CmdBulletVFX(spreadRot, VfxType.Spark); // hit nothing in range
                continue;
            }

            foreach (RaycastHit2D hit in hits)
            {
                //TODO: find a way to block walls
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
                        CmdBulletVFX(spreadRot, VfxType.NonSpark);

                        hitPointCache = hit.point;
                        entityStatus.CmdDealDamage(currentWeapon.damage, netIdentity);
                        if (currentWeapon.ammoType != AmmoType.Penetration)
                            break; // TODO: Find a way to get max raycast point for VFX purpose
                    }
                }
                else
                {
                    CmdBulletVFX(spreadRot, VfxType.Spark);
                    break; // player hit a wall before hitting an enemy
                }
            }
        }
    }

    #endregion

    #region Bullet VFX

    [Command]
    private void CmdBulletVFX(Quaternion spreadRot, VfxType vfxType)
    {
        RpcBulletVFX(spreadRot, vfxType);
    }

    [ClientRpc]
    private void RpcBulletVFX(Quaternion spreadRot, VfxType vfxType)
    {
        GameObject vfx = currentWeapon.sparkProjectileVfx;
        if(vfxType == VfxType.NonSpark)
            vfx = currentWeapon.nonSparkProjectileVfx;

        GameObject bullet = Instantiate(vfx, hand.position, arm.rotation * spreadRot);

        ParticleSystem bulletPS = bullet.GetComponent<ParticleSystem>();
        var main = bulletPS.main;

        var velocity = bulletPS.velocityOverLifetime;
        //Debug.Log((currentWeapon.range) / velocity.x.constant);

        main.startLifetime = (currentWeapon.range + 1) / velocity.x.constant;
    }

    #endregion

    public void SetControllable(bool controllable)
    {
        this.controllable = controllable;
        spraying = false;
    }

    private void OnDrawGizmos()
    {
        if (hitPointCache != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(hand.position, hitPointCache);
        }
    }
}







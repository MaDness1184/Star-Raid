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
    [SerializeField] private ProjectileWeapon[] projectileWeapons;

    [Header("Debugs")]
    private ProjectileWeapon currentWeapon;
    private PlayerInventory playerInventory;

    private bool canShootAgain;
    private float nextPrimaryShootable;
    private bool reloading;

    private readonly SyncList<int> currentAmmoCounts = new SyncList<int>();

    private void Start()
    {
        playerInventory = GetComponent<PlayerInventory>();

        currentWeapon = projectileWeapons[0];

        if (!isServer) return;
        for(int i  = 0; i < projectileWeapons.Length; i++)
        {
            currentAmmoCounts.Add(projectileWeapons[i].magazineSize);
        }
    }
    #region Weapon Selection

    [ClientCallback]
    public void WeaponSelection (InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            int hotkeyNum = int.Parse(context.control.name) - 1;
            if(hotkeyNum < projectileWeapons.Length)
            {
                currentWeapon = projectileWeapons[hotkeyNum];
                if (currentWeapon.automatic) canShootAgain = true;
            }

            Debug.Log("Current weapon is " + currentWeapon.name);
        }
    }

    #endregion

    #region Primary Attack

    [Client]
    private bool PrimaryAmmoCheck()
    {
        if (reloading) return false;

        if (currentAmmoCounts[currentWeapon.weaponSlot] <= 0)
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
        currentAmmoCounts[currentWeapon.weaponSlot]--;
    }

    [ClientCallback]
    public void PrimaryPerformed()
    {
        if (!PrimaryAmmoCheck()) return;

        canShootAgain = false;
        if (currentWeapon.automatic)
        {
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
        canShootAgain = true;
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

    [ClientRpc]
    public void RpcReload(int amount)
    {
        currentAmmoCounts[currentWeapon.weaponSlot] = amount;
    }

    #endregion

    #region Projectile Shooting Mechanisim

    [Client]
    IEnumerator AutomaticShoot()
    {
        while (!canShootAgain && PrimaryAmmoCheck())
        {
            ShootProjectiles();
            yield return new WaitForSeconds(currentWeapon.primaryCdr);
        }
    }

    Vector2 hitPointCache;

    [Client]
    private void ShootProjectiles()
    {
        for(int i = 0; i < currentWeapon.pelletPerShot; i++)
        {
            Quaternion spreadRot = Quaternion.Euler(0, 0, Random.Range(-currentWeapon.spread, currentWeapon.spread));
            RaycastHit2D[] hitArray = Physics2D.RaycastAll(hand.position, spreadRot * hand.right, 30f);
            Debug.Log("hitArray length = " + hitArray.Length);

            if (hitArray.Length > 0)
            {
                //BulletTrailVfx(hit.point);
                foreach (RaycastHit2D hit in hitArray)
                {
                    if (hit.collider.TryGetComponent<EntityStatus>(out EntityStatus entityStatus))
                    {
                        HostilityType hitHostility = entityStatus.GetHostility();
                        if (hitHostility == HostilityType.Hostile || hitHostility == HostilityType.Neutral)
                        {
                            hitPointCache = hit.point;
                            entityStatus.CmdDealDamage(currentWeapon.damage);
                            if (currentWeapon.ammoType != AmmoType.Penetration)
                                break; // TODO: Find a way to get max raycast point for VFX purpose
                        }
                    }
                }
            }
            else
            {
                //BulletTrailVfx(transform.position + transform.right * maxRange);
            }

            CmdBulletVFX(spreadRot);
        }
    }

    #endregion

    #region Bullet VFX

    [Command]
    private void CmdBulletVFX(Quaternion spreadRot)
    {
        RpcBulletVFX(spreadRot);
    }

    [ClientRpc]
    private void RpcBulletVFX(Quaternion spreadRot)
    {
        Instantiate(currentWeapon.projectileVfxs[0], hand.position, arm.rotation * spreadRot);
    }

    #endregion


    private void OnDrawGizmos()
    {
        if (hitPointCache != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(hand.position, hitPointCache);
        }
    }
}

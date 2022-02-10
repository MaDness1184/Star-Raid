using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerWeaponSystem : EntityWeaponSystem
{
    private PlayerInventory playerInventory;

    private readonly SyncList<int> currentAmmoCounts = new SyncList<int>();

    private float nextPrimary;

    private bool spraying;
    private bool primaryReloadInterruption;
    private bool switchReloadInterruption;

    protected override void Awake()
    {
        base.Awake();

        playerInventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
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

        UpdateAmmoText();
    }

    #region PrimaryAttack

    [ClientCallback]
    public void OnPrimaryAttack(InputAction.CallbackContext context)
    {
        if (!hasAuthority || !controllable) return;

        if (context.performed)
        {
            primaryReloadInterruption = true;

            if (currentWeapon.automatic)
            {
                spraying = true;
                StartCoroutine(AutomaticShoot());
            }
            else
            {
                if (Time.time < nextPrimary) return;
                nextPrimary = Time.time + currentWeapon.primaryCdr;

                if (PrimaryAmmoCheck())
                ClientShootProjectiles();
            }
        }
        else if (context.canceled)
        {
            spraying = false;
        }
    }

    [Client]
    IEnumerator AutomaticShoot()
    {
        while (!reloading && controllable && spraying && PrimaryAmmoCheck())
        {
            ClientShootProjectiles();
            yield return new WaitForSeconds(currentWeapon.primaryCdr);
        }
    }

    #endregion

    #region AmmoCheck

    [Client]
    private bool PrimaryAmmoCheck()
    {
        bool result;

        if (reloading)
        {
            CmdPlayDrySFXs(); // SFX only
            result = false;
        }
        else if (currentAmmoCounts[currentWeaponIndex] <= 0)
        {
            StartCoroutine(ReloadCoroutine());
            result = false;
        }
        else
        {
            CmdSpendAmmo();
            result = true;
        }

        return result;
    }

    [Command]
    private void CmdSpendAmmo()
    {
        currentAmmoCounts[currentWeaponIndex]--;
    }

    [Command]
    private void CmdPlayDrySFXs()
    {
        RpcPlayDrySFXs();
    }

    [ClientRpc]
    private void RpcPlayDrySFXs()
    {
        sfxAudioSource.PlayOneShot(currentWeapon.drySfxs[Random.Range(0, currentWeapon.drySfxs.Length)]);
    }

    #endregion

    #region Reload
    [ClientCallback]
    public void OnReload(InputAction.CallbackContext context)
    {
        if (!hasAuthority || !controllable) return;

        if (context.performed)
        {
            if (reloading) return;

            StartCoroutine(ReloadCoroutine());
        }
    }

    [Client]
    IEnumerator ReloadCoroutine()
    {
        reloading = true;
        spraying = false;
        primaryReloadInterruption = false;
        switchReloadInterruption = false;

        if (currentWeapon.shotgunStyleReload)
        {
            while(currentAmmoCounts[currentWeaponIndex] < currentWeapon.magazineSize && !primaryReloadInterruption)
            {
                CmdPlayReloadSFXs();

                yield return new WaitForSeconds(currentWeapon.reloadTime);

                if (!switchReloadInterruption)
                    playerInventory.CmdSpendAmmo(currentWeapon.ammoType, 1);
            }

            CmdPlaySelectSFXs();
            if (primaryReloadInterruption) yield return new WaitForSeconds(currentWeapon.reloadTime);
        }
        else
        {
            CmdPlayReloadSFXs();

            yield return new WaitForSeconds(currentWeapon.reloadTime);

            if (!switchReloadInterruption) 
            {
                CmdPlaySelectSFXs();
                playerInventory.CmdSpendAmmo(currentWeapon.ammoType, currentWeapon.magazineSize - currentAmmoCounts[currentWeaponIndex]);
            }
        }

        reloading = false;
    }

    [Server]
    public void ReloadFromInventory(int amount)
    {
        currentAmmoCounts[currentWeaponIndex] += amount;
    }

    [Command]
    private void CmdPlayReloadSFXs()
    {
        RpcPlayReloadSFXs();
    }

    [ClientRpc]
    private void RpcPlayReloadSFXs()
    {
        sfxAudioSource.PlayOneShot(currentWeapon.reloadSfxs[Random.Range(0, currentWeapon.reloadSfxs.Length)]);
    }

    #endregion

    #region Weapon Selection

    [ClientCallback]
    public void WeaponSelection(InputAction.CallbackContext context)
    {
        if (!hasAuthority) return;

        if (context.performed)
        {
            reloading = false;
            switchReloadInterruption = true;

            int hotkeyNum = int.Parse(context.control.name) - 1;
            if (hotkeyNum < projectileWeapons.Length)
            {
                spraying = false;

                CmdSwitchWeapon(hotkeyNum);
            }
        }
    }

    #endregion

    private void UpdateAmmoText()
    {
        PlayerUI.instance.SetInventoryString(currentWeapon.name + " "
            + currentAmmoCounts[currentWeaponIndex]
            + "/" + currentWeapon.magazineSize
            + "\n" + playerInventory.AmmoCountToString());
    }


    /* [Header("Setting")]
     [SerializeField] private bool unlimitedAmmo;

     [Header("Required Components")]
     [SerializeField] private Transform arm;
     [SerializeField] private Transform hand;
     [SerializeField] private LayerMask projectileBlockLayer;
     [SerializeField] private GameObject normalProjectileGO;
     [SerializeField] private ProjectileWeapon[] projectileWeapons;

     [Header("Debugs")]
     [SerializeField] [SyncVar]
     private int currentWeaponIndex;
     private ProjectileWeapon currentWeapon;
     private PlayerInventory playerInventory;
     private Animator animator;
     private AudioSource audioSource;
     private ParticleSystem normalProjectile;
     private ParticleSystem.MainModule normalMain;

     private bool spraying;
     private float nextPrimaryShootable;
     private bool reloading;
     private bool controllable = true;

     private readonly SyncList<int> currentAmmoCounts = new SyncList<int>();

     private void Awake()
     {
         playerInventory = GetComponent<PlayerInventory>();
         animator = GetComponent<Animator>();
         audioSource = GetComponent<AudioSource>();
         normalProjectile = normalProjectileGO.GetComponent<ParticleSystem>();
         normalMain = normalProjectile.main;

         currentWeaponIndex = 0;
         currentWeapon = projectileWeapons[0];
     }

     private void Start()
     {

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
                 spraying = false;

                 currentWeaponIndex = hotkeyNum;
                 currentWeapon = projectileWeapons[hotkeyNum];

                 normalMain.startLifetime = (currentWeapon.range + 1) / normalMain.startSpeed.constant;

                 CmdSwitchWeapon(hotkeyNum);
             }

             reloading = false;
         }
     }

     [Command]
     private void CmdSwitchWeapon(int weaponSlot)
     {
         currentWeaponIndex = weaponSlot;
         currentWeapon = projectileWeapons[weaponSlot];

         normalMain.startLifetime = (currentWeapon.range + 1) / normalMain.startSpeed.constant;

         RpcSwitchWeapon();
     }

     [ClientRpc]
     private void RpcSwitchWeapon()
     {
         audioSource.PlayOneShot(currentWeapon.selectVfxs[Random.Range(0, currentWeapon.selectVfxs.Length)]);
     }

     #endregion

     #region Primary Attack

     [Client]
     public void PrimaryPerformed()
     {
         if (!controllable) return;

         if (currentWeapon.automatic)
         {
             spraying = true;
             StartCoroutine(AutomaticShoot());
         }
         else
         {
             if (Time.time < nextPrimaryShootable) return;
             nextPrimaryShootable = Time.time + currentWeapon.primaryCdr;

             if(PrimaryAmmoCheck()) 
                 ShootProjectiles();
         }
     }

     [Client]
     public void PrimaryReleased()
     {
         spraying = false;
     }

     [Client]
     private bool PrimaryAmmoCheck()
     {
         if (reloading)
         {
             CmdAmmoCheck();
             return false;
         }

         if (currentAmmoCounts[currentWeaponIndex] <= 0)
         {
             StartCoroutine(ReloadCoroutine());
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
     IEnumerator AutomaticShoot()
     {
         while (spraying && PrimaryAmmoCheck())
         {
             ShootProjectiles();
             yield return new WaitForSeconds(currentWeapon.primaryCdr);
         }
     }

     [Command]
     private void CmdAmmoCheck()
     {
         RpcAmmoCheck();
     }

     [ClientRpc]
     private void RpcAmmoCheck()
     {
         audioSource.PlayOneShot(currentWeapon.dryVfxs[Random.Range(0, currentWeapon.dryVfxs.Length)]);
     }

     #endregion

     #region Reload

     [Client]
     public void ReloadPerformed()
     {
         if (reloading) return;

         StartCoroutine(ReloadCoroutine());
     }

     [Client]
     IEnumerator ReloadCoroutine()
     {
         reloading = true;
         CmdReload();

         yield return new WaitForSeconds(currentWeapon.reloadTime);

         if (unlimitedAmmo)
             CmdCheatReload();
         else
             playerInventory.CmdSpendAmmo(currentWeapon.ammoType, currentWeapon.magazineSize);

         reloading = false;
     }

     [Command]
     private void CmdCheatReload()
     {
         ReloadFromInventory(currentWeapon.magazineSize);
     }

     [Server]
     public void ReloadFromInventory(int amount)
     {
         currentAmmoCounts[currentWeaponIndex] = amount;
     }

     [Command]
     private void CmdReload()
     {
         RpcReload();
     }

     [ClientRpc]
     private void RpcReload()
     {
         audioSource.PlayOneShot(currentWeapon.reloadVfxs[Random.Range(0, currentWeapon.reloadVfxs.Length)]);
     }

     #endregion

     #region Projectile Shooting Mechanisim

     [SerializeField] Vector2 hitPointCache;
     [SerializeField] RaycastHit2D[] hitsCache;

     [Client]
     private void ShootProjectiles()
     {
         if (!controllable) return;

         // Character VFX
         animator.SetTrigger("shootTrigger");

         for (int i = 0; i < currentWeapon.pelletPerShot; i++)
         {
             Quaternion spreadRot = Quaternion.Euler(0, 0, Random.Range(-currentWeapon.spread,
                 currentWeapon.spread));

             CmdShootProjectile(spreadRot);

             RaycastHit2D[] hits = Physics2D.RaycastAll(hand.position, spreadRot * hand.right,
                 currentWeapon.range);
             hitsCache = hits;

             if (hits.Length <= 0) continue;

              // hit nothing in range
             hitPointCache = hits[0].transform.position;

             foreach (RaycastHit2D hit in hits)
             {
                 if (hit.collider.isTrigger)
                     continue;

                 if (hit.collider.TryGetComponent<EntityStatus>(out EntityStatus entityStatus))
                 {
                     HostilityType hitHostility = entityStatus.GetHostility();
                     if (hitHostility == HostilityType.Hostile ||
                         hitHostility == HostilityType.Neutral)
                     {
                         entityStatus.CmdDealDamage(currentWeapon.damage, netIdentity);
                         if (currentWeapon.ammoType != AmmoType.Penetration)
                             break; 
                     }
                 }
                 else
                 {
                     break; // player hit a wall before hitting an enemy
                 }
             }
         }
     }

     #endregion

     #region Projectile SFX and VFX

     [Command]
     private void CmdShootProjectile(Quaternion spreadRot)
     {
         RpcShootProjectile(hand.rotation * spreadRot);
     }

     [ClientRpc]
     private void RpcShootProjectile(Quaternion spreadRot)
     {
         normalProjectileGO.transform.rotation = spreadRot;
         normalProjectile.Play();

         audioSource.PlayOneShot(currentWeapon.primaryVfxs[Random.Range(0, currentWeapon.primaryVfxs.Length)]);
     }

     #endregion

     #region Audio

     [ClientRpc]
     private void PlaySFX()
     {

     }

     #endregion

     [Server]
     public void SetControllable(bool controllable)
     {
         RpcSetControllable(controllable);
     }

     [ClientRpc]
     private void RpcSetControllable(bool controllable)
     {
         if (!hasAuthority) return;
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
     }*/
}







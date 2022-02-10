using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EntityWeaponSystem : NetworkBehaviour
{
    [Header("Entity Required Components")]
    [SerializeField] protected Transform azimuth;
    [SerializeField] protected Transform muzzle;
    [SerializeField] protected AudioSource sfxAudioSource;
    [SerializeField] protected LayerMask projectileBlockLayer;
    [SerializeField] protected GameObject[] projectileSystems;        // Must match AmmoTypes enum position
    [SerializeField] protected ProjectileWeapon[] projectileWeapons;  // Must match with weapon slot number
    
    [SyncVar(hook = nameof(HandleCurrentWeaponIndexChange))]
    private int _currentWeaponIndex = 0;
    protected int currentWeaponIndex { get { return _currentWeaponIndex; } }

    [SyncVar]
    private bool _controllable = true;
    protected bool controllable { get { return _controllable; } }

    
    protected Animator animator;
    protected ParticleSystem[] projectileParticles;

    protected ProjectileWeapon currentWeapon { get; private set; }
    protected float arcPerPellet;
    private float currentSpreadArc = 0;
    
    protected bool reloading = false;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();

        projectileParticles = new ParticleSystem[projectileSystems.Length];
        for(int i = 0; i < projectileSystems.Length; i++)
            projectileParticles[i] = projectileSystems[i].GetComponent<ParticleSystem>();

        InitializeWeapon(0);
    }

    #region Switch Weapon

    private void HandleCurrentWeaponIndexChange(int oldIndex, int newIndex)
    {
        InitializeWeapon(newIndex);
        sfxAudioSource.PlayOneShot(currentWeapon.selectSfxs[Random.Range(0, currentWeapon.selectSfxs.Length)]);
    }

    private void InitializeWeapon(int weaponIndex)
    {
        currentWeapon = projectileWeapons[weaponIndex];
        arcPerPellet = currentWeapon.pelletPerShot == 1 ? 
            0 : 2 * currentWeapon.spreadArc / currentWeapon.pelletPerShot;
        var ammoTypeIndex = (int)currentWeapon.ammoType;

        var projectileMain = projectileParticles[ammoTypeIndex].main;
        projectileMain.startLifetime =
            (currentWeapon.range) / projectileMain.startSpeed.constant;

        var projectileEmission = projectileParticles[ammoTypeIndex].emission;
        projectileEmission.SetBurst(0, new ParticleSystem.Burst(0, currentWeapon.pelletPerShot));

        var projectileShape = projectileParticles[ammoTypeIndex].shape;
        projectileShape.arc = currentWeapon.spreadArc;
        projectileShape.rotation = new Vector3(0, 0, -currentWeapon.spreadArc / 2);
    }

    [Command]
    protected void CmdSwitchWeapon(int weaponIndex)
    {
        _currentWeaponIndex = weaponIndex;
    }

    [Command]
    protected void CmdPlaySelectSFXs()
    {
        RpcPlaySelectSFXs();
    }

    [ClientRpc]
    protected void RpcPlaySelectSFXs()
    {
        sfxAudioSource.PlayOneShot(currentWeapon.selectSfxs[Random.Range(0, currentWeapon.selectSfxs.Length)]);
    }

    #endregion

    #region ShootProjectiles

    [Client]
    protected void ClientShootProjectiles()
    {
        ShootProjectiles();
    }

    [Server]
    protected void ServerShootProjectiles()
    {
        ShootProjectiles();
    }

    List<RaycastHit2D> hitsArrayCache = new List<RaycastHit2D>();

    private void ShootProjectiles()
    {
        if (!controllable) return;

        // SFX
        if (isServer)
            RpcShootProjectilesSFX();
        else if (isClient)
            CmdShootProjectilesSFX();
        // Animation
        animator.SetTrigger("shootTrigger");

        hitsArrayCache.Clear(); // TODO: remove when finish debugging

        for (int i = 0; i < currentWeapon.pelletPerShot; i++)
        {
            if (isServer)
                RpcShootProjectilesVFX();
            else if (isClient)
                CmdShootProjectilesVFX();

            if (currentWeapon.pelletPerShot == 1)
                currentSpreadArc = 0;
            else
                currentSpreadArc = i == 0 ? -currentWeapon.spreadArc : currentSpreadArc + arcPerPellet;

            Quaternion spreadRot = Quaternion.Euler(0, 0, currentSpreadArc); // cache?

            RaycastHit2D[] hits = Physics2D.CircleCastAll(muzzle.position, currentWeapon.pelletCaliber, spreadRot * muzzle.right,
                currentWeapon.range); // cache?

            foreach (RaycastHit2D hitCache in hits)
                hitsArrayCache.Add(hitCache);

            if (hits.Length <= 0) continue;

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
                        if (isServer)
                            entityStatus.RecieveDamage(currentWeapon.damage, netIdentity);
                        else if (isClient)
                            entityStatus.CmdRecieveDamage(currentWeapon.damage, netIdentity);

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

    #region VFX and SFX

    [Command]
    protected void CmdShootProjectilesVFX()
    {
        RpcShootProjectilesVFX();
    }

    [ClientRpc]
    protected void RpcShootProjectilesVFX()
    {
        projectileParticles[(int)currentWeapon.ammoType].Play();
    }

    [Command]
    protected void CmdShootProjectilesSFX()
    {
        RpcShootProjectilesSFX();
    }

    [ClientRpc]
    protected void RpcShootProjectilesSFX()
    {
        sfxAudioSource.PlayOneShot(currentWeapon.primarySfxs[Random.Range(0, currentWeapon.primarySfxs.Length)]);
    }

    #endregion

    [Server]
    public void SetControllable(bool newControllable)
    {
        _controllable = newControllable;
    }

    private void OnDrawGizmos()
    {
        if (hitsArrayCache != null)
        {
            Gizmos.color = Color.blue;
            foreach(RaycastHit2D hit in hitsArrayCache)
                Gizmos.DrawLine(muzzle.position, hit.point);
        }
    }
}

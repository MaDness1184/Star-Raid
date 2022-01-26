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

    [SerializeField] private bool canShootAgain;
    [SerializeField] private float nextPrimaryShootable;

    private void Start()
    {
        currentWeapon = projectileWeapons[0];
    }

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

    [ClientCallback]
    public void PrimaryPerformed()
    {
        canShootAgain = false;
        if (currentWeapon.automatic)
        {
            StartCoroutine(AutomaticShoot());
        }
        else
        {
            if (Time.time < nextPrimaryShootable) return;
            nextPrimaryShootable = Time.time + currentWeapon.primaryCdr;

            ShootOneProjectile();
        }
    }

    IEnumerator AutomaticShoot()
    {
        while (!canShootAgain)
        {
            ShootOneProjectile();
            yield return new WaitForSeconds(currentWeapon.primaryCdr);
        }
    }


    [ClientCallBack]
    public void PrimaryReleased()
    {
        canShootAgain = true;
    }

    private void ShootOneProjectile()
    {
        //Raycast
        RaycastHit2D hit = Physics2D.Raycast(hand.position, hand.right, 30f);

        if (hit)
        {
            //BulletTrailVfx(hit.point);
            if (hit.collider.TryGetComponent<EntityStatus>(out EntityStatus entityStatus))
            {
                entityStatus.CmdDealDamage(currentWeapon.damage);
            }
        }
        else
        {
            //BulletTrailVfx(transform.position + transform.right * maxRange);
        }

        CmdBulletVFX();
    }

    [Command]
    private void CmdBulletVFX()
    {
        RpcBulletVFX();
    }

    [ClientRpc]
    private void RpcBulletVFX()
    {
        Instantiate(currentWeapon.bulletVfx, hand.position, arm.rotation);
    }

}

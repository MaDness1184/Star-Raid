using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponSystem : NetworkBehaviour
{
    [Header("Required Components")]
    [SerializeField] private Transform arm;
    [SerializeField] private Transform hand;
    [SerializeField] private GameObject bulletVFX;

    [ClientCallback]
    public void PrimaryPerformed()
    {
        //Raycast
        RaycastHit2D hit = Physics2D.Raycast(hand.position, hand.right, 30f);

        if (hit)
        {
            //BulletTrailVfx(hit.point);
            if (hit.collider.TryGetComponent<EntityStatus>(out EntityStatus entityStatus))
            {
                entityStatus.CmdDealDamage(1);
            }
        }
        else
        {
            //BulletTrailVfx(transform.position + transform.right * maxRange);
        }

        CmdBulletVFX();
    }

    [ClientCallBack]
    public void PrimaryReleased()
    {

    }

    [Command]
    private void CmdBulletVFX()
    {
        RpcBulletVFX();
    }

    [ClientRpc]
    private void RpcBulletVFX()
    {
        Instantiate(bulletVFX, hand.position, arm.rotation);
    }

}

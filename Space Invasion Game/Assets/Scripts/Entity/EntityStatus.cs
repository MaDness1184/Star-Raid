using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStatus : NetworkBehaviour
{
    [Command(requiresAuthority = false)]
    public void CmdDealDamage(int damage) 
    {
        DealDamage(damage);
    }

    [Server]
    protected virtual void DealDamage(int damage)
    {

    }

    public void GetCurrentHP()
    {

    }

}

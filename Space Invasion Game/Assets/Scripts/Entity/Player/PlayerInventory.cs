using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : EntityInventory
{

    [Header("Settings")]
    [SerializeField] private AmmoCount[] AmmoCounts;

    private readonly SyncDictionary<AmmoType, int> internalAmmoCounts = new SyncDictionary<AmmoType, int>();

    private PlayerWeaponSystem playerWeaponSystem;

    private void Start()
    {
        playerWeaponSystem = GetComponent<PlayerWeaponSystem>();

        if(isServer)
        {
            for(int i = 0; i < AmmoCounts.Length; i++)
            {
                internalAmmoCounts.Add(AmmoCounts[i].ammoType, AmmoCounts[i].count);
            }
        }
    }

    [Command]
    public void CmdSpendAmmo(AmmoType ammoType, int amount)
    {
        int ammoTypeCount = internalAmmoCounts[ammoType];
        if (ammoTypeCount > amount) //Reload success
        {
            playerWeaponSystem.RpcReload(amount);
            internalAmmoCounts[ammoType] -= amount;
        }
        else if (ammoTypeCount > 0 && ammoTypeCount < amount) //Partially success
        {
            playerWeaponSystem.RpcReload(ammoTypeCount);
            internalAmmoCounts[ammoType] = 0;
        }
        else //Unsuccessfull
        {
            //Play dry SFX and VFX
        }

        //Debug.Log(ammoType.ToString() + " count = " + internalAmmoCounts[ammoType]);
    }

    [Serializable]
    public struct AmmoCount
    {
        public AmmoType ammoType;
        public int count;
    }

    public string AmmoCountToString()
    {
        string result = "";
        foreach(KeyValuePair<AmmoType,int> keyValuePair in internalAmmoCounts)
        {
            result +=     keyValuePair.Key.ToString().Substring(0,2) 
                + ":" + keyValuePair.Value + " | ";
        }
        return result.Substring(0, result.Length - 3);
    }
}

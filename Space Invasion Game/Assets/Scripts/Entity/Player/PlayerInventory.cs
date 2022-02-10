using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerWeaponSystem))]
public class PlayerInventory : EntityInventory
{
    [Header("Player Specific Setting")]
    [SerializeField] private bool unlimitedAmmo;
    [SerializeField] private AmmoCount[] AmmoCounts;

    private PlayerWeaponSystem playerWeaponSystem;

    private readonly SyncDictionary<AmmoType, int> internalAmmoCounts = new SyncDictionary<AmmoType, int>();

    private void Awake()
    {
        playerWeaponSystem = GetComponent<PlayerWeaponSystem>();
    }

    private void Start()
    {
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
            playerWeaponSystem.ReloadFromInventory(amount);
            if(!unlimitedAmmo) internalAmmoCounts[ammoType] -= amount;
        }
        else if (ammoTypeCount > 0 && ammoTypeCount < amount) //Partially success
        {
            playerWeaponSystem.ReloadFromInventory(ammoTypeCount);
            if (!unlimitedAmmo) internalAmmoCounts[ammoType] = 0;
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

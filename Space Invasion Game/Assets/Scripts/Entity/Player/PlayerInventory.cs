using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AmmoCount
{
    public AmmoType ammoType;
    public int count;
}


[RequireComponent(typeof(PlayerWeaponSystem))]
public class PlayerInventory : EntityInventory
{
    [Header("Player Specific Setting")]
    [SerializeField] private bool unlimitedAmmo;
    [SerializeField] private AmmoCount[] AmmoCounts;
    [SerializeField] private List<Item> fakeInventory;

    [Header("Item Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask itemLayerMask;

    private PlayerWeaponSystem playerWeaponSystem;

    private readonly SyncDictionary<AmmoType, int> internalAmmoCounts = new SyncDictionary<AmmoType, int>();

    private readonly SyncDictionary<Item, int> internalInventory = new SyncDictionary<Item, int>();

    //[Header("Debugs")]
    

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

            foreach(Item item in fakeInventory)
            {
                internalInventory.Add(item, UnityEngine.Random.Range(20,99));
            }
        }

        if (isLocalPlayer)
        {
            CraftingUI.instance.SetPlayerInventory(this);
            CraftingUI.instance.NotifyInventoryUpdate(ConvertSyncDictionaryToDictionary(internalInventory));
        }  
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRange, itemLayerMask);

        if (colliders.Length <= 0) return;

        foreach(Collider2D collider in colliders)
        {
            if(collider.TryGetComponent<NetworkIdentity>(out NetworkIdentity itemIdentity))
            {
                if (itemIdentity.connectionToClient == null)
                {
                    itemIdentity.AssignClientAuthority(connectionToClient);
                }
            }
        }
    }

    [Server]
    public bool AddItem(Item itemToAdd)
    {
        if (internalInventory.ContainsKey(itemToAdd))
        {
            // TODO: add if item is full check
            internalInventory[itemToAdd]++;
        }
        else
        {
            internalInventory.Add(itemToAdd, 1);
        }

        if(CraftingUI.instance.isVisible)
            CraftingUI.instance.NotifyInventoryUpdate(ConvertSyncDictionaryToDictionary(internalInventory));

        return true;
    }

    #region Crafting

    [Server]
    public void Craft(Item item)
    {
        int requirementMet = 0;

        for(int i = 0; i < item.requiredMaterials.Count; i++)
        {
            if (!internalInventory.ContainsKey(item.requiredMaterials[i].item)) continue;

            if (internalInventory[item.requiredMaterials[i].item] >= item.requiredMaterials[i].count)
                requirementMet++;
        }

        if (requirementMet != item.requiredMaterials.Count) return;

        foreach(ItemCount reqMat in item.requiredMaterials)
        {
            internalInventory[reqMat.item] -= reqMat.count;
        }

        if(item.prefab == null)
        {
            foreach (ItemCount resultCom in item.resultComponents)
            {
                if (internalInventory.ContainsKey(resultCom.item))
                    internalInventory[resultCom.item] += resultCom.count;
                else
                    internalInventory.Add(resultCom.item, resultCom.count);
            }
        }
        else
        {
            var itemPrefab = Instantiate(item.prefab, 
                new Vector3(Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)), Quaternion.identity);

            NetworkServer.Spawn(itemPrefab);
        }

        CraftingUI.instance.NotifyInventoryUpdate(ConvertSyncDictionaryToDictionary(internalInventory));
    }

    #endregion

    #region Ammo

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

    #endregion

    public string AmmoCountToString()
    {
        string result = "";
        foreach(KeyValuePair<AmmoType,int> keyValuePair in internalAmmoCounts)
        {
            result +=     keyValuePair.Key.ToString().Substring(0,3) 
                + ":" + keyValuePair.Value + " | ";
        }
        return result.Substring(0, result.Length - 3 < 0 ? 0 : result.Length - 3);
    }

    public string ItemCountToString()
    {
        string result = "";
        foreach (KeyValuePair<Item, int> keyValuePair in internalInventory)
        {
            result += keyValuePair.Key.ToString().Substring(0, 3)
                + ":" + keyValuePair.Value + " | ";
        }
        return result.Substring(0, result.Length - 3 < 0 ? 0 : result.Length - 3);
    }

    Dictionary<Item, int> cacheDictionary = new Dictionary<Item, int>();
    private Dictionary<Item, int> ConvertSyncDictionaryToDictionary(SyncDictionary<Item, int> syncDictionary)
    {
        cacheDictionary.Clear();
        foreach (var kvp in syncDictionary)
            cacheDictionary.Add(kvp.Key, kvp.Value);

        return cacheDictionary;
    }

    [Client]
    public void CraftingUIRequestInventoryUpdate()
    {
        CraftingUI.instance.NotifyInventoryUpdate(ConvertSyncDictionaryToDictionary(internalInventory));
    }
}

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneItemReplica : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float inventoryRange = 0.25f;      // Actual pickup range
    [SerializeField] private float pickupRange = 4f;            // This should always be larger than actual player's pickup range
    [SerializeField] private float maxPickupTravel = 4f;                 
    [SerializeField] private float maxPickupSpeed = 10f;
    [SerializeField] private float pickupCdr = 2f;

    [Header("Debugs")]
    [SyncVar(hook = nameof(HandleCurrentItemChange))]
    [SerializeField] private Item currentItem;

    private SpriteRenderer spriteRenderer;

    private float maxPickupTravelSqr;
    private float pickupRangeSqr;
    private float distanceSqrCache;
    private float inventoryRangeSqr;
    private Vector3 directionCache;
    private Vector3 playerPositionCache;

    private float nextPickup;
    private Vector3 startPosition;
    private Vector3 velocity = Vector3.zero;

    private void HandleCurrentItemChange(Item oldName, Item newName)
    {
        UpdateCurrentItem();
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        maxPickupTravelSqr = maxPickupTravel * maxPickupTravel;
        pickupRangeSqr = pickupRange * pickupRange;
        inventoryRangeSqr = inventoryRange * inventoryRange;

        startPosition = transform.position;
    }

    private void Start()
    {
        UpdateCurrentItem();
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (netIdentity.connectionToClient == null) return;

        if (Time.time < nextPickup) return;

        playerPositionCache = netIdentity.connectionToClient.identity.transform.position;
        distanceSqrCache = (playerPositionCache - transform.position).sqrMagnitude;

        if(distanceSqrCache > pickupRangeSqr)
        {
            //DebugConsole.Log(currentItem.name + " removed client authority");
            netIdentity.RemoveClientAuthority();
        }
        else if(distanceSqrCache < inventoryRangeSqr)
        {
            //transform.position = Vector3.SmoothDamp(transform.position, playerPositionCache, ref velocity, 0.1f);

            if (netIdentity.connectionToClient.identity.TryGetComponent<PlayerInventory>(out PlayerInventory playerInventory))
            {
                if (playerInventory.AddItem(currentItem))
                {
                    //DebugConsole.Log(netIdentity.connectionToClient.identity.name + " picked up " + currentItem.name);
                    NetworkServer.Destroy(gameObject);
                }
                else
                {
                    //DebugConsole.Log(netIdentity.connectionToClient.identity.name + " unable to pick up " + currentItem.name);
                    nextPickup = Time.time + pickupCdr;
                }
            }
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, playerPositionCache,ref velocity, 0.1f, maxPickupSpeed);
        }

        if((transform.position - startPosition).sqrMagnitude > maxPickupTravelSqr)
        {
            startPosition = transform.position;
            nextPickup = Time.time + pickupCdr;
        }
    }

    private void UpdateCurrentItem()
    {
        name = currentItem.name + $" id = {netIdentity.sceneId}";
        spriteRenderer.sprite = currentItem.sprite;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetCurrentItemName(Item newItem)
    {
        SetCurrentItem(newItem);
    }

    [Server]
    public void SetCurrentItem(Item newItem)
    {
        currentItem = newItem;
    }
}

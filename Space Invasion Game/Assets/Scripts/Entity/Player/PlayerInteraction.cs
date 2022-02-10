using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : EntityInteraction
{
    [Header("Scan Setting")]
    [SerializeField] private float interactionRange = 1f;
    [SerializeField] private float placeableRange = 4f;
    [SerializeField] private float scanCdr = 0.1f;
    [SerializeField] private GameObject replicaObject;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask interactBlockLayer;
    [SerializeField] private LayerMask validPointBlockLayer;
    [SerializeField] private LayerMask validRayBlockLayer;

    [Header("Interaction Setting")]
    [SerializeField] private float pickupDistance = 1f;
    [SerializeField] private Transform holdingSpot;
    [SerializeField] private float rotationRate = 1f;

    [Header("Debugging")]
    [SerializeField] private float sqrDistanceScan;
    [SerializeField] private float closetSqrDistance = 99f;
    [SerializeField] private Collider2D closetCollider;
    [SerializeField] private Collider2D currentCollider;
    [SerializeField] private Interactable currentInteractable;
    [SerializeField] private bool currentColliderFound = false;
    [SerializeField] private Collider2D[] cachedColliders; // TODO: Remove when finish debugging

    private bool highlightValid;
    [SerializeField] private float sqrPlaceableRange;
    [SerializeField] private float mouseSqDistance;
    private Vector3 cachedHit; // TODO: Remove when finish debugging
    private Vector3 holdingSpotDirection;
    [SerializeField] private Vector3 mousePosition;
    private bool itemPickedUp = false;
    private Interactable cachedServerInteractable;
    private SpriteRenderer replicaSpriteRenderer;
    private InputAction inputAction;
    private Animator animator;

    Vector3 cachedPosition; //TODO cache position to maximize efficiency
    private float nextScan;
    private float nextValid;
    

    private void Awake()
    {
        sqrPlaceableRange = placeableRange * placeableRange;
        holdingSpotDirection = holdingSpot.position - transform.position;
        replicaSpriteRenderer = replicaObject.GetComponent<SpriteRenderer>();
        inputAction = inputAction = GetComponent<PlayerInput>().actions["aim"];
        animator = GetComponent<Animator>();
    }

    [ClientCallback]
    private void Update()
    {
        if (!hasAuthority) return;

        MoveGridHighlight();
    }

    [ClientCallback]
    private void FixedUpdate()
    {
        if (!hasAuthority) return;

        ScanClosetInteractable();
    }

    [SerializeField] Collider2D hitCache;
    [SerializeField] bool highlightValidCache;

    private void MoveGridHighlight()
    {
        if (Time.time < nextValid) return;
        nextValid = Time.time + scanCdr;

        mousePosition = Camera.main.ScreenToWorldPoint(inputAction.ReadValue<Vector2>());
        var mouseDirection = (Vector2)mousePosition - (Vector2)transform.position;
        mouseSqDistance = mouseDirection.sqrMagnitude;

        RaycastHit2D rayHit = Physics2D.Raycast(transform.position, mouseDirection, mouseDirection.magnitude, validRayBlockLayer);
        Collider2D pointHit = Physics2D.OverlapPoint(mousePosition, validPointBlockLayer);
        //hitCache = hit;
        highlightValid = !rayHit && !pointHit && mouseSqDistance <= sqrPlaceableRange;
        highlightValidCache = highlightValid;

        GridHighlight.main.SetValid(highlightValid);
        GridHighlight.main.MoveTo(mousePosition);
    }

    #region Interaction

    [ClientCallback]
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!hasAuthority) return;

        if (context.performed)
        {
            if (!itemPickedUp)
                PickupInteractable();
            else
                PutdownInteractable();
        } 
    }

    [Client]
    private void PutdownInteractable()
    {
        if (!highlightValid) return;
        GridHighlight.main.SetShow(false);

        replicaSpriteRenderer.sprite = null;
        
        CmdPutdownInteractable(mousePosition);

        animator.Play("Put Down"); // PutDown Animation Trigger
        animator.SetBool("isAiming", true);

        itemPickedUp = false;
    }

    [Command]
    private void CmdPutdownInteractable(Vector3 newPosition)
    {

        if (cachedServerInteractable.TryGetComponent<TurretStatus>(out TurretStatus turret))
        {
            turret.Dropdown(new Vector3(Mathf.RoundToInt(newPosition.x),
                Mathf.RoundToInt(newPosition.y)));
        }

        replicaSpriteRenderer.sprite = null;
        cachedServerInteractable = null;
    }

    [Client]
    private void PickupInteractable()
    {
        if (!currentInteractable) return;
        GridHighlight.main.SetShow(true);

        replicaSpriteRenderer.sprite = currentInteractable.GetPickupSprite();
        replicaObject.transform.position = currentInteractable.transform.position;

        animator.SetBool("isAiming", false);
        animator.Play("Pick Up"); // PickUp Animation Trigger

        AnimateReplicaObject();
        CmdPickupInteractable(currentInteractable.GetComponent<NetworkIdentity>().netId);

        itemPickedUp = true;
    }

    [Command]
    private void CmdPickupInteractable(uint replicaNetId)
    {
        cachedServerInteractable = GetServerCurrentInteractable(replicaNetId);

        replicaSpriteRenderer.sprite = cachedServerInteractable.GetPickupSprite();

        if (cachedServerInteractable.TryGetComponent<TurretStatus>(out TurretStatus turret))
            turret.Pickup();
    }

    #endregion

    #region Animate ReplicaObject

    [Client]
    private void AnimateReplicaObject()
    {
        StartCoroutine(AnimateReplicaObjectCoroutine());
    }
    [SerializeField] float currentAngle;

    [Client]
    IEnumerator AnimateReplicaObjectCoroutine()
    {
        Vector3 replicaDirection = replicaObject.transform.position - transform.position;
        currentAngle = Vector3.Angle(holdingSpotDirection, replicaDirection);
        //if (currentAngle < 90)
        bool clockwise = replicaObject.transform.localPosition.x < 0;
        if (clockwise)
            currentAngle = 360 - currentAngle;

        float currentRadAngle;
        Vector2 offset;

        while (currentAngle > 0 && currentAngle < 360)
        {
            currentAngle += (clockwise ? 1 : -1) * rotationRate * Time.deltaTime;
            currentRadAngle = Mathf.Deg2Rad * currentAngle;
            offset = new Vector2(Mathf.Sin(currentRadAngle), Mathf.Cos(currentRadAngle)) * pickupDistance;
            replicaObject.transform.localPosition = Vector3.Lerp(replicaObject.transform.localPosition, offset, 0.1f);

            yield return new WaitForFixedUpdate();
        }

        while(replicaObject.transform.position != holdingSpot.transform.position)
        {
            replicaObject.transform.position = Vector3.Lerp(replicaObject.transform.position, holdingSpot.transform.position, 0.1f);
            yield return new WaitForFixedUpdate();
        }
    }

    [Server]
    private Interactable GetServerCurrentInteractable(uint replicaNetId)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactableLayer);

        if (colliders.Length <= 0)
        {
            DebugConsole.LogWarning("Cannot find currentInteractable on server side. Something is terribly wrong");
            return null;
        }

        foreach (Collider2D collider in colliders)
        {
            if (collider.TryGetComponent<NetworkIdentity>(out NetworkIdentity targetIdentity))
            {
                if (targetIdentity.netId == replicaNetId)
                    return targetIdentity.GetComponent<Interactable>();
            }
        }

        DebugConsole.LogWarning("Cannot find currentInteractable on server side. Something went terribly wrong");
        return null;
    }

    #endregion

    #region InteractionScan

    [Client]
    private void ScanClosetInteractable()
    {
        if (itemPickedUp) return;

        if (Time.time < nextScan) return;
        nextScan = Time.time + scanCdr;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactableLayer);

        cachedColliders = colliders;
        if (colliders.Length <= 0)
        {
            ClearClosetInteractable();
            return;
        }

        closetSqrDistance = 99f;
        closetCollider = null;

        foreach (Collider2D collider in colliders)
        {
            if (collider == closetCollider)
                currentColliderFound = true;

            sqrDistanceScan = (transform.position - collider.transform.position).sqrMagnitude;

            if (sqrDistanceScan > closetSqrDistance) continue;

            closetSqrDistance = sqrDistanceScan;
            closetCollider = collider;
        }

        if (currentCollider == closetCollider) return;

        if (closetCollider.TryGetComponent<Interactable>(out Interactable interactable))
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position,
                closetCollider.transform.position - transform.position, interactionRange, interactBlockLayer);

            if (hit.collider != closetCollider) return;

            currentColliderFound = true;
            currentCollider = closetCollider;

            currentInteractable?.LocalHighlight(false);
            currentInteractable = interactable;
            currentInteractable.LocalHighlight(true);
        }

        if (!currentColliderFound)
            ClearClosetInteractable();
        else
            currentColliderFound = false;
    }

    [Client]
    private void ClearClosetInteractable()
    {
        if (itemPickedUp) return;

        currentInteractable?.LocalHighlight(false);
        currentInteractable = null;
        currentCollider = null;
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, placeableRange);
        Gizmos.DrawLine(transform.position, cachedHit);
    }
}

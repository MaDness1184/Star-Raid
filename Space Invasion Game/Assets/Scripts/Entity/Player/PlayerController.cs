using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : EntityController
{
    [Header("Player Specific Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Player Required Components")]
    [SerializeField] private Transform azimuth;
    [SerializeField] private Transform muzzle;

    private bool controllable = true;
    private Vector3 mousePosition;
    private Vector2 lookDirection;
    private float armRotZ;

    Rigidbody2D rb2D;
    Animator animator;
    InputAction inputAction;

    Vector2 movement;
    private void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inputAction = GetComponent<PlayerInput>().actions["aim"];
    }

    [ClientCallback]
    private void Update()
    {
        if (!hasAuthority || !controllable || !NetworkClient.ready) return;

        Aim();
        UpdateAnimationDirection();
    }

    Vector3 cachedPosition;

    private void FixedUpdate()
    {
        if (transform.position != cachedPosition)
            PlayMovementSFXs();
        else
            StopMovementSFXs();

        cachedPosition = transform.position;

        if (!isClient) return;

        if (!hasAuthority || !controllable || !NetworkClient.ready) return;

        rb2D.MovePosition(rb2D.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    #region Movement

    [ClientCallback]
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!hasAuthority || !controllable) return;

        if (context.performed)
        {
            movement = context.ReadValue<Vector2>();
            animator.SetBool("isMoving", true);
        }
        else if (context.canceled)
        {
            movement = Vector2.zero;
            animator.SetBool("isMoving", false);
        }
    }

    #endregion

    #region Aim

    [Client]
    private void Aim()
    {
        //if (!status.canLook) return;
        mousePosition = Camera.main.ScreenToWorldPoint(inputAction.ReadValue<Vector2>());
        lookDirection = mousePosition - new Vector3(azimuth.position.x, azimuth.position.y);
        lookDirection.Normalize();

        // Calculate angel
        armRotZ = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        azimuth.rotation = Quaternion.Euler(0f, 0f, armRotZ);
    }

    [Client]
    private void UpdateAnimationDirection()
    {
        animator.SetFloat("directionX", lookDirection.x);
        animator.SetFloat("directionY", lookDirection.y);
    }

    #endregion

    #region Attack

    [ClientCallback]
    public void OnDebugConsole(InputAction.CallbackContext context)
    {
        if (!hasAuthority) return;

        if (context.performed)
            DebugConsole.main.ToggleConsole();
    }

    [ClientCallback]
    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (!hasAuthority) return;

        if (context.performed)
            DebugConsole.main.RunCommand();
    }

    #endregion

    #region Controllable

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

        if (controllable) return;
        movement = Vector2.zero;
        rb2D.velocity = Vector2.zero;
    }

    #endregion
}

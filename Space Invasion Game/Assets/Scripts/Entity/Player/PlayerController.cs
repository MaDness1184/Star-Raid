using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerWeaponSystem))]
public class PlayerController : EntityController
{
  

    [Header("Settings")]
    [SerializeField ]private float moveSpeed = 5f;

    [Header("Required Components")]
    [SerializeField] private Transform arm;
    [SerializeField] private Transform hand;
    [SerializeField] private GameObject bulletVFX;

    Rigidbody2D rb2D;
    Animator animator;
    PlayerWeaponSystem playerWeaponSystem;

    InputAction inputActions;

    Vector2 movement;

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerWeaponSystem = GetComponent<PlayerWeaponSystem>();

        var playerInput = GetComponent<PlayerInput>();
        inputActions = playerInput.actions["aim"];
    }

    #region Movement

    [ClientCallBack]
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.performed)
        {
            movement = context.ReadValue<Vector2>();        }
        else if (context.canceled)
        {
            movement = Vector2.zero;
        }
    }

    #endregion

    #region Aim

    [ClientCallback]
    private void Aim() // directional animation variables changed in here for now
    {
        //if (!status.canLook) return;
        var mousePosition = inputActions.ReadValue<Vector2>();
        Vector2 cursor = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector2 difference = cursor - new Vector2(arm.position.x, arm.position.y);
        difference.Normalize();
        UpdateAnimationDirection(difference);
        //lookDirection = difference;
        // Calculate angel
        float rotZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
        arm.rotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    [ClientCallBack]
    private void UpdateAnimationDirection(Vector2 newDirection)
    {
        animator.SetFloat("directionX", newDirection.x);
        animator.SetFloat("directionY", newDirection.y);
    }

    #endregion

    #region Attack

    [ClientCallBack]
    public void OnPrimaryAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.performed)
        {
            playerWeaponSystem.PrimaryPerformed();
        }
        else if (context.canceled)
        {
            //Debug.Log("Primary attack canceled");
        }
    }


    #endregion

    [ClientCallback]
    private void Update()
    {
        if (!isLocalPlayer || !NetworkClient.ready) return;

        Aim();
    }

    [ClientCallBack]
    private void FixedUpdate()
    {
        rb2D.MovePosition(rb2D.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}

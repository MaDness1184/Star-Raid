using Mirror;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : EntityController
{
    [Header("Physic Setting")]
    [SerializeField] private bool dummy = false;
    [SerializeField] private float moveSpeed = 300;
    [SerializeField] private float movementSmoothing = 0.1f;

    [Header("Attack Setting")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackDelayTime = 0.5f;
    //[SerializeField] private float attackCdr = 0.5f;
    [SerializeField] private float attackRecorveryTime = 1f;
    //[SerializeField] private float knockBackForce = 500f;
    [SerializeField] private GameObject attackVfx;
    [SerializeField] private GameObject attackSuccessVfx;
    //[SerializeField] private float attackRadius = 1.25f;
    //[SerializeField] private LayerMask attackableLayer;

    [Header("Pathfinding Setting")]
    [SerializeField] private float nextWaypointDistance = 3f;
    [SerializeField] private float pathUpdateCdr = 0.5f;
    //[SerializeField] private float obstacleScanDistance = 0.7f;
    //[SerializeField] private float playerScanCdr = 1f;

    [Header("Debugs")]
    [SerializeField] private Transform target;

    private Path path;
    private int currentWaypoint = 0;
    private bool reachedEndOfPath = false;
    private float nextPathUpdate;

    [SyncVar(hook = nameof(HandleAttackingChange))]
    [SerializeField] private bool attacking;
    [SerializeField] private EntityStatus targetStatus;
    private float nextAttackRecovery;
    private float nextDelayAttack;

    private Seeker seeker;
    private Rigidbody2D rbody;

    private Vector3 velocity = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        if (!isServer) return;
        seeker = GetComponent<Seeker>();
        rbody = GetComponent<Rigidbody2D>();
    }

    [ServerCallback]
    void Update()
    {
        if (dummy && target != null) return;

        UpdatePath();
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        //TODO: Add target hostility check
        Attack();

        if (dummy) return;

        if (path != null)
        {
            if (currentWaypoint >= path.vectorPath.Count)
                reachedEndOfPath = true;
            else
                reachedEndOfPath = false;
        }

        if (path != null && !reachedEndOfPath)
            Move();
    }

    #region Pathfinding

    [Server]
    private void UpdatePath()
    {
        if (Time.time > nextPathUpdate)
        {
            if (target == null)
            {
                target = GameManager.instance.GetTarget();
                targetStatus = target.GetComponent<EntityStatus>();
            }

            if (seeker.IsDone() && target != null)
                seeker.StartPath(rbody.position, target.position, OnPathComplete);
            nextPathUpdate = Time.time + pathUpdateCdr;
        }
    }

    [Server]
    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    [Server]
    private void Move()
    {
        if (Time.time < attackRecorveryTime) return;

        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rbody.position).normalized;
        Vector3 targetVelocity = direction * moveSpeed * Time.fixedDeltaTime;
        rbody.velocity = Vector3.SmoothDamp(rbody.velocity, targetVelocity, ref velocity, movementSmoothing);

        if (Vector2.Distance(rbody.position, path.vectorPath[currentWaypoint]) < nextWaypointDistance)
        {
            currentWaypoint++;
        }
    }

    #endregion

    #region Attack

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (attacking) return;

        if (collision.transform == target)
        {
            attacking = true;

            nextDelayAttack = Time.time + attackDelayTime;
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform == target)
        {
            attacking = false;
        }
    }

    [Server]
    private void Attack()
    {
        if (!attacking || Time.time < nextDelayAttack) return;

        nextDelayAttack = Time.time + attackDelayTime + attackRecorveryTime;
        // Strike
        targetStatus.CmdDealDamage(damage, netIdentity);
        RpcShowAttackSuccessVFX();
        nextAttackRecovery = Time.time + attackRecorveryTime;
    }

    private void HandleAttackingChange(bool oldAttacking, bool newAttacking)
    {
        ShowAttackVFX(newAttacking);
    }

    private void ShowAttackVFX(bool show)
    {
        attackVfx.SetActive(show);
    }

    [ClientRpc]
    private void RpcShowAttackSuccessVFX()
    {
        StartCoroutine(AttackSuccessVFXCoroutine());
    }

    [Client]
    IEnumerator AttackSuccessVFXCoroutine()
    {
        attackSuccessVfx.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        attackSuccessVfx.SetActive(false);
    }

    #endregion

    #region Obsolete but might be useful code

    /*[SerializeField] float distanceCache;

    [Server]
    private void Attack()
    {
        if (target == null || targetStatus == null) return;

        distanceCache = Vector2.Distance(transform.position, target.position);

        if (distanceCache < attackRadius)
        {
            if (Time.time > nextAttack)
            {
                if (!attacking)
                {
                    attackExecutionTime = Time.time + attackDelayTime;
                    attacking = true;
                }
                else
                {
                    if (Time.time > attackExecutionTime)
                    {
                        attacking = false;
                        nextAttack = Time.time + attackCdr;

                        targetStatus.CmdDealDamage(damage, netIdentity);
                    }
                }
            }
        }
        else
        {
            attacking = false;
        }
    }*/

    #endregion

}

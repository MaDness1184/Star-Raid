using Mirror;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : EntityController
{
    [Header("Physic Setting")]
    [SerializeField] private float moveSpeed = 300;
    [SerializeField] private float movementSmoothing = 0.1f;

    [Header("Pathfinding Setting")]
    [SerializeField] private float nextWaypointDistance = 3f;
    [SerializeField] private float pathUpdateCdr = 0.5f;
    [SerializeField] private float obstacleScanDistance = 0.7f;
    [SerializeField] private float playerScanCdr = 5f;

    [Header("Debugs")]
    [SerializeField] private Transform target;

    private Path path;
    private int currentWaypoint = 0;
    private bool reachedEndOfPath = false;
    private float nextPathUpdate;

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

    // Update is called once per frame
   

    [Server]
    private void UpdatePath()
    {
        if (Time.time > nextPathUpdate)
        {
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
        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rbody.position).normalized;
        Vector3 targetVelocity = direction * moveSpeed * Time.fixedDeltaTime;
        rbody.velocity = Vector3.SmoothDamp(rbody.velocity, targetVelocity, ref velocity, movementSmoothing);

        if (Vector2.Distance(rbody.position, path.vectorPath[currentWaypoint]) < nextWaypointDistance)
        {
            currentWaypoint++;
        }
    }

    [ServerCallback]
    void Update()
    {
        UpdatePath();
    }

    [ServerCallback]
    private void FixedUpdate()
    {
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
}

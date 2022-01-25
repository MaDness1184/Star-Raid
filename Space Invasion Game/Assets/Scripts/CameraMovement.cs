using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

public class CameraMovement : MonoBehaviour
{

    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float xAxis = 1.5f;
    [SerializeField] private float yAxis = 1;
    //[SerializeField] private float zAxis = -10;
    [SerializeField] private float yAxisZone = 1;
    [SerializeField] private float yAxisDisplacement = 1;

    float yAxisModified = 0;
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;

    PlayerStatus status;
    InputAction inputActions;

    void Update()
    {
        if (target == null) return;

        /*if(status!= null)
            if (!status.canLook) return;*/
        
        if(xAxis != 0 && yAxis != 0)
        {
            //No need to modify, transform point got player local scale already
            var mousePosition = inputActions.ReadValue<Vector2>();
            Vector2 cursor = Camera.main.ScreenToWorldPoint(mousePosition);
            yAxisModified = cursor.y;

            if (cursor.y < target.transform.position.y - yAxisZone)
            {
                yAxisModified = -yAxis;
            }
            else if (cursor.y > target.transform.position.y + yAxisZone)
            {
                yAxisModified = yAxis;
            }
            else
            {
                yAxisModified = 0;
            }
        }

        targetPosition = target.TransformPoint(
                new Vector3(xAxis, yAxisModified + yAxisDisplacement, -1));
    }

    private void FixedUpdate()
    {
        if (smoothTime == 0)
            transform.position = targetPosition;
        else
            transform.position = Vector3.SmoothDamp(transform.position,
                targetPosition, ref velocity, smoothTime);
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void SetTarget(PlayerStatus player)
    {
        this.target = player.transform;
        this.status = player;
    }
}


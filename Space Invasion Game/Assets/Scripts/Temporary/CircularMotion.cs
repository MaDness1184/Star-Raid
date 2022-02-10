using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularMotion : MonoBehaviour
{
    public float angularSpeed = 1f;
    public float circleRad = 1f;

    private Vector2 fixedPoint;
    [SerializeField] private float currentAngle;
    [SerializeField] private float currentRadAngle;

    void Start()
    {
        fixedPoint = transform.position;
    }

    void FixedUpdate()
    {
        currentRadAngle = Mathf.Deg2Rad * currentAngle;
        Vector2 offset = new Vector2(Mathf.Sin(currentRadAngle), Mathf.Cos(currentRadAngle)) * circleRad;
        transform.position = Vector3.Lerp(transform.position, fixedPoint + offset, 0.1f);
    }
}

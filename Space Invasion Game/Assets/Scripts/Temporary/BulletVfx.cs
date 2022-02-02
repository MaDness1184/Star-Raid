using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletVfx : MonoBehaviour
{
    [SerializeField] private float speed = 3000;
    //[SerializeField] private float destroyTime = 10;

    private float rangeSqr;
    private float distanceSqr;
    private Vector3 startPos;

    Rigidbody2D rb2D;

    private void Start()
    {
        startPos = transform.position;
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        rb2D.velocity = transform.right * speed * Time.deltaTime; ;
        distanceSqr = (transform.position - startPos).sqrMagnitude;

        if (distanceSqr > rangeSqr)
            Destroy(gameObject);
    }

    public void SetRange(float range)
    {
        rangeSqr = range * range;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 1000;
    [SerializeField] private float destroyTime = 5;

    Rigidbody2D rb2D;

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        Destroy(gameObject, destroyTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        rb2D.velocity = transform.right * speed * Time.deltaTime;
    }
}

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSystem : MonoBehaviour
{
    [SerializeField] private AudioClip[] ricochetSfxs;
    
    private ParticleSystem ps;
    private AudioSource audioSource;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (other.GetComponent<EnemyStatus>())
        {
            ps.TriggerSubEmitter(1);
        }
        else
        {
            ps.TriggerSubEmitter(0);
            audioSource.PlayOneShot(ricochetSfxs[Random.Range(0, ricochetSfxs.Length)]);
        }
    }
}

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityController : NetworkBehaviour
{
    [Header("Entity Specific Settings")]
    [SerializeField] private float movementSfxRate = 0.1f;

    [Header("Entity Required Componentes")]
    [SerializeField] private AudioSource movementAudioSource;

    [Header("Entity SFX")]
    [SerializeField] private AudioClip[] entityMovementSfxs;

    private float nextMovementSfx;

    [Client]
    protected void PlayMovementSFXs()
    {
        if (Time.time < nextMovementSfx) return;
        nextMovementSfx = Time.time + movementSfxRate;

        movementAudioSource.PlayOneShot(entityMovementSfxs[Random.Range(0, entityMovementSfxs.Length)]);
    }

    [Client]
    protected void StopMovementSFXs()
    {
        movementAudioSource.Stop();
    }
}

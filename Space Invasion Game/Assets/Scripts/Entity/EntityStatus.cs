using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EntityStatus : NetworkBehaviour
{
    const float TELEPORT_MARGINERROR = 1f;

    [Header("Entity Specific Settings")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private HostilityType hostility;

    [Header("Entity Required Components")]
    [SerializeField] AudioSource sfxAudioSource;

    [Header("Entity SFX and VFX")]
    [SerializeField] private ParticleSystem entityDamagedVfxs;
    [SerializeField] private GameObject[] entityDeSpawnVfxs;
    [SerializeField] private AudioClip[] entityDamagedSfxs;
    [SerializeField] private AudioClip[] entityDeSpawnSfxs;

    [Header("Entity Spawn Events")]
    [SerializeField] protected UnityEvent OnEntitySpawn;
    [SerializeField] protected UnityEvent OnEntityDeSpawn;

    [Header("Entity Debug")]
    [SerializeField]
    [SyncVar(hook = nameof(HandleHPChange))]
    private int _currentHP;
    public int currentHP
    {
        get { return _currentHP; }
    }

    protected virtual void Awake()
    {
        
    }

    #region Entity Spawn and DeSpawn

    [SyncVar(hook = nameof(HandleIsSpawnedChange))]
    private bool _isSpawned = true;
    public bool isSpawned
    {
        get { return _isSpawned; }
    }

    protected virtual void HandleIsSpawnedChange(bool oldValue, bool newValue)
    {

    }

    [Server]
    protected void EntityTeleportAndSpawn(Vector3 position)
    {
        transform.position = position;

        _isSpawned = true;
        RpcEntityTeleportAndSpawn(position);

        OnEntitySpawn?.Invoke();
    }

    [ClientRpc]
    private void RpcEntityTeleportAndSpawn(Vector3 position)
    {
        StartCoroutine(WaitForTeleportBeforeSpawn(position));
    }

    [Client]
    IEnumerator WaitForTeleportBeforeSpawn(Vector3 position)
    {
        yield return new WaitUntil(() => (transform.position - position).sqrMagnitude < TELEPORT_MARGINERROR);
        ClientEnableEntity(true);
    }

    [Server]
    protected void EntitySpawn()
    {
        _isSpawned = true;
        EnableEntity(true);

        OnEntitySpawn?.Invoke();
    }

    [Server]
    protected void EntityDeSpawn()
    {
        _isSpawned = false;
        EnableEntity(false);

        OnEntityDeSpawn?.Invoke();
    }

    [Server]
    protected virtual void EnableEntity(bool enable)
    {
        RpcEnableEntity(enable);
    }

    [ClientRpc]
    protected virtual void RpcEnableEntity(bool enable)
    {
        ClientEnableEntity(enable);
    }

    [Client]
    protected virtual void ClientEnableEntity(bool enable)
    {

    }

    #endregion

    #region HP

    protected virtual void HandleHPChange(int oldHP, int newHP)
    {
        //UpdateUI
    }

    [Server]
    protected void SetCurrentHP(int newValue)
    {
        _currentHP = newValue;
    }

    [Server]
    protected void ModifyCurrentHP(int value)
    {
        _currentHP += value;
    }

    [Server]
    protected void ResetCurrentHP()
    {
        _currentHP = maxHP;
    }

    protected int GetMaxHP()
    {
        return maxHP;
    }



    #endregion

    #region DealDamage

    [Command(requiresAuthority = false)]
    public void CmdRecieveDamage(int damage, NetworkIdentity perpetratorIdentity)
    {
        RecieveDamage(damage, perpetratorIdentity);
    }

    [Server]
    public virtual void RecieveDamage(int damage, NetworkIdentity perpetratorIdentity)
    {

    }

    #endregion

    #region VFX and SFX

    [ClientRpc]
    protected void RpcPlayOnDamagedVFXs()
    {
        entityDamagedVfxs.Play();
    }

    [ClientRpc]
    protected void RpcPlayOnDespawnVFXs(Vector3 position)
    {
        foreach (GameObject vfxGo in entityDeSpawnVfxs)
            Instantiate(vfxGo, position, Quaternion.identity);

    }

    [ClientRpc]
    protected void RpcPlayOnDeSpawnSFXs()
    {
        sfxAudioSource.PlayOneShot(entityDeSpawnSfxs[Random.Range(0, entityDeSpawnSfxs.Length)]);
    }

    [ClientRpc]
    protected void RpcPlayOnDamagedSFXs()
    {
        sfxAudioSource.PlayOneShot(entityDamagedSfxs[Random.Range(0, entityDamagedSfxs.Length)]);
    }

    #endregion

    #region Get and Set

    public HostilityType GetHostility()
    {
        return hostility;
    }

    #endregion
}


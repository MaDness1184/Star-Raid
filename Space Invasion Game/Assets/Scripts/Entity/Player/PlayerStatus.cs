using Cinemachine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStatus : EntityStatus
{
    [Header("Player Specific Settings")]
    [SerializeField] private bool godMode = false;
    [SerializeField] private bool vulnerableMode = false;
    [SerializeField] private float respawnDuration = 5f;
    [SerializeField] private float iframeDuration = 0.5f;
    [SerializeField] private float baseKnockbackForce = 500f;
    [SerializeField] private float knockbackStunDuration = 0.2f;

    [Header("Player Stunned Events")]
    [SerializeField] private UnityEvent OnPlayerStunned;
    [SerializeField] private UnityEvent OnPlayerUnStunned;

    /*[Header("Player Spawn Events")]
    [SerializeField] private UnityEvent OnPlayerDeath;
    [SerializeField] private UnityEvent OnPlayerRespawn;*/

    [Header("Player Debugs")]
    [SyncVar(hook = nameof(HandleNameChange))]
    private string playerName = "Uninitialized";

    [SyncVar]
    [SerializeField] private bool _isStunned = false;
    public bool isStunned
    {
        get { return _isStunned; }
    }

    [SyncVar]
    [SerializeField] private bool _isInvincible = false;
    public bool isInvincible
    {
        get { return _isInvincible; }
    }

    private bool respawning;

    private Rigidbody2D rb2D;
    private SpriteRenderer[] spriteRenderers;

    protected override void Awake()
    {
        base.Awake();

        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Start()
    {
        if (isServer)
            ResetCurrentHP();

        if (isClient)
            ClientEnableEntity(isSpawned);

        if (!isLocalPlayer) return; 

        Camera.main.GetComponent<CinemachineVirtualCamera>().Follow = gameObject.transform;
        DebugConsole.main.SetPlayer(this);
    }

    [Server]
    private void OnRespawn()
    {
        EntitySpawn();
        ResetCurrentHP();

        Teleport(CloningMachine.location);
    }

    [Server]
    private void OnDeath()
    {
        EntityDeSpawn();
        SetCurrentHP(0);

        RpcPlayOnDespawnVFXs(transform.position);
        RpcPlayOnDeSpawnSFXs();

        StartCoroutine(Respawning());
    }

    [Server]
    private IEnumerator Respawning()
    {
        yield return new WaitForSeconds(respawnDuration);
        OnRespawn();
    }

    [Client]
    protected override void ClientEnableEntity(bool enable)
    {
        rb2D.velocity = Vector3.zero;
        rb2D.angularVelocity = 0; 
        rb2D.simulated = enable;

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            spriteRenderer.color = enable ? Color.white : Color.clear;

        //light?
    }

    [Server]
    public override void RecieveDamage(int damage, NetworkIdentity perpetratorIdentity)
    {
        if (currentHP <= 0) return;

        RpcPlayOnDamagedVFXs();
        RpcPlayOnDamagedSFXs();

        if (vulnerableMode) damage = GetMaxHP();
        if (!godMode || vulnerableMode) ModifyCurrentHP(-damage);

        StartCoroutine(Invincible(iframeDuration));

        if (currentHP <= 0)
        {
            OnDeath();
        }
        else
        {
            KnockBack(damage * baseKnockbackForce, perpetratorIdentity.transform.position);
        }
    }

    [Server]
    IEnumerator Invincible(float invincibleDuration)
    {
        _isInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        _isInvincible = false;
    }

    #region Stunned

    [Server]
    public void Stun(bool stun)
    {
        _isStunned = stun;

        if (stun)
        {
            OnPlayerStunned?.Invoke();
        }
        else
        {
            OnPlayerUnStunned?.Invoke();
        }
    }

    [Server]
    private IEnumerator StunCoroutine(float duration)
    {
        Stun(true);
        yield return new WaitForSeconds(duration);
        Stun(false);
    }

    #endregion

    #region Knock back

    [Server]
    private void KnockBack(float force, Vector3 source)
    {
        StartCoroutine(StunCoroutine(knockbackStunDuration));
        RpcKnockBack(force, source);
    }

    [ClientRpc]
    private void RpcKnockBack(float force, Vector3 source)
    {
        if (!hasAuthority || respawning) return;

        Vector3 direction = transform.position - source;
        GetComponent<Rigidbody2D>().AddForce(direction * force);
    }

    #endregion

    #region Name Change

    [Server]
    public void ChangePlayerName(string newName)
    {
        playerName = newName;
    }

    private void HandleNameChange(string oldName, string newName)
    {
        if (!hasAuthority) return;

        gameObject.name = newName;
        PlayerUI.instance.SetPlayerName(newName);
    }

    #endregion

    #region HP Change

    protected override void HandleHPChange(int oldHP, int newHP)
    {
        base.HandleHPChange(oldHP, newHP);

        if (!hasAuthority) return;

        PlayerUI.instance.SetPlayerHP(newHP);
    }

    #endregion

    [ClientRpc]
    private void Teleport(Vector3 destination)
    {
        if (!hasAuthority) return;

        transform.position = destination;
    }

    [Server]
    public void EnableGodMode(bool enable)
    {
        godMode = enable;
    }

    [Server]
    public void EnableVulnerableMode(bool enable)
    {
        vulnerableMode = enable;
    }
}

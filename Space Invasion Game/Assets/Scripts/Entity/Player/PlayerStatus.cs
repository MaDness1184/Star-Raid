using Cinemachine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStatus : EntityStatus
{
    [Header("Player Settings")]
    [SerializeField] private bool godMode = false;
    [SerializeField] private bool vulnerableMode = false;
    [SerializeField] private float respawnDuration = 5f;
    [SerializeField] private float iframeDuration = 0.5f;
    [SerializeField] private float baseKnockbackForce = 500f;
    [SerializeField] private float knockbackStunDuration = 0.2f;

    [Header("Player Stunned Events")]
    [SerializeField] private UnityEvent OnPlayerStunned;
    [SerializeField] private UnityEvent OnPlayerUnStunned;

    [Header("Player Death Events")]
    [SerializeField] private UnityEvent OnPlayerDeath;
    [SerializeField] private UnityEvent OnPlayerRespawn;

    [Header("Player Debugs")]
    [SyncVar(hook = nameof(HandleNameChange))]
    private string playerName = "Uninitialized";

    [SyncVar]
    [SerializeField] private bool internalIsStunned = false;
    public bool isStunned
    {
        get { return internalIsStunned; }
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

    // Start is called before the first frame update
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (isServer)
            internalCurrentHP = GetMaxHP();

        if (!isLocalPlayer) return;

        Camera.main.GetComponent<CinemachineVirtualCamera>().Follow = gameObject.transform;
        DebugConsole.main.SetPlayer(this);
    }

    [Server]
    protected override void DealDamage(int damage, NetworkIdentity perpetratorIdentity)
    {
        if (vulnerableMode) damage = GetMaxHP();
        if (!godMode)
        {
            PlayOnDamagedParticle();
            internalCurrentHP -= damage;
            DebugConsole.Log($"{name} took {damage}");
        }

        StartCoroutine(Invincible(iframeDuration));

        if (internalCurrentHP <= 0)
        {
            Debug.Log(gameObject.name + " died");
            if (!respawning)
                OnDeath();
        }
        else
        {
            KnockBack(damage * baseKnockbackForce, perpetratorIdentity.transform.position);
        }
    }

    [Server]
    private void OnDeath()
    {
        foreach (GameObject vfxGO in deathVfxs)
        {
            GameObject go = Instantiate(vfxGO, transform.position, Quaternion.identity);
        }

        Stun(true);
        EnablePlayer(false);

        StartCoroutine(Respawning());

        OnPlayerDeath?.Invoke();
    }

    [Server]
    private IEnumerator Respawning()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDuration);
        respawning = false;

        OnRespawn();
    }

    [Server]
    private void OnRespawn()
    {
        Stun(false);
        EnablePlayer(true);

        internalCurrentHP = GetMaxHP();
        Teleport(CloningMachine.location);

        OnPlayerRespawn?.Invoke();
    }

    [ClientRpc]
    private void EnablePlayer(bool enable)
    {
        if (hasAuthority && rb2D.bodyType != RigidbodyType2D.Static) 
            rb2D.velocity = Vector3.zero;

        rb2D.simulated = enable;
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            spriteRenderer.color = enable ? Color.white : Color.clear;
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
        internalIsStunned = stun;

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
        if (!hasAuthority) return;

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

using Cinemachine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : EntityStatus
{
    //Header("Settings")]

    [Header("Player Debugs")]
    [SyncVar(hook = nameof(HandleNameChange))]
    [SerializeField] private string playerName = "Uninitialized";

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


    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
            internalCurrentHP = maxHP;

        if (isLocalPlayer)
            Camera.main.GetComponent<CinemachineVirtualCamera>().Follow = gameObject.transform; 
    }

    [Server]
    protected override void DealDamage(int damage)
    {
        if(internalCurrentHP <= 0)
        {
            Debug.Log(gameObject.name + " died");
        }
        else
        {
            internalCurrentHP -= damage;
            Debug.Log(gameObject.name + " HP = " + internalCurrentHP);
        }
    }
}

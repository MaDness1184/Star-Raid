using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNetworkManager : NetworkManager
{
    [SerializeField] public static readonly List<NetworkIdentity> playerIdentities = new List<NetworkIdentity>(); // Can only be read on Server
    
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);

        playerIdentities.Add(conn.identity);
        GameManager.instance.AddNewPlayer(conn.identity);

        PlayerStatus playerStatus = conn.identity.GetComponent<PlayerStatus>();
        if (numPlayers == 1)
            playerStatus.ChangePlayerName($"Host");
        else
            playerStatus.ChangePlayerName($"Client {numPlayers - 1}");

        //DebugUI.log.ShowConsole(false);
    }

    //TODO: Add event that notify all subscriber a new player has joined
}

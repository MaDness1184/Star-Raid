using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNetworkManager : NetworkManager
{
    [SerializeField] private static List<Transform> players = new List<Transform>(); // Can only be read on Server
    [SerializeField] private static List<PlayerStatus> playerStatuses = new List<PlayerStatus>();
    
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);

        GameObject player = conn.identity.gameObject;
        players.Add(player.transform);

        PlayerStatus playerStatus = player.GetComponent<PlayerStatus>();
        playerStatuses.Add(playerStatus);

        if (numPlayers == 1)
            playerStatus.ChangePlayerName($"Host");
        else
            playerStatus.ChangePlayerName($"Client {numPlayers - 1}");

        //DebugUI.log.ShowConsole(false);
    }
}

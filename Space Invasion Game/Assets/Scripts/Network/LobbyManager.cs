using kcp2k;
using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HostAddressKey = "HostAddress";

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            DebugConsole.Log("Steam was not initialized");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostSteamLobby()
    {
        Transport.activeTransport = GetComponent<FizzySteamworks>();

        if (!SteamManager.Initialized)
        {
            DebugConsole.Log("Steam was not initialized");
            return;
        }

        DebugConsole.Log("Creating Steam Lobby");

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);

        NetworkManager.singleton.StartHost();
    }

    public void JoinSteamLobby()
    {
        Transport.activeTransport = GetComponent<FizzySteamworks>();

        if (!SteamManager.Initialized)
        {
            DebugConsole.Log("Steam was not initialized");
            return;
        }

        DebugConsole.Log("Joinning Steam");

        SteamFriends.ActivateGameOverlay("Friends");
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            DebugConsole.Log("Lobby Create failed");
            return;
        }

        NetworkManager.singleton.StartHost();

        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(lobbyID,
            HostAddressKey,
            SteamUser.GetSteamID().ToString());

        DebugConsole.Log($"SteamID {SteamUser.GetSteamID()}");

        SteamFriends.ActivateGameOverlayInviteDialog(lobbyID);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        DebugConsole.Log("Join request recieved");

        Transport.activeTransport = GetComponent<FizzySteamworks>();

        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active) { return; }

        DebugConsole.Log("Client joined");
        //Client joined
        string hostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressKey);

        DebugConsole.Log($"SteamID {hostAddress}");

        NetworkManager.singleton.networkAddress = hostAddress;
        NetworkManager.singleton.StartClient();
    }

    [ContextMenu("Start Local Host")]
    public void HostLocalLobby()
    {
        Transport.activeTransport = GetComponent<KcpTransport>();

        NetworkManager.singleton.StartHost();
    }

    public void JoinLocalLobby()
    {
        Transport.activeTransport = GetComponent<KcpTransport>();

        NetworkManager.singleton.StartClient();
    }
}

using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using TMPro;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public Lobby hostLobby;
    public Lobby joinLobby;
    private float heartbeatTimer;
    private float heartbeatFrequency = 15f;
    public TMP_InputField inputFieldCode;
    public TMP_InputField inputFieldName;
    public TextMeshProUGUI lobbyCodeText;
    public RelayManager relayManager;

    private async void Start()
    {
        //await UnityServices.InitializeAsync();
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    public void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = heartbeatFrequency;
                SendHeartbeat();
            }
        }
    }

    private async void SendHeartbeat()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            Debug.Log("Heartbeat sent to lobby: " + hostLobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

public async void CreateLobby(string lobbyName, int maxPlayers = 4, bool IsPrivate = false, string gameMode = "Deathmatch", string map = "Arena")
{
    try
    {
        Player player = await GetPlayer();

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = IsPrivate,
            Player = player,

            Data = new Dictionary<string, DataObject>
            {
                { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) },
                { "Map", new DataObject(DataObject.VisibilityOptions.Public, map) },
                // 👇 AJOUTER CETTE LIGNE (pour initialiser le Relay Code)
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "0") }
            }
        };

        hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        joinLobby = hostLobby;
        lobbyCodeText.text = "Lobby Code: " + joinLobby.LobbyCode;
        Debug.Log("Party created: " + hostLobby.Name + " | Players: " + hostLobby.Players.Count + "/" + hostLobby.MaxPlayers + " | Lobby Code: " + hostLobby.LobbyCode);
        
        // 👇 AJOUTER CETTE LIGNE
        relayManager.ShowLobbyWaitingUI(true); // true = isHost
    }
    catch (LobbyServiceException e)
    {
        Debug.LogException(e);
    }
}

    public async void CreateLobby()
    {
        if (string.IsNullOrEmpty(inputFieldName.text))
            CreateLobby("DefaultLobby");
        else
            CreateLobby(inputFieldName.text);
    }

    public async void ListLobbies()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + response.Results.Count);

            foreach (Lobby lobby in response.Results)
            {
                Debug.Log("Lobby Name: " + lobby.Name + " | Players: " + lobby.Players.Count + "/" + lobby.MaxPlayers + " | Lobby Code: " + lobby.LobbyCode);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            Player player = await GetPlayer(); // Await ici au lieu de .Result

            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = player
            };

            joinLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            Debug.Log("Joined lobby with code: " + lobbyCode);
            PrintPlayers(joinLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public void JoinLobbyByCode()
    {
        if (string.IsNullOrEmpty(inputFieldCode.text))
            Debug.Log("Please enter a valid lobby code.");
        else
            JoinLobbyByCode(inputFieldCode.text);
    }

    public void PrintPlayers()
    {
        PrintPlayers(hostLobby);
    }

    public void PrintPlayers(Lobby lobby)
    {
        foreach (Player player in lobby.Players)
        {
            Debug.Log("Player ID: " + player.Id + " | Player Name: " + player.Data["PlayerName"].Value);
        }
    }

    public async Task<Player> GetPlayer()
    {
        var nickname = await AuthenticationService.Instance.GetPlayerNameAsync();

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, nickname) }
            }
        };
    }
}
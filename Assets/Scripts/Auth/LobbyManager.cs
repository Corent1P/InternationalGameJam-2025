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
    public MenuManager menuManager;
    public TMP_InputField inputFieldCode;
    public TMP_InputField inputFieldName;
    public TextMeshProUGUI lobbyCodeText;
    public TextMeshProUGUI joinErrorText;
    public TextMeshProUGUI joinSuccessText;
    public RelayManager relayManager;

    private async void Start()
    {
        // S'assurer que les services sont initialisés
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }
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
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };

            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            joinLobby = hostLobby;
            
            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = "Lobby Code: " + joinLobby.LobbyCode;
            }
            
            Debug.Log("Party created: " + hostLobby.Name + " | Players: " + hostLobby.Players.Count + "/" + hostLobby.MaxPlayers + " | Lobby Code: " + hostLobby.LobbyCode);

            // Afficher l'UI d'attente pour l'hôte
            if (relayManager != null)
                relayManager.ShowLobbyWaitingUI(true);
            if (menuManager != null)
                menuManager.HideAllMenus();
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
            Player player = await GetPlayer();

            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = player
            };

            joinLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            Debug.Log("Joined lobby with code: " + lobbyCode);
            PrintPlayers(joinLobby);
            joinErrorText.gameObject.SetActive(false);
            joinSuccessText.gameObject.SetActive(true);

            // 🔥 AJOUT CRITIQUE : Afficher l'UI d'attente pour le client
            if (relayManager != null)
                relayManager.ShowLobbyWaitingUI(false); // false = n'est pas l'hôte
            if (menuManager != null)
                menuManager.HideAllMenus();
        }
        catch (LobbyServiceException e)
        {
            joinSuccessText.gameObject.SetActive(false);
            joinErrorText.gameObject.SetActive(true);
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
        if (lobby == null) return;
        
        foreach (Player player in lobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                Debug.Log("Player ID: " + player.Id + " | Player Name: " + player.Data["PlayerName"].Value);
            }
        }
    }

    public async Task<Player> GetPlayer()
    {
        string nickname = "Player";
        
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                nickname = await AuthenticationService.Instance.GetPlayerNameAsync();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Could not get player name: " + e.Message);
        }

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, nickname) }
            }
        };
    }

    // Méthode pour quitter proprement un lobby
    public async void LeaveLobby()
    {
        try
        {
            if (joinLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(joinLobby.Id, AuthenticationService.Instance.PlayerId);
                joinLobby = null;
                hostLobby = null;
                
                if (relayManager != null)
                {
                    relayManager.HideLobbyWaitingUI();
                }
                
                Debug.Log("Left lobby successfully");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }
}
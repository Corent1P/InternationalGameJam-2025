using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;

public class RelayManager : MonoBehaviour
{
    [Header("References")]
    public LobbyManager lobbyManager;

    [Header("UI References")]
    public GameObject lobbyWaitingUI; // Panel d'attente du lobby
    // public Transform playerListContainer; // Container pour la liste des joueurs
    // public GameObject playerListItemPrefab; // Prefab pour afficher un joueur
    public GameObject startGameButton; // Bouton Start (visible uniquement pour l'hôte)

    [Header("Game Settings")]
    public string gameSceneName = "GameScene"; // Nom de votre scène de jeu

    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private float lobbyUpdateTimer;
    private float lobbyUpdateFrequency = 3f;
    private bool isHost = false;
    private bool hasJoinedRelay = false;

    private void Update()
    {
        HandleLobbyPolling();
    }

    #region Lobby Polling & UI

    private void HandleLobbyPolling()
    {
        if (lobbyManager.joinLobby != null && !hasJoinedRelay)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer <= 0f)
            {
                lobbyUpdateTimer = lobbyUpdateFrequency;
                PollLobby();
            }
        }
    }

    private async void PollLobby()
    {
        try
        {
            // Vérifier que le lobby existe toujours
            if (lobbyManager.joinLobby == null)
            {
                Debug.LogWarning("Join lobby is null, stopping polling");
                return;
            }

            lobbyManager.joinLobby = await LobbyService.Instance.GetLobbyAsync(lobbyManager.joinLobby.Id);
            UpdatePlayerListUI();
            
            // Si on n'est pas l'hôte, vérifier si le Relay Code est disponible
            if (!isHost && lobbyManager.joinLobby.Data != null && lobbyManager.joinLobby.Data.ContainsKey(KEY_RELAY_JOIN_CODE))
            {
                string relayJoinCode = lobbyManager.joinLobby.Data[KEY_RELAY_JOIN_CODE].Value;
                if (!string.IsNullOrEmpty(relayJoinCode) && relayJoinCode != "0")
                {
                    // L'hôte a démarré la partie, rejoindre via Relay
                    await JoinRelay(relayJoinCode);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobby polling failed: {e.Message} | Reason: {e.Reason} | Code: {e.ErrorCode}");
            
            // Si le lobby n'existe plus, arrêter le polling
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning("Lobby no longer exists, stopping polling");
                lobbyManager.joinLobby = null;
                HideLobbyWaitingUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error in polling: {e.Message}");
        }
    }

    public void ShowLobbyWaitingUI(bool isHostPlayer)
    {
        isHost = isHostPlayer;
        
        if (lobbyWaitingUI != null)
            lobbyWaitingUI.SetActive(true);

        if (startGameButton != null)
            startGameButton.SetActive(isHost);

        UpdatePlayerListUI();
    }

    public void HideLobbyWaitingUI()
    {
        if (lobbyWaitingUI != null)
            lobbyWaitingUI.SetActive(false);
        
        isHost = false;
        hasJoinedRelay = false;
    }

    private void UpdatePlayerListUI()
    {
        // if (lobbyManager.joinLobby == null || playerListContainer == null) return;

        // // Nettoyer la liste actuelle
        // foreach (Transform child in playerListContainer)
        // {
        //     Destroy(child.gameObject);
        // }

        // // Créer un élément UI pour chaque joueur
        // foreach (Player player in lobbyManager.joinLobby.Players)
        // {
        //     GameObject playerItem = Instantiate(playerListItemPrefab, playerListContainer);
        //     TMP_Text playerNameText = playerItem.GetComponentInChildren<TMP_Text>();
            
        //     if (playerNameText != null && player.Data.ContainsKey("PlayerName"))
        //     {
        //         playerNameText.text = player.Data["PlayerName"].Value;
        //     }
        // }

        // Afficher le nombre de joueurs 
        Debug.Log($"Players in lobby: {lobbyManager.joinLobby.Players.Count}/{lobbyManager.joinLobby.MaxPlayers}");
    }

    #endregion

    #region Relay Integration

    public async void StartGame()
    {
        if (!isHost)
        {
            Debug.LogWarning("Only the host can start the game!");
            return;
        }

        if (lobbyManager.joinLobby == null)
        {
            Debug.LogWarning("No active lobby!");
            return;
        }

        try
        {
            Debug.Log("Starting game and creating Relay allocation...");

            // Créer une allocation Relay
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(lobbyManager.joinLobby.MaxPlayers - 1);
            
            // Obtenir le Join Code
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            Debug.Log("Relay Join Code: " + relayJoinCode);

            // Mettre à jour le Lobby avec le Relay Join Code
            await LobbyService.Instance.UpdateLobbyAsync(lobbyManager.joinLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });

            // Configurer le transport Unity pour l'hôte
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Démarrer l'hôte
            NetworkManager.Singleton.StartHost();

            hasJoinedRelay = true;
            PlayerPrefs.SetInt("MaxPlayers", lobbyManager.joinLobby.MaxPlayers);

            // Charger la scène de jeu
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);

            Debug.Log("Game started successfully!");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay creation failed: " + e);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Lobby update failed: " + e);
        }
    }

    private async Task JoinRelay(string relayJoinCode)
    {
        if (hasJoinedRelay) return;

        try
        {
            Debug.Log("Joining game via Relay with code: " + relayJoinCode);

            // Rejoindre l'allocation Relay
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // Configurer le transport Unity pour le client
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Démarrer le client
            NetworkManager.Singleton.StartClient();

            hasJoinedRelay = true;

            Debug.Log("Successfully joined Relay!");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay join failed: " + e);
        }
    }

    #endregion
}
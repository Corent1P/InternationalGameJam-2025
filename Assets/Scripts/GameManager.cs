using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

public enum Team
{
    Children,
    Adults
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NetworkPlayerSpawner playerSpawner;

    [Header("Game Data")]
    public Dictionary<ulong, PlayerData> playerStatesByID = new();

    [SerializeField] private float waitingTimeForJoiningPlayers = 15f;
    private bool hasAdultBeenAssigned = false;
    private bool hasBeenSpawned = false;

    private int expectedPlayerCount;
    private List<ulong> readyPlayers = new List<ulong>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            Debug.Log("GameManager spawned on client.");
            return;
        }
        Debug.Log("GameManager spawned on server.");
        expectedPlayerCount = PlayerPrefs.GetInt("MaxPlayers", 4);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        StartCoroutine(WaitForAllPlayersAndAssignTeams());
    }

    private IEnumerator WaitForAllPlayersAndAssignTeams()
    {
        int expectedPlayers = PlayerPrefs.GetInt("MaxPlayers", 4);
        float timeout = waitingTimeForJoiningPlayers;
        float elapsed = 0f;
        
        while (NetworkManager.Singleton.ConnectedClientsIds.Count < expectedPlayers && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        AssignTeamsAndSpawnPlayers();
        hasBeenSpawned = true;
    }
    
    private void AssignTeamsAndSpawnPlayers()
    {
        List<ulong> allClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        
        // Choisir aléatoirement un adulte
        int adultIndex = Random.Range(0, allClients.Count);
        
        for (int i = 0; i < allClients.Count; i++)
        {
            Team team = (i == adultIndex) ? Team.Adults : Team.Children;
            playerSpawner.SpawnPlayerForClient(allClients[i], team);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Client {clientId} connected to game, spawning player...");

        if (!NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject && hasBeenSpawned)
        {
            Debug.Log($"---------------------- Spawning player for client {clientId}");
            playerSpawner.SpawnPlayerForClient(clientId, Team.Children); // Default to Children team; modify as needed
        }
        else
        {
            Debug.Log($"Client {clientId} already has a PlayerObject");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Client {clientId} disconnected from game");

        if (playerStatesByID.ContainsKey(clientId))
        {
            playerStatesByID.Remove(clientId);
        }

        playerSpawner.ResetSpawnPoints();
    }

    public void RegisterPlayerData(ulong clientId, PlayerData data)
    {
        if (!IsServer) return;

        playerStatesByID[clientId] = data;
        Debug.Log($"Registered player data for client {clientId}");
    }

    public PlayerData GetPlayerData(ulong clientId)
    {
        if (playerStatesByID.TryGetValue(clientId, out PlayerData data))
        {
            return data;
        }
        return null;
    }
}

// Classe de données temporaire (à adapter selon vos besoins)
[System.Serializable]
public class PlayerData
{
    public ulong ID;
    public Vector3 Position;

    public PlayerData(ulong id, Vector3 position)
    {
        ID = id;
        Position = position;
    }
}
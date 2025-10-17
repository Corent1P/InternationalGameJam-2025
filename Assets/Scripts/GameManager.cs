using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private NetworkPlayerSpawner playerSpawner;
    
    [Header("Game Data")]
    public Dictionary<ulong, PlayerData> playerStatesByID = new();

    private void Awake()
    {
        // Singleton simple SANS DontDestroyOnLoad
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

        // S'assurer qu'on est bien sur le serveur
        if (!IsServer) 
        {
            Debug.Log("GameManager spawned on client.");
            return;
        }

        Debug.Log("GameManager spawned on server.");

        // S'abonner aux événements de connexion
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Spawner les joueurs déjà connectés (y compris l'hôte)
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            // Vérifier si le joueur n'a pas déjà un PlayerObject
            if (!NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject)
            {
                Debug.Log($"Spawning player for already connected client {clientId}");
                playerSpawner.SpawnPlayerForClient(clientId);
            }
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
        
        // Vérifier si le joueur n'a pas déjà un PlayerObject
        if (!NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject)
        {
            playerSpawner.SpawnPlayerForClient(clientId);
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
        
        // Nettoyer les données du joueur
        if (playerStatesByID.ContainsKey(clientId))
        {
            playerStatesByID.Remove(clientId);
        }
        
        // Réinitialiser les spawn points
        playerSpawner.ResetSpawnPoints();
    }

    // Méthode pour enregistrer les données d'un joueur
    public void RegisterPlayerData(ulong clientId, PlayerData data)
    {
        if (!IsServer) return;

        playerStatesByID[clientId] = data;
        Debug.Log($"Registered player data for client {clientId}");
    }

    // Méthode pour récupérer les données d'un joueur
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
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class GameManager : NetworkBehaviour
{

    public static GameManager Instance { get; private set; }
    public GameObject playerPrefab;
    public Dictionary<string, PlayerData> playerStatesByID = new();

    public Action OnConnection;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }
        else
        {
            Debug.Log("Client: GameManager5 has spawned.");
        }

        OnConnection?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
        }
        else
        {
            Debug.Log("Client: GameManager5 has despawned.");
        }
    }

    private void HandleDisconnect(ulong clientId)
    {
        Debug.Log($"Client {clientId} has disconnected.");
    }

    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string clientId, ulong ID)
    {
        if (!playerStatesByID.TryGetValue(clientId, out PlayerData playerData))
        {
            PlayerData newPlayerData = new PlayerData(ID, Vector3.zero, 100, 5);
            playerStatesByID[clientId] = newPlayerData;
            SpawnPlayerServer(ID, newPlayerData);
            Debug.Log($"Registered new player with ID {clientId} and spawned at position {newPlayerData.Position}.");
        }
        else
        {
            Debug.Log($"Player with ID {clientId} already exists. Respawning existing player data.");
            Debug.Log($"Existing Player Data - Position: {playerData.Position}, Health: {playerData.Health}, Attack: {playerData.Attack}");
            SpawnPlayerServer(ID, playerData);
        }
    }

    public void SpawnPlayerServer(ulong ID, PlayerData playerData)
    {
        if (!IsServer) return;

        GameObject playerObject = Instantiate(playerPrefab, playerData.Position, Quaternion.identity);

        playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        playerObject.GetComponent<Tmp_Player>().SetData(playerData);
    }
}

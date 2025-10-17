using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class GameManager5 : NetworkBehaviour
{

    public static GameManager5 Instance { get; private set; }
    public GameObject playerPrefab;
    public Dictionary<string, PlayerData_tmp> playerStatesByID = new();

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
        if (!playerStatesByID.TryGetValue(clientId, out PlayerData_tmp playerData))
        {
            PlayerData_tmp newPlayerData = new PlayerData_tmp(ID, Vector3.zero, 100, 5);
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

    public void SpawnPlayerServer(ulong ID, PlayerData_tmp playerData)
    {
        if (!IsServer) return;

        GameObject playerObject = Instantiate(playerPrefab, playerData.Position, Quaternion.identity);

        playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        playerObject.GetComponent<Tmp_Player>().SetData(playerData);
    }
}

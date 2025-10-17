using System.Numerics;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player5 : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accountID = new();
    public NetworkVariable<int> health = new(100);
    public NetworkVariable<int> attack = new(0);
    public override void OnNetworkSpawn()
    {
        // if (IsOwner)
        // {
        //     Debug.Log("Player5: I am the owner of this object." + NetworkManager.Singleton.LocalClientId);
        // }
        // else
        // {
        //     Debug.Log("Player5: I am not the owner of this object." + NetworkManager.Singleton.LocalClientId);
        // }
    }

    public override void OnNetworkDespawn()
    {
        GameManager.Instance.playerStatesByID[accountID.Value.ToString()] = new PlayerData(NetworkManager.Singleton.LocalClientId, transform.position, health.Value, attack.Value);
        Debug.Log($"Player5: Despawning player {accountID.Value} and saving state.");
    }

    public void SetData(PlayerData data)
    {
        accountID.Value = data.ClientId.ToString();
        health.Value = data.Health;
        attack.Value = data.Attack;
        transform.position = data.Position;
    }
}

public class PlayerData
{
    public ulong ClientId { get; set; }
    public UnityEngine.Vector3 Position { get; set; }
    public int Health { get; set; }
    public int Attack { get; set; }

    public PlayerData(ulong clientId, UnityEngine.Vector3 position, int health, int attack)
    {
        ClientId = clientId;
        Position = position;
        Health = health;
        Attack = attack;
    }
}

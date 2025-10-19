using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class AdultManager : NetworkBehaviour
{
    [Header("Game Stats")]
    private NetworkVariable<int> coins = new NetworkVariable<int>(0);
    private NetworkVariable<int> childrenCaught = new NetworkVariable<int>(0);

    [Header("Game Phase")]
    private NetworkVariable<bool> isPreparationPhase = new NetworkVariable<bool>(true);

    [Header("Inventory")]
    public int maxInventorySize = 10;
    private List<GameObject> inventory = new List<GameObject>();

    #region Coins Management
    public void SetCoins(int amount)
    {
        if (!IsServer)
        {
            SetCoinsServerRpc(amount);
            return;
        }
        coins.Value = Mathf.Max(0, amount);
        Debug.Log($"Adult coins set to: {coins.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetCoinsServerRpc(int amount)
    {
        coins.Value = Mathf.Max(0, amount);
    }

    public void AddCoins(int amount)
    {
        if (!IsServer)
        {
            AddCoinsServerRpc(amount);
            return;
        }
        coins.Value += amount;
        Debug.Log($"Adult coins added! Total: {coins.Value} (+{amount})");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCoinsServerRpc(int amount)
    {
        coins.Value += amount;
    }

    public void RemoveCoins(int amount)
    {
        if (!IsServer)
        {
            RemoveCoinsServerRpc(amount);
            return;
        }
        coins.Value = Mathf.Max(0, coins.Value - amount);
        Debug.Log($"Adult coins removed! Total: {coins.Value} (-{amount})");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveCoinsServerRpc(int amount)
    {
        coins.Value = Mathf.Max(0, coins.Value - amount);
    }

    public int GetCoins() => coins.Value;
    #endregion

    #region Children Caught Stats
    public void IncrementChildrenCaught()
    {
        if (!IsServer)
        {
            IncrementChildrenCaughtServerRpc();
            return;
        }
        childrenCaught.Value++;
        Debug.Log($"Total children caught: {childrenCaught.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncrementChildrenCaughtServerRpc()
    {
        childrenCaught.Value++;
    }

    public int GetChildrenCaught() => childrenCaught.Value;
    #endregion

    #region Inventory Management
    public bool AddItemToInventory(GameObject itemPrefab)
    {
        if (!IsServer)
        {
            Debug.LogWarning("AddItemToInventory can only be called on server!");
            return false;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning("Cannot add null item to inventory!");
            return false;
        }

        if (inventory.Count >= maxInventorySize)
        {
            Debug.Log($"Inventory full! ({inventory.Count}/{maxInventorySize})");
            return false;
        }

        inventory.Add(itemPrefab);
        Debug.Log($"✅ Added {itemPrefab.name} to inventory! ({inventory.Count}/{maxInventorySize})");
        return true;
    }

    public bool RemoveItemFromInventory(int index)
    {
        if (!IsServer)
        {
            RemoveItemServerRpc(index);
            return false;
        }

        if (index < 0 || index >= inventory.Count)
        {
            Debug.LogWarning($"Invalid inventory index: {index}");
            return false;
        }

        GameObject item = inventory[index];
        inventory.RemoveAt(index);
        Debug.Log($"❌ Removed {item.name} from inventory! ({inventory.Count}/{maxInventorySize})");
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveItemServerRpc(int index)
    {
        RemoveItemFromInventory(index);
    }

    public GameObject GetItemAtIndex(int index)
    {
        if (index < 0 || index >= inventory.Count)
        {
            return null;
        }
        return inventory[index];
    }

    public int GetInventoryCount() => inventory.Count;

    public List<GameObject> GetInventory() => new List<GameObject>(inventory);

    public bool IsInventoryFull() => inventory.Count >= maxInventorySize;

    public void PlaceTrap(int inventoryIndex, Vector3 position, Quaternion rotation)
    {
        // Si c'est un client, on envoie la requête au serveur
        if (!IsServer)
        {
            PlaceTrapServerRpc(inventoryIndex, position, rotation);
            return;
        }

        // Serveur : on instancie et on spawn
        GameObject trapPrefab = GetItemAtIndex(inventoryIndex);
        if (trapPrefab == null)
        {
            Debug.LogWarning($"[AdultManager] Aucun piège à l'index {inventoryIndex}");
            return;
        }

        // Important : vérifier que le prefab est bien enregistré dans NetworkManager
        NetworkObject prefabNetObj = trapPrefab.GetComponent<NetworkObject>();
        if (prefabNetObj == null)
        {
            Debug.LogError($"[AdultManager] Le prefab '{trapPrefab.name}' n’a pas de NetworkObject !");
            return;
        }

        // Instancier le piège côté serveur
        GameObject trapInstance = Instantiate(trapPrefab, position, rotation);

        // Obtenir son NetworkObject et le propager à tous les clients
        NetworkObject netObj = trapInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true); // true => visible par tous les clients
            Debug.Log($"[AdultManager] Spawned trap '{trapPrefab.name}' for all clients.");
        }
        else
        {
            Debug.LogError("[AdultManager] Le piège instancié n’a pas de NetworkObject !");
        }

        // Retirer l’objet de l’inventaire
        RemoveItemFromInventory(inventoryIndex);

        Debug.Log($"🎯 {trapPrefab.name} placé à {position}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceTrapServerRpc(int inventoryIndex, Vector3 position, Quaternion rotation)
    {
        // Exécute la logique serveur
        PlaceTrap(inventoryIndex, position, rotation);
    }


    // public void PlaceTrap(int inventoryIndex, Vector3 position, Quaternion rotation)
    // {
    //     if (!IsServer)
    //     {
    //         PlaceTrapServerRpc(inventoryIndex, position, rotation);
    //         return;
    //     }

    //     GameObject trapPrefab = GetItemAtIndex(inventoryIndex);
    //     if (trapPrefab == null)
    //     {
    //         Debug.LogWarning($"No trap at index {inventoryIndex}");
    //         return;
    //     }

    //     GameObject trap = Instantiate(trapPrefab, position, rotation);

    //     NetworkObject networkObject = trap.GetComponent<NetworkObject>();
    //     if (networkObject != null)
    //     {
    //         networkObject.Spawn();
    //     }

    //     RemoveItemFromInventory(inventoryIndex);

    //     Debug.Log($"🎯 Placed {trapPrefab.name} at {position}");
    // }

    // [ServerRpc(RequireOwnership = false)]
    // private void PlaceTrapServerRpc(int inventoryIndex, Vector3 position, Quaternion rotation)
    // {
    //     PlaceTrap(inventoryIndex, position, rotation);
    // }

    public void ClearInventory()
    {
        if (!IsServer)
        {
            ClearInventoryServerRpc();
            return;
        }
        inventory.Clear();
        Debug.Log("🗑️ Inventory cleared!");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClearInventoryServerRpc()
    {
        inventory.Clear();
    }
    #endregion

    #region Game Phase
    public void SetPreparationPhase(bool isPhase)
    {
        if (!IsServer)
        {
            SetPreparationPhaseServerRpc(isPhase);
            return;
        }
        isPreparationPhase.Value = isPhase;
        Debug.Log($"Adult preparation phase: {isPhase}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPreparationPhaseServerRpc(bool isPhase)
    {
        isPreparationPhase.Value = isPhase;
    }

    public bool IsPreparationPhase() => isPreparationPhase.Value;
    #endregion

    #region Reset & Utility
    public void ResetStats()
    {
        if (!IsServer)
        {
            ResetStatsServerRpc();
            return;
        }
        coins.Value = 0;
        childrenCaught.Value = 0;
        inventory.Clear();
        Debug.Log("Adult stats reset!");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetStatsServerRpc()
    {
        coins.Value = 0;
        childrenCaught.Value = 0;
        inventory.Clear();
    }
    #endregion
}

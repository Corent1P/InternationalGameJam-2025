using UnityEngine;
using Unity.Netcode;

public class ShopManager : NetworkBehaviour
{
    public NetworkAdultController adultController;

    public void SetAdultController(NetworkAdultController controller)
    {
        adultController = controller;
    }

    public void TryPurchaseItem(int itemIndex)
    {
        if (adultController == null)
            return;
        // Passer une référence réseau pour adultController
        NetworkObject adultNetworkObject = adultController.GetComponent<NetworkObject>();
        if (adultNetworkObject == null)
            return;

        PurchaseItemServerRpc(itemIndex, new NetworkObjectReference(adultNetworkObject));
    }

    [ServerRpc(RequireOwnership = false)]
    private void PurchaseItemServerRpc(int itemIndex, NetworkObjectReference adultControllerRef, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"[ShopManager] PurchaseItemServerRpc called for item index {itemIndex} by client {serverRpcParams.Receive.SenderClientId}"); // ici

        // Résoudre la référence réseau
        if (!adultControllerRef.TryGet(out NetworkObject adultNetworkObject))
        {
            Debug.LogWarning("[ShopManager] Failed to resolve adultController NetworkObject!");
            return;
        }

        NetworkAdultController adultController = adultNetworkObject.GetComponent<NetworkAdultController>();
        if (adultController == null)
        {
            Debug.LogWarning("[ShopManager] adultController is null on server!");
            return;
        }

        AdultManager adultManager = adultController.GetAdultManager();
        if (adultManager == null)
        {
            Debug.LogWarning("[ShopManager] adultManager is null!");
            return;
        }

        var shop = adultController.GetShopMenu();
        if (shop == null || itemIndex < 0 || itemIndex >= shop.shopItems.Count)
        {
            if (shop != null)
                Debug.LogWarning("[ShopManager] index : " + itemIndex + "/" + shop.shopItems.Count);
            Debug.LogWarning("[ShopManager] Invalid shop or item index!"); // ici
            return;
        }

        Debug.Log($"[ShopManager] Attempting to purchase item at index {itemIndex}");

        ShopItem item = shop.shopItems[itemIndex];

        Debug.Log($"[ShopManager] Player {adultController.OwnerClientId} is attempting to purchase {item.itemName} for {item.price} coins.");

        if (adultManager.GetCoins() < item.price)
            return;
        if (adultManager.IsInventoryFull())
            return;
        if (item.itemPrefab == null)
            return;

        adultManager.RemoveCoins(item.price);

        bool success = adultManager.AddItemToInventory(item.itemPrefab);

        if (success)
        {
            NotifyPurchaseSuccessClientRpc(item.itemName, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { adultController.OwnerClientId } }
            });
        }
        else
        {
            // Remboursement si échec
            adultManager.AddCoins(item.price);
            Debug.LogWarning($"[ShopManager] Failed to add {item.itemName} to inventory!");
        }
    }

    [ClientRpc]
    private void NotifyPurchaseSuccessClientRpc(string itemName, ClientRpcParams clientRpcParams)
    {
        Debug.Log($"✅ Purchased item: {itemName}");
    }
}

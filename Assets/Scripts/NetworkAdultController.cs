using UnityEngine;

[RequireComponent(typeof(AdultManager))]
public class NetworkAdultController : NetworkPlayerController
{
    [Header("Shop")]
    public ShopRadialMenu shopMenu;

    private AdultManager adultManager;

    protected override void Awake()
    {
        base.Awake();
        adultManager = GetComponent<AdultManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) {
            Debug.LogWarning("[NetworkAdultController] This controller is not the owner, skipping shop init.");
            return;
        }

        if (shopMenu == null) {
            shopMenu = FindObjectOfType<ShopRadialMenu>(true);

            if (shopMenu == null) {
                Debug.LogError("[NetworkAdultController] No ShopRadialMenu found in scene!");
                return;
            }
        }

        shopMenu.gameObject.SetActive(true);
        Debug.Log("[NetworkAdultController] Shop menu successfully linked and activated for adult.");
    }

    private void HandleBuyRequest(int index)
    {
        Debug.Log($"[CLIENT] Player requested to buy item {index}");
    }

    public AdultManager GetAdultManager() => adultManager;
}

using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AdultManager))]
public class NetworkAdultController : NetworkPlayerController
{
    [Header("Shop")]
    public ShopRadialMenu shopMenu;

    private AdultManager adultManager;
    private Animator animator;
    
    private NetworkVariable<float> networkAnimSpeed = new NetworkVariable<float>(0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);

    protected override void Awake()
    {
        base.Awake();
        adultManager = GetComponent<AdultManager>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkAnimSpeed.OnValueChanged += OnAnimSpeedChanged;

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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        networkAnimSpeed.OnValueChanged -= OnAnimSpeedChanged;
    }

    private void OnAnimSpeedChanged(float oldValue, float newValue)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", newValue);
        }
    }

    protected override void HandleMovement()
    {
        if (IsOwner)
        {
            if (moveInput == Vector2.zero)
                networkAnimSpeed.Value = 0f;
            else
                networkAnimSpeed.Value = 1f;
        }

        base.HandleMovement();
    }

    private void HandleBuyRequest(int index)
    {
        Debug.Log($"[CLIENT] Player requested to buy item {index}");
    }

    public AdultManager GetAdultManager() => adultManager;
}

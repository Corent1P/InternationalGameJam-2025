using UnityEngine;
using Unity.Netcode;

public class AdultManager : NetworkBehaviour
{
    [Header("Game Stats")]
    private NetworkVariable<int> coins = new NetworkVariable<int>(0);

    [Header("Game Phase")]
    private NetworkVariable<bool> isPreparationPhase = new NetworkVariable<bool>(true);

    #region Coins Management
    public void SetCoins(int amount) {
        if (!IsServer) {
            SetCoinsServerRpc(amount);
            return;
        }
        coins.Value = Mathf.Max(0, amount);
        Debug.Log($"Adult coins set to: {coins.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetCoinsServerRpc(int amount) {
        coins.Value = Mathf.Max(0, amount);
    }

    public void AddCoins(int amount) {
        if (!IsServer) {
            AddCoinsServerRpc(amount);
            return;
        }
        coins.Value += amount;
        Debug.Log($"Adult coins added ! Total: {coins.Value} (+{amount})");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCoinsServerRpc(int amount) {
        coins.Value += amount;
    }

    public void RemoveCoins(int amount) {
        if (!IsServer) {
            RemoveCoinsServerRpc(amount);
            return;
        }
        coins.Value = Mathf.Max(0, coins.Value - amount);
        Debug.Log($"Adult coins removed! Total: {coins.Value} (-{amount})");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveCoinsServerRpc(int amount) {
        coins.Value = Mathf.Max(0, coins.Value - amount);
    }

    public int GetCoins() => coins.Value;
    #endregion

    #region Game Phase
    public void SetPreparationPhase(bool isPhase) {
        if (!IsServer) {
            SetPreparationPhaseServerRpc(isPhase);
            return;
        }
        isPreparationPhase.Value = isPhase;
        Debug.Log($"Adult preparation phase: {isPhase}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPreparationPhaseServerRpc(bool isPhase) {
        isPreparationPhase.Value = isPhase;
    }

    public bool IsPreparationPhase() => isPreparationPhase.Value;
    #endregion

    #region Reset & Utility
    public void ResetStats() {
        if (!IsServer) {
            ResetStatsServerRpc();
            return;
        }
        coins.Value = 0;
        Debug.Log("Adult stats reset!");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetStatsServerRpc() {
        coins.Value = 0;
    }
    #endregion
}

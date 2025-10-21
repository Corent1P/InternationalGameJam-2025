using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(ChildrenAbility))]
public class ChildrenManager : NetworkBehaviour
{
    [Header("Candy Inventory")]
    public int maxCandies = 5;
    private NetworkVariable<int> currentCandies = new NetworkVariable<int>(0);

    [Header("Game Stats")]
    private NetworkVariable<int> coins = new NetworkVariable<int>(0);
    private NetworkVariable<int> timesCaught = new NetworkVariable<int>(0);
    private NetworkVariable<bool> caught = new NetworkVariable<bool>(false);

    [Header("Game Phase")]
    private NetworkVariable<bool> isPreparationPhase = new NetworkVariable<bool>(true);

    private ChildrenAbility childrenAbility;

    private void Awake() {
        childrenAbility = GetComponent<ChildrenAbility>();
    }

    #region Ability Methods
    public void UseAbility() {
        childrenAbility?.ActivateAbility();
    }

    public void SpeedBoost(float multiplier, float duration) {
        childrenAbility?.ActivateSpeedBoost(multiplier, duration);
    }

    public void Slow(float multiplier, float duration) {
        childrenAbility?.ActivateSlow(multiplier, duration);
    }

    public void Stun(float duration) {
        childrenAbility?.Stun(duration);
    }

    public bool CanMove() => childrenAbility?.CanMove() ?? true;
    public float GetSpeedMultiplier() => childrenAbility?.GetSpeedMultiplier() ?? 1f;
    #endregion

    #region Candy Management
    public bool AddCandy() {
        if (!IsServer) {
            Debug.LogWarning("AddCandy called on client! This should only be called on server.");
            return false;
        }
        if (currentCandies.Value >= maxCandies) {
            return false;
        }
        currentCandies.Value++;
        return true;
    }

    public bool RemoveCandy() {
        if (!IsServer) {
            Debug.LogWarning("RemoveCandy called on client ! This should only be called on server.");
            return false;
        }
        if (currentCandies.Value <= 0) {
            return false;
        }
        currentCandies.Value--;
        Debug.Log($"Candy removed! ({currentCandies.Value}/{maxCandies})");
        return true;
    }

    public int GetCandyCount() => currentCandies.Value;
    public int GetMaxCandies() => maxCandies;
    public bool IsCandyFull() => currentCandies.Value >= maxCandies;
    public int getCandy() => GetCandyCount();
    #endregion

    #region Coins Management
    public void SetCoins(int amount) {
        if (!IsServer) {
            SetCoinsServerRpc(amount);
            return;
        }
        coins.Value = Mathf.Max(0, amount);
        Debug.Log($"Coins set to: {coins.Value}");
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
        Debug.Log($"Coins added ! Total: {coins.Value} (+{amount})");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCoinsServerRpc(int amount) {
        coins.Value += amount;
    }

    public int GetCoins() => coins.Value;
    #endregion

    #region Caught Status
    public void SetCaught(bool isCaught) {
        if (!IsServer) {
            SetCaughtServerRpc(isCaught);
            return;
        }
        caught.Value = isCaught;

        if (isCaught) {
            timesCaught.Value++;
            Debug.Log($"Child caught ! Total times: {timesCaught.Value}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetCaughtServerRpc(bool isCaught) {
        caught.Value = isCaught;
        if (isCaught) {
            timesCaught.Value++;
        }
    }

    public bool IsCaught() => caught.Value;
    public bool isCaught() => IsCaught();
    public int GetNumberCaught() => timesCaught.Value;
    public int getNumberCaught() => GetNumberCaught();

    public void ResetCaughtStatus() {
        if (!IsServer) {
            ResetCaughtStatusServerRpc();
            return;
        }
        caught.Value = false;
        Debug.Log("Child released!");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetCaughtStatusServerRpc() {
        caught.Value = false;
    }
    #endregion

    #region Game Phase
    public void SetPreparationPhase(bool isPhase) {
        if (!IsServer) {
            SetPreparationPhaseServerRpc(isPhase);
            return;
        }
        isPreparationPhase.Value = isPhase;
        Debug.Log($"Preparation phase: {isPhase}");
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
        currentCandies.Value = 0;
        coins.Value = 0;
        timesCaught.Value = 0;
        caught.Value = false;
        Debug.Log("Stats reset !");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetStatsServerRpc() {
        currentCandies.Value = 0;
        coins.Value = 0;
        timesCaught.Value = 0;
        caught.Value = false;
    }
    #endregion
}

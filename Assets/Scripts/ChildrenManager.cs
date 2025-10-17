using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(ChildrenAbility))]
public class ChildrenManager : NetworkBehaviour
{
    [Header("Candy Inventory")]
    public int maxCandies = 5;

    private NetworkVariable<int> currentCandies = new NetworkVariable<int>(0);
    private ChildrenAbility childrenAbility;

    private void Awake() {
        childrenAbility = GetComponent<ChildrenAbility>();
    }

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

    public bool AddCandy() {
        if (!IsServer) {
            Debug.LogWarning("AddCandy called on client! This should only be called on server.");
            return false;
        }

        if (currentCandies.Value >= maxCandies) {
            Debug.Log($"Inventory full! ({currentCandies.Value}/{maxCandies})");
            return false;
        }

        currentCandies.Value++;
        Debug.Log($"🍬 Candy added! ({currentCandies.Value}/{maxCandies})");
        return true;
    }

    public bool RemoveCandy() {
        if (!IsServer) {
            Debug.LogWarning("RemoveCandy called on client! This should only be called on server.");
            return false;
        }

        if (currentCandies.Value <= 0) {
            return false;
        }

        currentCandies.Value--;
        Debug.Log($"Candy removed! ({currentCandies.Value}/{maxCandies})");
        return true;
    }

    public bool CanMove() => childrenAbility?.CanMove() ?? true;
    public float GetSpeedMultiplier() => childrenAbility?.GetSpeedMultiplier() ?? 1f;
    public int GetCandyCount() => currentCandies.Value;
    public int GetMaxCandies() => maxCandies;
    public bool IsCandyFull() => currentCandies.Value >= maxCandies;
}

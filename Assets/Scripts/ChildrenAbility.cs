using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class ChildrenAbility : NetworkBehaviour
{
    private NetworkVariable<bool> canMove = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> speedMultiplier = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine speedCoroutine;
    private Coroutine stunCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) {
            canMove.OnValueChanged += OnCanMoveChanged;
            speedMultiplier.OnValueChanged += OnSpeedMultiplierChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsServer) {
            canMove.OnValueChanged -= OnCanMoveChanged;
            speedMultiplier.OnValueChanged -= OnSpeedMultiplierChanged;
        }
    }


    private void OnCanMoveChanged(bool oldValue, bool newValue)
    {
        // TODO : Ajoutez ici les effets visuels pour stun/unstun
    }

    private void OnSpeedMultiplierChanged(float oldValue, float newValue)
    {
        // TODO : Ajoutez ici les effets visuels pour speed boost/slow
    }

    public void ActivateAbility()
    {
        if (!IsOwner) {
            Debug.LogWarning("ActivateAbility can only be called by the owner!");
            return;
        }

        ActivateAbilityServerRpc();
    }

    [ServerRpc]
    private void ActivateAbilityServerRpc()
    {
        ActivateSpeedBoost(1.5f, 3f);
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        if (!IsServer) {
            Debug.LogWarning("ActivateSpeedBoost should only be called on the server!");
            return;
        }

        if (speedCoroutine != null) {
            StopCoroutine(speedCoroutine);
        }

        speedCoroutine = StartCoroutine(SpeedRoutine(multiplier, duration));
    }

    public void ActivateSlow(float multiplier, float duration)
    {
        if (!IsServer) {
            Debug.LogWarning("ActivateSlow should only be called on the server!");
            return;
        }

        if (speedCoroutine != null) {
            StopCoroutine(speedCoroutine);
        }

        speedCoroutine = StartCoroutine(SpeedRoutine(multiplier, duration));
    }

    public void Stun(float duration)
    {
        if (!IsServer) {
            Debug.LogWarning("Stun should only be called on the server!");
            return;
        }

        if (stunCoroutine != null) {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator SpeedRoutine(float multiplier, float duration)
    {
        speedMultiplier.Value = multiplier;

        string effectType = multiplier > 1f ? "Speed Boost" : "Slow";
        Debug.Log($"[Server] {gameObject.name} - {effectType} applied: {multiplier}x for {duration}s");

        yield return new WaitForSeconds(duration);

        speedMultiplier.Value = 1f;
        Debug.Log($"[Server] {gameObject.name} - Speed effect ended");

        speedCoroutine = null;
    }

    private IEnumerator StunRoutine(float duration)
    {
        canMove.Value = false;
        Debug.Log($"[Server] {gameObject.name} - Stunned for {duration}s");

        yield return new WaitForSeconds(duration);

        canMove.Value = true;
        Debug.Log($"[Server] {gameObject.name} - Stun ended");

        stunCoroutine = null;
    }

    public bool CanMove()
    {
        return canMove.Value;
    }

    public float GetSpeedMultiplier()
    {
        return speedMultiplier.Value;
    }

    public bool IsStunned()
    {
        return !canMove.Value;
    }

    public bool HasSpeedEffect()
    {
        return speedMultiplier.Value != 1f;
    }
}

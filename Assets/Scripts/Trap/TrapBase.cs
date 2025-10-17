using UnityEngine;
using System.Collections;
using Unity.Netcode;

public abstract class TrapBase : NetworkBehaviour
{
    [Header("TrapBase Settings")]
    public bool canRearm = false;
    public float rearmDelay = 5f;

    private NetworkVariable<bool> hasTriggered = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) {
            hasTriggered.OnValueChanged += OnTrapStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsServer) {
            hasTriggered.OnValueChanged -= OnTrapStateChanged;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (hasTriggered.Value)
            return;

        if (other.tag != "Child")
            return;

        NetworkChildrenController player = other.GetComponent<NetworkChildrenController>();

        if (player != null) {
            hasTriggered.Value = true;
            ActivateTrap(player);

            OnTrapActivatedClientRpc(player.NetworkObjectId);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!IsServer)
            return;

        if (other.tag != "Child")
            return;

        if (hasTriggered.Value && canRearm) {
            StartCoroutine(RearmTrap());
        }
    }

    private IEnumerator RearmTrap() {
        yield return new WaitForSeconds(rearmDelay);

        if (IsServer) {
            hasTriggered.Value = false;
            OnRearmed();

            OnTrapRearmedClientRpc();
        }
    }

    protected virtual void OnRearmed() {
        Debug.Log($"[Server] {gameObject.name} rearmed");
    }

    protected abstract void ActivateTrap(NetworkChildrenController child);

    private void OnTrapStateChanged(bool oldValue, bool newValue) {
        if (newValue) {
            OnTrapActivatedVisual();
        }
        else {
            OnTrapRearmedVisual();
        }
    }

    [ClientRpc]
    private void OnTrapActivatedClientRpc(ulong childNetworkId)
    {
        if (IsServer)
            return;

        OnTrapActivatedVisual();
    }

    [ClientRpc]
    private void OnTrapRearmedClientRpc()
    {
        if (IsServer)
            return;

        OnTrapRearmedVisual();
    }

    protected virtual void OnTrapActivatedVisual()
    {
        Debug.Log($"[Client] {gameObject.name} activated (visual)");
    }

    protected virtual void OnTrapRearmedVisual()
    {
        Debug.Log($"[Client] {gameObject.name} rearmed (visual)");
    }

    public bool IsTriggered()
    {
        return hasTriggered.Value;
    }
}

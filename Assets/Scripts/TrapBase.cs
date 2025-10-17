using UnityEngine;
using System.Collections;

public abstract class TrapBase : MonoBehaviour {
    [Header("TrapBase Settings")]
    public bool canRearm = false;
    public float rearmDelay = 5f;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other) {
        if (hasTriggered)
           return;

        if (other.tag != "Child")
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null) {
            hasTriggered = true;
            ActivateTrap(player);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.tag != "Child")
            return;

        if (hasTriggered && canRearm)
            StartCoroutine(RearmTrap());
    }

    private IEnumerator RearmTrap() {
        yield return new WaitForSeconds(rearmDelay);
        hasTriggered = false;
        OnRearmed();
    }

    protected virtual void OnRearmed() {}
    protected abstract void ActivateTrap(PlayerController player);
}

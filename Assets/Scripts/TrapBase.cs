using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    public bool canRearm = false;
    public float rearmDelay = 5f;
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other) {
        if (hasTriggered)
           return;

        if (other.tag != "Child")
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null) {
            hasTriggered = true;
            ActivateTrap(player);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.tag != "Child" || !canRearm)
            return;

        if (hasTriggered)
            StartCoroutine(RearmTrap());
    }

    private IEnumerator RearmTrap() {
        yield return new WaitForSeconds(rearmDelay);
        hasTriggered = false;
        OnRearmed();
    }

    protected virtual void OnRearmed() {}
    protected abstract void ActivateTrap(PlayerMovement player);
}

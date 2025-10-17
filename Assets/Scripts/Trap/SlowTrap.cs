using UnityEngine;
using System.Collections;

public class SlowTrap : TrapBase
{
    [Header("Slow Settings")]
    [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;
    public float slowDuration = 5f;

    protected override void ActivateTrap(NetworkChildrenController child)
    {
        if (child == null)
            return;

        ChildrenManager manager = child.GetComponent<ChildrenManager>();

        if (manager != null) {
            manager.Slow(slowMultiplier, slowDuration);
            Debug.Log($"[Server] {child.name} slowed to {slowMultiplier * 100}% for {slowDuration}s");
        }
    }
}

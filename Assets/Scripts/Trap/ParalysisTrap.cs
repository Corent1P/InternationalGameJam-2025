using UnityEngine;
using System.Collections;

public class ParalysisTrap : TrapBase
{
    [Header("Paralysis Settings")]
    public float paralysisDuration = 3f;

    protected override void ActivateTrap(NetworkChildrenController child)
    {
        ChildrenManager manager = child.GetComponent<ChildrenManager>();

        if (manager != null) {
            manager.Stun(paralysisDuration);
            Debug.Log($"[Server] {child.name} paralyzed for {paralysisDuration}s");
        }
    }
}

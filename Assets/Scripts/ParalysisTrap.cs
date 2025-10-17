using UnityEngine;
using System.Collections;

public class ParalysisTrap : TrapBase {
    [Header("Paralysis Settings")]
    public float paralysisDuration = 3f;

    protected override void ActivateTrap(PlayerController player)
    {
        StartCoroutine(Paralyze(player));
    }

    private IEnumerator Paralyze(PlayerController player)
    {
        player.CanMove = false;
        yield return new WaitForSeconds(paralysisDuration);
        player.CanMove = true;
    }
}

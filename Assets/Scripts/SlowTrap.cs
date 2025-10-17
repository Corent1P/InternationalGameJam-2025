using UnityEngine;
using System.Collections;

public class SlowTrap : TrapBase
{
    [Header("Slow Settings")]
    [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;
    public float slowDuration = 5f;

    protected override void ActivateTrap(PlayerController player)
    {
        if (player == null)
            return;

        Debug.Log($"{player.name} hit a slow trap ! Speed reduced to {slowMultiplier * 100}% for {slowDuration} seconds.");
        player.ApplySlow(slowMultiplier, slowDuration);
    }

    protected override void OnRearmed()
    {
        Debug.Log($"{name} is ready to slow another player!");
    }
}

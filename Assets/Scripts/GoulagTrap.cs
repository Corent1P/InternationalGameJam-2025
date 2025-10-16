using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GoulagTrap : TrapBase {
    [Header("Goulag Settings")]
    public Transform goulagSpawnPoint;
    public Transform releaseSpawnPoint;

    private List<PlayerController> trappedPlayers = new List<PlayerController>();

    protected override void ActivateTrap(PlayerController player) {
        if (goulagSpawnPoint == null) {
            Debug.LogWarning("No goulag spawn !");
            return;
        }

        if (trappedPlayers.Contains(player)) {
            Debug.Log($"{player.name} already in the goulag !");
            return;
        }

        trappedPlayers.Add(player);
        player.transform.position = goulagSpawnPoint.position;
        player.transform.rotation = goulagSpawnPoint.rotation;

        Debug.Log($"{player.name} has been send to the goulag !");
    }

    public void ReleaseAllPlayers() {
        if (trappedPlayers.Count == 0) {
            Debug.Log("No player to free !");
            return;
        }

        foreach (PlayerController player in trappedPlayers) {
            if (player == null)
                continue;

            if (releaseSpawnPoint != null) {
                player.transform.position = releaseSpawnPoint.position;
                player.transform.rotation = releaseSpawnPoint.rotation;
            }

            Debug.Log($"{player.name} is now free !");
        }

        trappedPlayers.Clear();
    }

    protected override void OnRearmed() {
        Debug.Log($"{name} is re armed !");
    }
}

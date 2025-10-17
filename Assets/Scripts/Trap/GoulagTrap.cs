using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GoulagTrap : TrapBase
{
    [Header("Goulag Settings")]
    public Transform goulagSpawnPoint;
    public Transform releaseSpawnPoint;

    private List<NetworkChildrenController> trappedPlayers = new List<NetworkChildrenController>();

    protected override void ActivateTrap(NetworkChildrenController child) {
        if (goulagSpawnPoint == null) {
            Debug.LogWarning("No goulag spawn!");
            return;
        }

        if (trappedPlayers.Contains(child)) {
            Debug.Log($"{child.name} already in the goulag!");
            return;
        }

        trappedPlayers.Add(child);
        child.transform.position = goulagSpawnPoint.position;
        child.transform.rotation = goulagSpawnPoint.rotation;

        Debug.Log($"{child.name} has been sent to the goulag!");
    }

    public void ReleaseAllPlayers() {
        if (trappedPlayers.Count == 0) {
            Debug.Log("No player to free!");
            return;
        }

        foreach (NetworkChildrenController child in trappedPlayers) {
            if (child == null)
                continue;

            if (releaseSpawnPoint != null) {
                child.transform.position = releaseSpawnPoint.position;
                child.transform.rotation = releaseSpawnPoint.rotation;
            }

            Debug.Log($"{child.name} is now free!");
        }

        trappedPlayers.Clear();
    }

    protected override void OnRearmed() {
        Debug.Log($"{name} is re-armed!");
    }
}

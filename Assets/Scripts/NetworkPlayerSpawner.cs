using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject childrenPlayerPrefab;
    [SerializeField] private GameObject adultPlayerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool randomizeSpawnPoints = true;

    private List<Transform> availableSpawnPoints = new List<Transform>();

    private void Start()
    {
        if (!IsServer) return;

        // Initialiser la liste des spawn points disponibles
        ResetSpawnPoints();
    }

    public void SpawnPlayerForClient(ulong clientId, Team team)
    {
        // Obtenir une position de spawn
        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();

        // Instancier le joueur
        GameObject playerInstance = Instantiate(team == Team.Children ? childrenPlayerPrefab : adultPlayerPrefab, spawnPosition, spawnRotation);
        playerInstance.tag = (team == Team.Children) ? "Child" : "Adult";
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("Player prefab doesn't have a NetworkObject component!");
            Destroy(playerInstance);
            return;
        }

        // Spawn le NetworkObject et lui assigner la propriété au client
        networkObject.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"Player spawned for client {clientId} at position {spawnPosition}");
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points defined, using default position");
            return Vector3.zero;
        }

        Transform spawnPoint;

        if (randomizeSpawnPoints && availableSpawnPoints.Count > 0)
        {
            // Choisir un spawn point aléatoire parmi ceux disponibles
            int randomIndex = Random.Range(0, availableSpawnPoints.Count);
            spawnPoint = availableSpawnPoints[randomIndex];
            availableSpawnPoints.RemoveAt(randomIndex);
        }
        else
        {
            // Utiliser le prochain spawn point dans l'ordre
            int index = spawnPoints.Length - availableSpawnPoints.Count;
            if (index >= spawnPoints.Length)
            {
                // Si on a plus de joueurs que de spawn points, reset
                ResetSpawnPoints();
                index = 0;
            }
            spawnPoint = spawnPoints[index];
        }

        return spawnPoint.position;
    }

    private Quaternion GetSpawnRotation()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Quaternion.identity;

        // Utiliser la rotation du spawn point si disponible
        int currentIndex = spawnPoints.Length - availableSpawnPoints.Count - 1;
        if (currentIndex >= 0 && currentIndex < spawnPoints.Length)
        {
            return spawnPoints[currentIndex].rotation;
        }

        return Quaternion.identity;
    }

    public void ResetSpawnPoints()
    {
        availableSpawnPoints.Clear();
        if (spawnPoints != null)
        {
            availableSpawnPoints.AddRange(spawnPoints);
        }
    }
}
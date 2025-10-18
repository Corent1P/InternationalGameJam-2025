using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class RoundManager : NetworkBehaviour
{
    [Header("Phase Durations")]
    [SerializeField] private float preparationPhaseDuration = 60f;
    [SerializeField] private float roundDuration = 300f;

    [Header("Game Objects")]
    [SerializeField] private GameObject shoppingManager;
    [SerializeField] private GameObject houseFences;
    [SerializeField] private GameObject candySpawner;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] childrenSpawnPoints;
    [SerializeField] private Transform adultSpawnPoint;
    [SerializeField] private Transform candySpawnPoint;

    private GameObject adultPlayer;
    private List<GameObject> childPlayers = new List<GameObject>();
    private bool hasBeenFinishedEarlier = false;

    #region Phase Management

    public void StartPreparationPhase()
    {
        if (!IsServer) return;

        Debug.Log("Starting Preparation Phase...");
        
        // Réinitialiser l'état du round
        ResetRound();

        // Activer les barrières
        if (houseFences != null)
        {
            houseFences.SetActive(true);
            ActivateFencesClientRpc(true);
        }

        // Activer le shopping manager
        if (shoppingManager != null)
        {
            shoppingManager.SetActive(true);
            ActivateShoppingClientRpc(true);
        }

        // Spawner les bonbons
        if (candySpawner != null)
        {
            candySpawner.SetActive(true);
            ActivateCandySpawnerClientRpc(true);
        }

        // Téléporter les joueurs aux spawn points
        TeleportPlayersToSpawnPoints();

        // Notifier les joueurs qu'ils sont en phase de préparation
        if (adultPlayer != null)
        {
            adultPlayer.GetComponent<AdultManager>().SetPreparationPhase(true);
        }

        foreach (var child in childPlayers)
        {
            if (child != null)
            {
                child.GetComponent<ChildrenManager>().SetPreparationPhase(true);
            }
        }

        Debug.Log("Preparation phase active. Players can shop and prepare.");
    }

    public void StartGamePhase()
    {
        if (!IsServer) return;

        Debug.Log("Starting Game Phase...");

        // Désactiver les barrières
        if (houseFences != null)
        {
            houseFences.SetActive(false);
            ActivateFencesClientRpc(false);
        }

        // Désactiver le shopping manager
        if (shoppingManager != null)
        {
            shoppingManager.SetActive(false);
            ActivateShoppingClientRpc(false);
        }

        // Notifier les joueurs que la phase de jeu commence
        if (adultPlayer != null)
        {
            adultPlayer.GetComponent<AdultManager>().SetPreparationPhase(false);
        }

        foreach (var child in childPlayers)
        {
            if (child != null)
            {
                child.GetComponent<ChildrenManager>().SetPreparationPhase(false);
            }
        }

        Debug.Log("Game phase active. Fences are down, hunting begins!");
    }

    public void EndRound()
    {
        if (!IsServer) return;

        Debug.Log("Ending round...");

        // Désactiver le spawner de bonbons
        if (candySpawner != null)
        {
            candySpawner.SetActive(false);
            ActivateCandySpawnerClientRpc(false);
        }

        // Optionnel : figer les joueurs ou désactiver leurs contrôles
        // pour éviter qu'ils continuent de jouer pendant le décompte des points
    }

    private void ResetRound()
    {
        hasBeenFinishedEarlier = false;
        
        // Réinitialiser les états si nécessaire
        Debug.Log("Round reset complete.");
    }

    #endregion

    #region Player Management

    private void TeleportPlayersToSpawnPoints()
    {
        // Téléporter l'adulte
        if (adultPlayer != null && adultSpawnPoint != null)
        {
            // Obtenir le NetworkObject de l'adulte
            NetworkObject adultNetObj = adultPlayer.GetComponent<NetworkObject>();
            if (adultNetObj != null)
            {
                TeleportPlayerClientRpc(adultNetObj.NetworkObjectId, adultSpawnPoint.position, adultSpawnPoint.rotation);
            }
        }

        // Téléporter les enfants
        if (childPlayers != null && childrenSpawnPoints != null && childrenSpawnPoints.Length > 0)
        {
            for (int i = 0; i < childPlayers.Count; i++)
            {
                if (childPlayers[i] != null)
                {
                    // Utiliser un spawn point cyclique
                    Transform spawnPoint = childrenSpawnPoints[i % childrenSpawnPoints.Length];
                    
                    NetworkObject childNetObj = childPlayers[i].GetComponent<NetworkObject>();
                    if (childNetObj != null && spawnPoint != null)
                    {
                        TeleportPlayerClientRpc(childNetObj.NetworkObjectId, spawnPoint.position, spawnPoint.rotation);
                    }
                }
            }
        }
    }

    public void SetAdultPlayer(GameObject adult)
    {
        adultPlayer = adult;
    }

    public void SetChildPlayers(List<GameObject> children)
    {
        childPlayers = children;
    }

    #endregion

    #region Win Conditions

    public bool AdultHasWon()
    {
        if (childPlayers == null || childPlayers.Count == 0)
            return false;

        // L'adulte gagne si tous les enfants sont attrapés
        foreach (var child in childPlayers)
        {
            if (child != null && !child.GetComponent<ChildrenManager>().isCaught())
            {
                return false;
            }
        }
        
        return true;
    }

    public bool HasFinishedEarlier()
    {
        return hasBeenFinishedEarlier;
    }

    public void SetFinishedEarlier(bool value)
    {
        hasBeenFinishedEarlier = value;
    }

    #endregion

    #region Getters & Setters

    public float GetPreparationPhaseDuration()
    {
        return preparationPhaseDuration;
    }

    public float GetRoundDuration()
    {
        return roundDuration;
    }

    public void SetAdultSpawnPoint(Transform point)
    {
        adultSpawnPoint = point;
    }

    public void SetChildrenSpawnPoints(Transform[] points)
    {
        childrenSpawnPoints = points;
    }

    public void SetCandySpawnPoint(Transform point)
    {
        candySpawnPoint = point;
    }

    #endregion

    #region Network RPCs

    [ClientRpc]
    private void ActivateFencesClientRpc(bool active)
    {
        if (houseFences != null)
        {
            houseFences.SetActive(active);
        }
        Debug.Log($"[CLIENT] Fences {(active ? "activated" : "deactivated")}");
    }

    [ClientRpc]
    private void ActivateShoppingClientRpc(bool active)
    {
        if (shoppingManager != null)
        {
            shoppingManager.SetActive(active);
        }
        Debug.Log($"[CLIENT] Shopping {(active ? "activated" : "deactivated")}");
    }

    [ClientRpc]
    private void ActivateCandySpawnerClientRpc(bool active)
    {
        if (candySpawner != null)
        {
            candySpawner.SetActive(active);
        }
        Debug.Log($"[CLIENT] Candy spawner {(active ? "activated" : "deactivated")}");
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(ulong networkObjectId, Vector3 position, Quaternion rotation)
    {
        // Trouver le NetworkObject correspondant
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            // Téléporter le joueur
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;

            // Si c'est un joueur avec un Rigidbody, réinitialiser la vélocité
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[CLIENT] Player teleported to {position}");
        }
    }

    #endregion
}

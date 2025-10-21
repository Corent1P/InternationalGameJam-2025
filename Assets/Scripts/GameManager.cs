using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using Unity.Services.Matchmaker.Models;

public enum Team
{
    Children,
    Adults
}

public enum GameState
{
    WaitingForPlayers,
    PreparationPhase,
    GamePhase,
    RoundEnd,
    GameEnd
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NetworkPlayerSpawner playerSpawner;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private GameObject temporaryCamera;
    [SerializeField] private GameObject hudCanvas; // Canvas du HUD à activer au début de la partie

    [Header("Game Data")]
    public Dictionary<ulong, PlayerData> playerStatesByID = new();

    [Header("Game Settings")]
    [SerializeField] private int numberRounds = 3;
    [SerializeField] private float timeBetweenRounds = 10f;
    [SerializeField] private float waitingTimeForJoiningPlayers = 15f;

    private AdultManager adultPlayer;
    private List<GameObject> childPlayers = new List<GameObject>();
    private int currentRound = 0;
    private bool hasBeenSpawned = false;

    private NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(
        GameState.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Timer synchronisé sur le réseau
    private NetworkVariable<float> phaseRemainingTime = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float phaseStartTime;
    private float currentPhaseDuration;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            Debug.Log("GameManager spawned on client.");

            // S'abonner aux changements d'état pour tous les clients
            currentGameState.OnValueChanged += OnGameStateChanged;
            return;
        }

        Debug.Log("GameManager spawned on server.");

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        StartCoroutine(WaitForAllPlayersAndAssignTeams());
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsServer && currentGameState != null)
        {
            currentGameState.OnValueChanged -= OnGameStateChanged;
        }

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region Player Connection & Team Assignment

    private IEnumerator WaitForAllPlayersAndAssignTeams()
    {
        int expectedPlayers = PlayerPrefs.GetInt("MaxPlayers", 4);
        float timeout = waitingTimeForJoiningPlayers;
        float elapsed = 0f;

        Debug.Log($"Waiting for {expectedPlayers} players to connect...");

        while (NetworkManager.Singleton.ConnectedClientsIds.Count < expectedPlayers && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        Debug.Log($"Player wait finished. {NetworkManager.Singleton.ConnectedClientsIds.Count}/{expectedPlayers} players connected.");

        AssignTeamsAndSpawnPlayers();
        hasBeenSpawned = true;

        // Démarrer le jeu après l'assignation des équipes
        yield return new WaitForSeconds(2f); // Petit délai pour que tout soit prêt
        StartGame();
    }

    private void AssignTeamsAndSpawnPlayers()
    {
        List<ulong> allClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);

        // Choisir aléatoirement un adulte
        // int adultIndex = Random.Range(0, allClients.Count);
        int adultIndex = allClients.Count - 1;
        GameObject go = null;

        Debug.Log($"Spawning {allClients.Count} players. Adult index: {adultIndex}");

        DisableTemporaryCameraClientRpc();
        for (int i = 0; i < allClients.Count; i++)
        {
            Team team = (i == adultIndex) ? Team.Adults : Team.Children;
            go = playerSpawner.SpawnPlayerForClient(allClients[i], team);

            if (go != null)
            {
                if (team == Team.Adults)
                {
                    roundManager.SetAdultPlayer(go);
                    Debug.Log($"Adult player assigned (Client {allClients[i]})");
                }
                else
                {
                    childPlayers.Add(go);
                    Debug.Log($"Child player added (Client {allClients[i]})");
                }
            }
        }

        roundManager.SetChildPlayers(childPlayers);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Client {clientId} connected to game.");

        if (!NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject && hasBeenSpawned)
        {
            // Joueur rejoint après le début, spawn en tant qu'enfant par défaut
            GameObject go = playerSpawner.SpawnPlayerForClient(clientId, Team.Children);

            if (go != null)
            {
                childPlayers.Add(go);
                roundManager.SetChildPlayers(childPlayers);
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Client {clientId} disconnected from game");
        if (playerStatesByID.ContainsKey(clientId))
        {
            playerStatesByID.Remove(clientId);
        }

        playerSpawner.ResetSpawnPoints();
    }

    #endregion

    #region Game Flow

    private void StartGame()
    {
        if (!IsServer) return;

        currentRound = 1;
        Debug.Log("=== GAME STARTING ===");
        
        // Activer le HUD pour tous les clients
        ActivateHUDClientRpc();
        
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (currentRound <= numberRounds)
        {
            Debug.Log($"=== STARTING ROUND {currentRound}/{numberRounds} ===");

            // Lancer le round
            yield return StartCoroutine(PlayRound());

            currentRound++;

            // Si ce n'est pas le dernier round, attendre entre les rounds
            if (currentRound <= numberRounds)
            {
                currentGameState.Value = GameState.RoundEnd;
                NotifyRoundEndClientRpc(currentRound - 1);

                Debug.Log($"Round {currentRound - 1} finished. Waiting {timeBetweenRounds}s before next round...");
                
                // Mettre à jour le timer pendant la pause entre les rounds
                currentPhaseDuration = timeBetweenRounds;
                phaseRemainingTime.Value = timeBetweenRounds;
                float elapsedBreak = 0f;
                
                while (elapsedBreak < timeBetweenRounds)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedBreak += 0.1f;
                    phaseRemainingTime.Value = Mathf.Max(0, timeBetweenRounds - elapsedBreak);
                }
            }
        }

        // Fin du jeu
        EndGame();
    }

    private IEnumerator PlayRound()
    {
        // Phase de préparation
        currentGameState.Value = GameState.PreparationPhase;
        Debug.Log("=== PREPARATION PHASE ===");

        currentPhaseDuration = roundManager.GetPreparationPhaseDuration();
        phaseStartTime = Time.time;
        phaseRemainingTime.Value = currentPhaseDuration;

        roundManager.StartPreparationPhase();
        NotifyPreparationPhaseClientRpc();

        // Mise à jour du timer pendant la phase de préparation
        float elapsedPrep = 0f;
        while (elapsedPrep < currentPhaseDuration)
        {
            yield return new WaitForSeconds(0.1f);
            elapsedPrep += 0.1f;
            phaseRemainingTime.Value = Mathf.Max(0, currentPhaseDuration - elapsedPrep);
        }

        // Phase de jeu
        currentGameState.Value = GameState.GamePhase;
        Debug.Log("=== GAME PHASE ===");

        currentPhaseDuration = roundManager.GetRoundDuration();
        phaseStartTime = Time.time;
        phaseRemainingTime.Value = currentPhaseDuration;

        roundManager.StartGamePhase();
        NotifyGamePhaseClientRpc();

        // Surveiller si l'adulte gagne en cours de partie
        float elapsedTime = 0f;
        float roundDuration = roundManager.GetRoundDuration();
        bool adultWonEarly = false;

        while (elapsedTime < roundDuration)
        {
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
            phaseRemainingTime.Value = Mathf.Max(0, roundDuration - elapsedTime);

            if (roundManager.AdultHasWon())
            {
                Debug.Log("Adult won early!");
                adultWonEarly = true;
                roundManager.SetFinishedEarlier(true);
                phaseRemainingTime.Value = 0f;
                break;
            }
        }

        if (!adultWonEarly)
        {
            roundManager.SetFinishedEarlier(false);
        }

        // Fin du round
        Debug.Log($"=== ROUND {currentRound} ENDED ===");
        roundManager.EndRound();

        // Calculer les récompenses
        ComputeRewards();
    }

    private void EndGame()
    {
        currentGameState.Value = GameState.GameEnd;
        phaseRemainingTime.Value = 0f;
        Debug.Log("=== GAME ENDED ===");

        NotifyGameEndClientRpc();

        // Afficher les résultats finaux, retourner au lobby, etc.
    }

    #endregion

    #region Rewards

    private void ComputeRewards()
    {
        if (!IsServer) return;

        Debug.Log("Computing rewards...");

        ComputeAdultCoins();
        ComputeChildrenCoins();
    }

    private void ComputeAdultCoins()
    {
        if (adultPlayer != null)
        {
            int coinsEarned = numberRounds * 100;

            foreach (var child in childPlayers)
            {
                if (child != null)
                {
                    coinsEarned -= child.GetComponent<ChildrenManager>().getCandy() * 10;
                    coinsEarned += child.GetComponent<ChildrenManager>().getNumberCaught() * 50;
                }
            }

            if (roundManager.HasFinishedEarlier())
            {
                coinsEarned += 200;
            }

            adultPlayer.SetCoins(coinsEarned);
            Debug.Log($"Adult earned {coinsEarned} coins");
        }
    }

    private void ComputeChildrenCoins()
    {
        if (childPlayers != null)
        {
            bool hasBeenFinishedEarlier = roundManager.HasFinishedEarlier();

            foreach (var child in childPlayers)
            {
                if (child != null)
                {
                    int coinsEarned = numberRounds * 50 + child.GetComponent<ChildrenManager>().getCandy() * 20;

                    if (hasBeenFinishedEarlier)
                    {
                        coinsEarned /= 2;
                    }

                    child.GetComponent<ChildrenManager>().SetCoins(coinsEarned);
                    Debug.Log($"Child earned {coinsEarned} coins");
                }
            }
        }
    }

    #endregion

    #region Network RPCs

    [ClientRpc]
    private void NotifyPreparationPhaseClientRpc()
    {
        Debug.Log("[CLIENT] Preparation phase started");
        // Mettre à jour l'UI, afficher le timer, etc.
    }

    [ClientRpc]
    private void NotifyGamePhaseClientRpc()
    {
        Debug.Log("[CLIENT] Game phase started");
        // Mettre à jour l'UI, cacher le shop, etc.
    }

    [ClientRpc]
    private void NotifyRoundEndClientRpc(int roundNumber)
    {
        Debug.Log($"[CLIENT] Round {roundNumber} ended");
        // Afficher les scores du round
    }

    [ClientRpc]
    private void NotifyGameEndClientRpc()
    {
        Debug.Log("[CLIENT] Game ended");
        // Afficher le scoreboard final

        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        Debug.Log($"[CLIENT] Game state changed: {oldState} -> {newState}");
        // Réagir aux changements d'état côté client
    }

    [ClientRpc]
    private void DisableTemporaryCameraClientRpc()
    {
        if (temporaryCamera != null)
        {
            temporaryCamera.SetActive(false);
            Debug.Log("[CLIENT] Temporary camera disabled");
        }
    }

    [ClientRpc]
    private void ActivateHUDClientRpc()
    {
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(true);
            Debug.Log("[CLIENT] HUD Canvas activated");
        }
        else
        {
            Debug.LogWarning("[CLIENT] HUD Canvas reference is missing!");
        }
    }

    #endregion

    #region Player Data Management

    public void RegisterPlayerData(ulong clientId, PlayerData data)
    {
        if (!IsServer) return;

        playerStatesByID[clientId] = data;
        Debug.Log($"Registered player data for client {clientId}");
    }

    public PlayerData GetPlayerData(ulong clientId)
    {
        if (playerStatesByID.TryGetValue(clientId, out PlayerData data))
        {
            return data;
        }
        return null;
    }

    public GameState GetCurrentGameState()
    {
        return currentGameState.Value;
    }

    public int GetCurrentRound()
    {
        return currentRound;
    }

    public int GetTotalRounds()
    {
        return numberRounds;
    }

    public RoundManager GetRoundManager()
    {
        return roundManager;
    }

    public float GetPhaseRemainingTime()
    {
        return phaseRemainingTime.Value;
    }

    #endregion
}

[System.Serializable]
public class PlayerData
{
    public ulong ID;
    public Vector3 Position;

    public PlayerData(ulong id, Vector3 position)
    {
        ID = id;
        Position = position;
    }
}
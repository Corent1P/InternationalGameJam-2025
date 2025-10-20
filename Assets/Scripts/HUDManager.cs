using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Common UI (Global Info)")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Player Specific UI")]
    [SerializeField] private GameObject coinsUI; // GameObject contenant l'icône + texte des coins (Adult)
    [SerializeField] private TextMeshProUGUI coinsText; // Texte pour afficher le nombre de coins
    [SerializeField] private GameObject candiesUI; // GameObject contenant l'icône + texte des candies (Child)
    [SerializeField] private TextMeshProUGUI candiesText; // Texte pour afficher le nombre de candies

    private NetworkObject localPlayer;
    private bool isAdult = false;
    private AdultManager adultManager;
    private ChildrenManager childrenManager;

    private void Start()
    {
        // Attendre que le joueur local soit spawné
        StartCoroutine(WaitForLocalPlayer());
    }

    private System.Collections.IEnumerator WaitForLocalPlayer()
    {
        // Attendre que le NetworkManager soit prêt
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Attendre que le joueur local soit spawné
        while (NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        Debug.Log($"[HUDManager] Local player found: {localPlayer.name}");

        // Détecter si le joueur est Adult ou Child
        DetectPlayerType();

        // Initialiser l'UI
        InitializeUI();
    }

    private void DetectPlayerType()
    {
        adultManager = localPlayer.GetComponent<AdultManager>();
        childrenManager = localPlayer.GetComponent<ChildrenManager>();

        if (adultManager != null)
        {
            isAdult = true;
            Debug.Log("[HUDManager] Local player is an ADULT");
        }
        else if (childrenManager != null)
        {
            isAdult = false;
            Debug.Log("[HUDManager] Local player is a CHILD");
        }
        else
        {
            Debug.LogError("[HUDManager] Could not determine player type!");
        }
    }

    private void InitializeUI()
    {
        // Activer/désactiver les UI de ressources selon le type de joueur
        if (coinsUI != null) 
        {
            coinsUI.SetActive(isAdult);
            Debug.Log($"[HUDManager] Coins UI {(isAdult ? "activated" : "deactivated")}");
        }
        
        if (candiesUI != null) 
        {
            candiesUI.SetActive(!isAdult);
            Debug.Log($"[HUDManager] Candies UI {(!isAdult ? "activated" : "deactivated")}");
        }

        // Initialiser les valeurs
        UpdatePlayerResources();
    }

    private void Update()
    {
        if (localPlayer == null) return;

        // Mettre à jour les infos du joueur (coins ou candies)
        UpdatePlayerResources();

        // Mettre à jour les infos globales (timer, phase, round)
        UpdateGlobalInfo();
    }

    private void UpdatePlayerResources()
    {
        if (isAdult && adultManager != null)
        {
            // Afficher les coins de l'adulte
            int coins = adultManager.GetCoins();
            
            if (coinsText != null)
            {
                coinsText.text = coins.ToString();
            }
        }
        else if (!isAdult && childrenManager != null)
        {
            // Afficher les candies de l'enfant
            int candies = childrenManager.GetCandyCount();
            
            if (candiesText != null)
            {
                candiesText.text = candies.ToString();
            }
        }
    }

    private void UpdateGlobalInfo()
    {
        if (GameManager.Instance == null) return;

        // Mettre à jour la phase de jeu
        if (phaseText != null)
        {
            GameState currentState = GameManager.Instance.GetCurrentGameState();
            phaseText.text = GetPhaseText(currentState);
        }

        // Mettre à jour le round
        if (roundText != null)
        {
            int currentRound = GameManager.Instance.GetCurrentRound();
            int totalRounds = GameManager.Instance.GetTotalRounds();
            roundText.text = $"Round: {currentRound}/{totalRounds}";
        }

        // Mettre à jour le timer
        if (timerText != null)
        {
            float remainingTime = GameManager.Instance.GetPhaseRemainingTime();
            timerText.text = FormatTime(remainingTime);
        }
    }

    private string GetPhaseText(GameState state)
    {
        switch (state)
        {
            case GameState.WaitingForPlayers:
                return "Waiting for players...";
            case GameState.PreparationPhase:
                return "Preparation Phase";
            case GameState.GamePhase:
                return "Game Phase";
            case GameState.RoundEnd:
                return "Round Ended";
            case GameState.GameEnd:
                return "Game Over";
            default:
                return "Unknown";
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds <= 0)
        {
            return "00:00";
        }

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        
        // Afficher en rouge si moins de 30 secondes (nécessite TextMeshPro rich text)
        if (timeInSeconds <= 30f && timeInSeconds > 0f)
        {
            return $"<color=red>{minutes:00}:{seconds:00}</color>";
        }
        
        return $"{minutes:00}:{seconds:00}";
    }

}

using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using System;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using System.Collections.Generic;
using UnityEngine.UI;

public class UnityPlayerAuth : MonoBehaviour
{
    [SerializeField] private Button loginButton;
    [SerializeField] private MenuManager menuManager;

    public event Action<PlayerInfo, string> OnSingedIn;
    public event Action<String> OnUpdateName;
    private PlayerInfo playerInfo;

    void OnEnable()
    {
        loginButton?.onClick.AddListener(LoginButton);
    }

    void OnDisable()
    {
        loginButton?.onClick.RemoveListener(LoginButton);
    }

    private async void LoginButton()
    {
        await InitSignIn();
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        SetupEvents();
        PlayerAccountService.Instance.SignedIn += SignIn;

        // Vérifier si déjà connecté au démarrage
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Already signed in");
            await HandleAlreadySignedIn();
        }
    }

    private void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Player ID " + AuthenticationService.Instance.PlayerId);
            Debug.Log("Access Token " + AuthenticationService.Instance.AccessToken);
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.Log(err);
        };
        
        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player log out");
        };
        
        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Player session expired");
        };
    }

    private async Task HandleAlreadySignedIn()
    {
        try
        {
            playerInfo = AuthenticationService.Instance.PlayerInfo;
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();
            
            OnSingedIn?.Invoke(playerInfo, name);
            menuManager?.ShowPlayMenu();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error handling already signed in state: " + ex.Message);
        }
    }

    public async Task InitSignIn()
    {
        // Si déjà connecté, ne rien faire ou gérer directement
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Already signed in, skipping sign in process");
            await HandleAlreadySignedIn();
            return;
        }

        await PlayerAccountService.Instance.StartSignInAsync();
    }

    private async void SignIn()
    {
        // Vérifier si déjà connecté avant de tenter la connexion
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Already signed in during SignIn callback");
            await HandleAlreadySignedIn();
            return;
        }

        try
        {
            await SignInWithUnityAuth();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    private async Task SignInWithUnityAuth()
    {
        try
        {
            string accessToken = PlayerAccountService.Instance.AccessToken;
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
            
            Debug.Log("Login Successful");
            playerInfo = AuthenticationService.Instance.PlayerInfo;
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();

            OnSingedIn?.Invoke(playerInfo, name);
            Debug.Log("Sign In Successful");
            menuManager?.ShowPlayMenu();
        }
        catch (AuthenticationException ex)
        {   
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.Log(ex);
        }
    }

    public async Task UpdateName(string newName)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Cannot update name: not signed in");
            return;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();
            OnUpdateName?.Invoke(name);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error updating name: " + ex.Message);
        }
    }

    public async Task DeleteAccountUnityAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Cannot delete account: not signed in");
            return;
        }

        try
        {
            await AuthenticationService.Instance.DeleteAccountAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error deleting account: " + ex.Message);
            throw;
        }
    }

    // Cloud Save

    public async void SaveData(string key, string value)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Cannot save data: not signed in");
            return;
        }

        try
        {
            var playerData = new Dictionary<string, object>()
            {
                {key, value}
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(playerData);
            Debug.Log($"Data saved successfully: {key}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error saving data: " + ex.Message);
        }
    }

    public async void LoadData(string key)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Cannot load data: not signed in");
            return;
        }

        try
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(
               new HashSet<string> { key }
            );
            
            if (playerData.TryGetValue(key, out var value))
            {
                Debug.Log(key + " value: " + value.Value.GetAs<String>());
            }
            else
            {
                Debug.Log($"No data found for key: {key}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading data: " + ex.Message);
        }
    }

    public async void DeleteData(string key)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Cannot delete data: not signed in");
            return;
        }

        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
            Debug.Log($"Data deleted successfully: {key}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error deleting data: " + ex.Message);
        }
    }
}
using UnityEngine;

using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using System;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;

using System.Collections.Generic;
using UnityEngine.UI;
// using Sirenix.OdinInspector;
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
    }

    private void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Player ID " + AuthenticationService.Instance.PlayerId);
            Debug.Log("Acces Token " + AuthenticationService.Instance.AccessToken);
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

    //->Lo puedes llamar a traves de un boton
    public async Task InitSignIn()
    {
        await PlayerAccountService.Instance.StartSignInAsync();
    }
    private async void SignIn()
    {
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
            Debug.Log("Login Succ");
            playerInfo = AuthenticationService.Instance.PlayerInfo;
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();

            OnSingedIn?.Invoke(playerInfo, name);
            Debug.Log("Sign In Successful ");
            menuManager.ShowPlayMenu();
        }
        catch (AuthenticationException ex)
        {   
            Debug.LogException(ex);
        }
        catch(RequestFailedException ex)
        {
            Debug.Log(ex);
        }
    }

    public async Task UpdateName(string newName)
    {
        await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
        var name = await AuthenticationService.Instance.GetPlayerNameAsync();

        OnUpdateName?.Invoke(name);
    }
    public async Task DeleteAccountUnityAsync()
    {
        try
        {
            await AuthenticationService.Instance.DeleteAccountAsync();
        }
        catch (Exception)
        {

            throw;
        }
    }

    //-> Cloud Save


    public async void SaveData(string key , string value)
    {
        var playerData = new Dictionary<string, object>()
        {
            {key, value}
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(playerData);
    }

    public async void LoadData(string key)
    {
        var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(
           new HashSet<string> { key } 
            );
        if(playerData.TryGetValue(key, out var value))
        {
            Debug.Log(key + " value : " + value.Value.GetAs<String>());
        }

    }
    public async void DeleteData(string key)
    {
        await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
    }

}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class NameManager : NetworkBehaviour
{
    public TMP_InputField inputField;
    public Button submitButton;

    private void Start()
    {
        submitButton.onClick.AddListener(OnSubmitName);
    }

    public void OnSubmitName()
    {
        string accountID = inputField.text;
        if (!string.IsNullOrEmpty(accountID))
        {
            GameManager5.Instance.RegisterPlayerServerRpc(accountID, NetworkManager.Singleton.LocalClientId);
            submitButton.interactable = false;
            inputField.interactable = false;
        }
    }
}

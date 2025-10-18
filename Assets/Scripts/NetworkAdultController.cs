using UnityEngine;

[RequireComponent(typeof(AdultManager))]
public class NetworkAdultController : NetworkPlayerController
{
    private AdultManager adultManager;

    private void Awake() {
        base.Awake();
        adultManager = GetComponent<AdultManager>();
    }

    public AdultManager GetManager() => adultManager;
}

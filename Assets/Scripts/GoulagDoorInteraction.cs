using UnityEngine;

public class GoulagDoorInteraction : MonoBehaviour {
    [Header("References")]
    public GoulagTrap goulagTrap;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactLayer;

    [Header("Optional Feedback")]
    public string interactionHint = "Press E to open the gulag door";

    private Camera playerCamera;

    private void Start() {
        playerCamera = Camera.main;
    }

    private void Update() {
        if (playerCamera == null || goulagTrap == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactLayer)) {
            if (hit.collider.gameObject == gameObject) {
                Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.green);
                Debug.Log(interactionHint);

                if (Input.GetKeyDown(interactKey)) {
                    goulagTrap.ReleaseAllPlayers();
                    Debug.Log("Gulag door opened!");
                }
            }
        }
    }
}

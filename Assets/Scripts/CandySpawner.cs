using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CandySpawner : MonoBehaviour
{
    [Header("Candy Settings")]
    public GameObject candyPrefab;
    public Transform spawnPoint;
    public float respawnCooldown = 10f;
    public float launchForce = 3f;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Feedback")]
    public Text interactionText;
    public string interactionHint = "Press E to collect candy";

    private GameObject currentCandy;
    private bool isOnCooldown = false;
    private Camera playerCamera;

    private void Start() {
        playerCamera = Camera.main;
        SpawnCandy();

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void Update() {
        if (isOnCooldown || currentCandy == null || playerCamera == null) {
            HideInteractionText();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance)) {
            if (hit.collider != null && hit.collider.gameObject == currentCandy) {
                ShowInteractionText();

                if (Input.GetKeyDown(interactKey))
                    CollectCandy();
            }
            else {
                HideInteractionText();
            }
        }
        else {
            HideInteractionText();
        }
    }

    private void SpawnCandy() {
        if (candyPrefab == null || spawnPoint == null) {
            Debug.LogWarning("CandySpawner missing prefab or spawn point!");
            return;
        }

        currentCandy = Instantiate(candyPrefab, spawnPoint.position, spawnPoint.rotation);

        Rigidbody rb = currentCandy.GetComponent<Rigidbody>();
        if (rb == null) {
            rb = currentCandy.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.isKinematic = false;

        Vector3 randomDirection = Vector3.up + new Vector3(
            Random.Range(-0.2f, 0.2f),
            0,
            Random.Range(-0.2f, 0.2f)
        );

        rb.AddForce(randomDirection.normalized * launchForce, ForceMode.Impulse);


        isOnCooldown = false;
        Debug.Log("🍬 Candy appeared with physics!");
    }

    private void CollectCandy() {
        if (currentCandy != null) {
            Destroy(currentCandy);
            currentCandy = null;
            HideInteractionText();
            Debug.Log("🧍 Player collected the candy!");
            StartCoroutine(CandyRespawnCooldown());
        }
    }

    private IEnumerator CandyRespawnCooldown() {
        isOnCooldown = true;
        Debug.Log($"⏳ Waiting {respawnCooldown} seconds before next candy...");
        yield return new WaitForSeconds(respawnCooldown);
        SpawnCandy();
    }

    private void ShowInteractionText() {
        if (interactionText != null) {
            interactionText.text = interactionHint;
            interactionText.gameObject.SetActive(true);
        }
    }

    private void HideInteractionText() {
        if (interactionText != null && interactionText.gameObject.activeSelf) {
            interactionText.gameObject.SetActive(false);
        }
    }
}

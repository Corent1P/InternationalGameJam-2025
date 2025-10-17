using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SoundDetectorTrap : TrapBase {
    [Header("Sound Settings")]
    public AudioClip triggerSound;
    [Range(0f, 1f)] public float volume = 1f;
    public bool playOnce = false;

    private AudioSource audioSource;
    private bool permanentlyDisabled = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    protected override void ActivateTrap(PlayerController player)
    {
        if (permanentlyDisabled)
            return;

        if (triggerSound != null) {
            audioSource.PlayOneShot(triggerSound, volume);
        }

        if (playOnce)
            permanentlyDisabled = true;
    }
}

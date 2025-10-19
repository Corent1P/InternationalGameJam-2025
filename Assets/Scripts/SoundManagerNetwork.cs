using UnityEngine;
using Unity.Netcode;

public class NetworkSoundManager : NetworkBehaviour
{
    // public static NetworkSoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonPressedSound;
    [SerializeField] private AudioClip[] footstepSounds; // Plusieurs sons de parquet
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip catchSound;

    [Header("3D Sound Settings")]
    [SerializeField] private float maxHearingDistance = 30f;
    [SerializeField] private float footstepVolume = 0.5f;

    // private void Awake()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //         DontDestroyOnLoad(gameObject);
    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    /// <summary>
    /// Joue un son 2D (UI, etc.) - Local uniquement
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Joue un son 3D à une position dans le monde - Synchronisé réseau
    /// </summary>
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        if (IsServer)
        {
            PlaySoundAtPositionClientRpc(GetClipIndex(clip), position, volume);
        }
        else
        {
            PlaySoundAtPositionServerRpc(GetClipIndex(clip), position, volume);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySoundAtPositionServerRpc(int clipIndex, Vector3 position, float volume)
    {
        PlaySoundAtPositionClientRpc(clipIndex, position, volume);
    }

    [ClientRpc]
    private void PlaySoundAtPositionClientRpc(int clipIndex, Vector3 position, float volume)
    {
        AudioClip clip = GetClipFromIndex(clipIndex);
        if (clip == null) return;

        // Créer un AudioSource temporaire à la position
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    /// <summary>
    /// Joue un son de pas de parquet aléatoire
    /// </summary>
    public void PlayFootstepSound(Vector3 position)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        // Choisir un son aléatoire parmi les sons de pas
        AudioClip randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
        
        // Variation aléatoire de pitch pour plus de réalisme
        float randomPitch = Random.Range(0.9f, 1.1f);

        if (IsServer)
        {
            PlayFootstepClientRpc(GetFootstepIndex(randomFootstep), position, randomPitch);
        }
        else
        {
            PlayFootstepServerRpc(GetFootstepIndex(randomFootstep), position, randomPitch);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayFootstepServerRpc(int footstepIndex, Vector3 position, float pitch)
    {
        PlayFootstepClientRpc(footstepIndex, position, pitch);
    }

    [ClientRpc]
    private void PlayFootstepClientRpc(int footstepIndex, Vector3 position, float pitch)
    {
        if (footstepIndex < 0 || footstepIndex >= footstepSounds.Length) return;

        // Créer un GameObject temporaire pour le son 3D
        GameObject soundObj = new GameObject("Footstep_Sound");
        soundObj.transform.position = position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = footstepSounds[footstepIndex];
        source.volume = footstepVolume;
        source.pitch = pitch;
        source.spatialBlend = 1f; // 3D complet
        source.maxDistance = maxHearingDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        // Détruire après lecture
        Destroy(soundObj, source.clip.length);
    }

    /// <summary>
    /// Joue le son du bouton (local)
    /// </summary>
    public void PlayButtonPressedSound()
    {
        PlaySound(buttonPressedSound);
    }

    /// <summary>
    /// Joue le son du dash
    /// </summary>
    public void PlayDashSound(Vector3 position)
    {
        PlaySoundAtPosition(dashSound, position, 0.7f);
    }

    /// <summary>
    /// Joue le son du catch
    /// </summary>
    public void PlayCatchSound(Vector3 position)
    {
        PlaySoundAtPosition(catchSound, position, 1f);
    }

    #region Helper Methods
    private int GetClipIndex(AudioClip clip)
    {
        // Pour simplifier, on utilise le hash du nom
        return clip.GetInstanceID();
    }

    private AudioClip GetClipFromIndex(int index)
    {
        // Recherche par ID (simplifié, marche pour les clips en Resources)
        if (dashSound != null && dashSound.GetInstanceID() == index) return dashSound;
        if (catchSound != null && catchSound.GetInstanceID() == index) return catchSound;
        if (buttonPressedSound != null && buttonPressedSound.GetInstanceID() == index) return buttonPressedSound;
        
        return null;
    }

    private int GetFootstepIndex(AudioClip clip)
    {
        for (int i = 0; i < footstepSounds.Length; i++)
        {
            if (footstepSounds[i] == clip) return i;
        }
        return 0;
    }

    public void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }
    #endregion
}
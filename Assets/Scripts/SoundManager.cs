using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonPressedSound;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySound(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void PlaySound(AudioClip clip, Vector3 position)
    {
        audioSource.transform.position = position;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StopSound()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    public float GetSoundTime()
    {
        return audioSource.time;
    }

    private float lastPlayTime = -1f;
    private float minInterval = 0.1f;

    public void PlaySafeSound(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        float currentTime = Time.time;
        if (currentTime - lastPlayTime < minInterval)
            return;

        lastPlayTime = currentTime;

        source.clip = clip;
        source.Play();
    }

    public void PlayButtonPressedSound()
    {
        PlaySound(buttonPressedSound);
    }
}

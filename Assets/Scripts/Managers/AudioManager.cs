using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip movementMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (musicSource != null && movementMusic != null)
        {
            musicSource.clip = movementMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        if (PlayerController.Instance != null && musicSource != null)
        {
            bool isMoving = PlayerController.Instance.IsMoving();

            if (Time.timeScale == 0f)
            {
                if (musicSource.isPlaying) musicSource.Pause();
                return;
            }

            if (isMoving)
            {
                if (!musicSource.isPlaying) musicSource.Play();
            }
            else
            {
                if (musicSource.isPlaying) musicSource.Pause();
            }
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
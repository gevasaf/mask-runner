using UnityEngine;

public class PlayAudioOnActive : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The audio clip to play when activated")]
    public AudioClip audioClip;
    
    [Tooltip("The audio source that will play the audio clip. If not assigned, will try to find one on this GameObject.")]
    public AudioSource audioSource;
    
    [Header("Activation Settings")]
    [Tooltip("Play audio automatically when the GameObject becomes active/enabled")]
    public bool playOnEnable = false;
    
    [Tooltip("Play audio automatically when the script starts")]
    public bool playOnStart = false;
    
    void Start()
    {
        // Try to find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("PlayAudioOnActive: No AudioSource found on " + gameObject.name + ". Please assign one or add an AudioSource component.");
            }
        }
        
        // Play on start if enabled
        if (playOnStart)
        {
            Activate();
        }
    }
    
    void OnEnable()
    {
        // Play on enable if enabled
        if (playOnEnable)
        {
            Activate();
        }
    }
    
    /// <summary>
    /// Activates the audio player and plays the audio clip as one shot
    /// </summary>
    public void Activate()
    {
        // Try to find AudioSource if still null
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        if (audioSource == null)
        {
            Debug.LogWarning("PlayAudioOnActive: AudioSource is not assigned on " + gameObject.name + " and no AudioSource component found.");
            return;
        }
        
        if (audioClip == null)
        {
            Debug.LogWarning("PlayAudioOnActive: AudioClip is not assigned on " + gameObject.name);
            return;
        }
        
        audioSource.PlayOneShot(audioClip);
    }
    
    /// <summary>
    /// Alternative method name for activation - plays the audio clip
    /// </summary>
    public void PlayAudio()
    {
        Activate();
    }
}


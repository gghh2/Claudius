using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SoundEffect
{
    public string soundName = "New Sound";
    public AudioClip audioClip;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.1f;
    public bool is3D = true;
}

public class SoundEffectsManager : MonoBehaviour
{
    public static SoundEffectsManager Instance { get; private set; }
    
    [Header("===== SOUND EFFECTS CONFIGURATION =====")]
    
    [Header("Sound Library")]
    [Tooltip("All sound effects available in the game")]
    public List<SoundEffect> soundEffects = new List<SoundEffect>();
    
    [Header("Audio Source Pool")]
    [Tooltip("Number of audio sources to pre-create")]
    public int poolSize = 20;
    
    [Header("Settings")]
    [Tooltip("Master SFX volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    
    [Tooltip("Maximum simultaneous sounds")]
    public int maxSimultaneousSounds = 30;
    
    [Header("3D Sound Settings")]
    [Tooltip("Default min distance for 3D sounds")]
    public float default3DMinDistance = 1f;
    
    [Tooltip("Default max distance for 3D sounds")]
    public float default3DMaxDistance = 20f;
    
    // Private
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private Dictionary<string, SoundEffect> soundDictionary = new Dictionary<string, SoundEffect>();
    private int currentPlayingSounds = 0;
    private float distanceMultiplier = 1f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
            BuildSoundDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject soundObject = new GameObject($"PooledAudioSource_{i}");
            soundObject.transform.SetParent(transform);
            
            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            soundObject.SetActive(false);
            
            audioSourcePool.Add(source);
        }
    }
    
    void BuildSoundDictionary()
    {
        soundDictionary.Clear();
        foreach (var sound in soundEffects)
        {
            if (!soundDictionary.ContainsKey(sound.soundName))
            {
                soundDictionary.Add(sound.soundName, sound);
            }
            else
            {
                Debug.LogWarning($"Duplicate sound name found: {sound.soundName}");
            }
        }
    }
    
    public void PlaySound(string soundName, Vector3? position = null)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound '{soundName}' not found!");
            return;
        }
        
        PlaySound(soundDictionary[soundName], position);
    }
    
    public void PlaySound(SoundEffect sound, Vector3? position = null)
    {
        if (sound == null || sound.audioClip == null)
            return;
        
        if (currentPlayingSounds >= maxSimultaneousSounds)
            return;
        
        AudioSource source = GetAvailableAudioSource();
        if (source == null)
            return;
        
        // Configure audio source
        source.clip = sound.audioClip;
        source.volume = sound.volume * masterVolume * distanceMultiplier;
        source.pitch = sound.pitch + Random.Range(-sound.pitchVariation, sound.pitchVariation);
        source.spatialBlend = sound.is3D ? 1f : 0f;
        
        // Position for 3D sounds
        if (sound.is3D && position.HasValue)
        {
            source.transform.position = position.Value;
            source.minDistance = default3DMinDistance;
            source.maxDistance = default3DMaxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
        }
        else
        {
            source.transform.position = Vector3.zero;
        }
        
        source.gameObject.SetActive(true);
        source.Play();
        currentPlayingSounds++;
        
        // Return to pool when finished
        StartCoroutine(ReturnToPool(source, sound.audioClip.length));
    }
    
    public void PlayRandomSound(List<string> soundNames, Vector3? position = null)
    {
        if (soundNames == null || soundNames.Count == 0)
            return;
        
        string randomSound = soundNames[Random.Range(0, soundNames.Count)];
        PlaySound(randomSound, position);
    }
    
    public void PlaySoundAtPlayer(string soundName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlaySound(soundName, player.transform.position);
        }
        else
        {
            PlaySound(soundName);
        }
    }
    
    AudioSource GetAvailableAudioSource()
    {
        foreach (var source in audioSourcePool)
        {
            if (!source.gameObject.activeInHierarchy)
            {
                return source;
            }
        }
        
        // If no available source, create a new one
        if (audioSourcePool.Count < poolSize * 2) // Allow pool to grow
        {
            GameObject soundObject = new GameObject($"PooledAudioSource_Extra_{audioSourcePool.Count}");
            soundObject.transform.SetParent(transform);
            
            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Add(source);
            
            return source;
        }
        
        return null;
    }
    
    System.Collections.IEnumerator ReturnToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);
        
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        currentPlayingSounds--;
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", masterVolume);
        PlayerPrefs.Save();
    }
    
    public void SetDistanceMultiplier(float multiplier)
    {
        distanceMultiplier = multiplier;
    }
    
    public void StopAllSounds()
    {
        foreach (var source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                source.Stop();
                source.gameObject.SetActive(false);
            }
        }
        currentPlayingSounds = 0;
    }
    
    void OnEnable()
    {
        // Load saved volume
        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            masterVolume = PlayerPrefs.GetFloat("SFXVolume");
        }
    }
}

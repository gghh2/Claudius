using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MusicTrack
{
    public string trackName = "New Track";
    public AudioClip audioClip;
    [Range(0f, 1f)]
    public float volume = 0.7f;
    public bool loop = true;
    public MusicZoneType[] playInZones;
}

[System.Serializable]
public enum MusicZoneType
{
    Menu,
    Laboratory,
    Hangar,
    Market,
    Ruins,
    SecurityArea,
    Storage,
    Residential,
    Engineering,
    Bridge,
    MedicalBay,
    Combat,
    Boss,
    Victory,
    GameOver
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    
    [Header("===== MUSIC CONFIGURATION =====")]
    
    [Header("Music Tracks")]
    [Tooltip("List of all music tracks in the game")]
    public List<MusicTrack> musicTracks = new List<MusicTrack>();
    
    [Header("Audio Sources")]
    [Tooltip("Primary audio source for music")]
    private AudioSource primarySource;
    
    [Tooltip("Secondary audio source for crossfading")]
    private AudioSource secondarySource;
    
    [Header("Settings")]
    [Tooltip("Master music volume")]
    [Range(0f, 1f)]
    public float masterVolume = 0.7f;
    
    [Tooltip("Fade duration when changing tracks")]
    public float fadeDuration = 2f;
    
    [Tooltip("Default track to play on start")]
    public string defaultTrackName = "Main Theme";
    
    [Header("Current State")]
    [SerializeField] private MusicTrack currentTrack;
    [SerializeField] private MusicZoneType currentZone = MusicZoneType.Menu;
    [SerializeField] private bool isFading = false;
    
    [Header("Debug")]
    public bool debugMode = true;
    
    // Private variables
    private Coroutine fadeCoroutine;
    private AudioSource activeSource;
    private AudioSource inactiveSource;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void SetupAudioSources()
    {
        // Create primary audio source
        primarySource = gameObject.AddComponent<AudioSource>();
        primarySource.playOnAwake = false;
        primarySource.loop = true;
        primarySource.volume = 0f;
        
        // Create secondary audio source for crossfading
        secondarySource = gameObject.AddComponent<AudioSource>();
        secondarySource.playOnAwake = false;
        secondarySource.loop = true;
        secondarySource.volume = 0f;
        
        activeSource = primarySource;
        inactiveSource = secondarySource;
        
        if (debugMode)
            Debug.Log("🎵 MusicManager: Audio sources created");
    }
    
    void Start()
    {
        // Play default track if specified
        if (!string.IsNullOrEmpty(defaultTrackName))
        {
            PlayTrackByName(defaultTrackName);
        }
        else if (musicTracks.Count > 0)
        {
            PlayTrack(musicTracks[0]);
        }
    }
    
    public void PlayTrackByName(string trackName)
    {
        MusicTrack track = musicTracks.FirstOrDefault(t => t.trackName == trackName);
        if (track != null)
        {
            PlayTrack(track);
        }
        else
        {
            Debug.LogWarning($"🎵 Track '{trackName}' not found!");
        }
    }
    
    public void PlayTrack(MusicTrack track)
    {
        if (track == null || track.audioClip == null)
        {
            Debug.LogError("🎵 Cannot play null track or clip!");
            return;
        }
        
        if (currentTrack == track && activeSource.isPlaying)
        {
            if (debugMode)
                Debug.Log($"🎵 Track '{track.trackName}' is already playing");
            return;
        }
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(CrossfadeToTrack(track));
    }
    
    IEnumerator CrossfadeToTrack(MusicTrack newTrack)
    {
        isFading = true;
        
        if (debugMode)
            Debug.Log($"🎵 Crossfading to: {newTrack.trackName}");
        
        // Setup the inactive source with new track
        inactiveSource.clip = newTrack.audioClip;
        inactiveSource.volume = 0f;
        inactiveSource.loop = newTrack.loop;
        inactiveSource.Play();
        
        // Crossfade
        float elapsed = 0f;
        float startVolumeActive = activeSource.volume;
        float targetVolumeInactive = newTrack.volume * masterVolume;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            
            // Fade out active source
            activeSource.volume = Mathf.Lerp(startVolumeActive, 0f, t);
            
            // Fade in inactive source
            inactiveSource.volume = Mathf.Lerp(0f, targetVolumeInactive, t);
            
            yield return null;
        }
        
        // Stop the old source
        activeSource.Stop();
        activeSource.volume = 0f;
        
        // Swap sources
        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
        
        currentTrack = newTrack;
        isFading = false;
        
        if (debugMode)
            Debug.Log($"🎵 Now playing: {newTrack.trackName}");
    }
    
    public void SetZone(MusicZoneType newZone)
    {
        if (currentZone == newZone)
            return;
        
        currentZone = newZone;
        
        if (debugMode)
            Debug.Log($"🎵 Entered zone: {newZone}");
        
        // Find appropriate track for this zone
        MusicTrack zoneTrack = GetTrackForZone(newZone);
        if (zoneTrack != null)
        {
            PlayTrack(zoneTrack);
        }
    }
    
    MusicTrack GetTrackForZone(MusicZoneType zone)
    {
        // First, try to find a track specifically for this zone
        var zoneTracks = musicTracks.Where(t => t.playInZones != null && t.playInZones.Contains(zone)).ToList();
        
        if (zoneTracks.Count > 0)
        {
            // Return random track from available ones for variety
            return zoneTracks[Random.Range(0, zoneTracks.Count)];
        }
        
        // No specific track for this zone
        if (debugMode)
            Debug.Log($"🎵 No specific track for zone: {zone}");
        
        return null;
    }
    
    public void PlayCombatMusic()
    {
        SetZone(MusicZoneType.Combat);
    }
    
    public void PlayVictoryMusic()
    {
        MusicTrack victoryTrack = GetTrackForZone(MusicZoneType.Victory);
        if (victoryTrack != null)
        {
            // Victory music typically doesn't loop
            victoryTrack.loop = false;
            PlayTrack(victoryTrack);
        }
    }
    
    public void PlayGameOverMusic()
    {
        MusicTrack gameOverTrack = GetTrackForZone(MusicZoneType.GameOver);
        if (gameOverTrack != null)
        {
            gameOverTrack.loop = false;
            PlayTrack(gameOverTrack);
        }
    }
    
    public void StopMusic(bool fade = true)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        if (fade)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            activeSource.Stop();
            activeSource.volume = 0f;
            currentTrack = null;
        }
    }
    
    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startVolume = activeSource.volume;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        activeSource.Stop();
        activeSource.volume = 0f;
        currentTrack = null;
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        if (currentTrack != null && !isFading)
        {
            activeSource.volume = currentTrack.volume * masterVolume;
        }
        
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("MusicVolume", masterVolume);
        PlayerPrefs.Save();
    }
    
    public void PauseMusic()
    {
        if (activeSource.isPlaying)
        {
            activeSource.Pause();
            if (debugMode)
                Debug.Log("🎵 Music paused");
        }
    }
    
    public void ResumeMusic()
    {
        if (!activeSource.isPlaying && currentTrack != null)
        {
            activeSource.UnPause();
            if (debugMode)
                Debug.Log("🎵 Music resumed");
        }
    }
    
    void OnEnable()
    {
        // Load saved volume
        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            masterVolume = PlayerPrefs.GetFloat("MusicVolume");
        }
    }
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("===== MUSIC MANAGER STATE =====");
        Debug.Log($"Current Track: {(currentTrack != null ? currentTrack.trackName : "None")}");
        Debug.Log($"Current Zone: {currentZone}");
        Debug.Log($"Master Volume: {masterVolume}");
        Debug.Log($"Is Fading: {isFading}");
        Debug.Log($"Active Source Playing: {activeSource.isPlaying}");
        Debug.Log($"Total Tracks: {musicTracks.Count}");
        Debug.Log("===============================");
    }
    
    [ContextMenu("List All Tracks")]
    public void ListAllTracks()
    {
        Debug.Log("===== AVAILABLE MUSIC TRACKS =====");
        foreach (var track in musicTracks)
        {
            string zones = track.playInZones != null && track.playInZones.Length > 0 
                ? string.Join(", ", track.playInZones.Select(z => z.ToString())) 
                : "No zones assigned";
            Debug.Log($"• {track.trackName} - Volume: {track.volume} - Zones: {zones}");
        }
        Debug.Log("==================================");
    }
}

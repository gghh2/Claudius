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
    
    [Header("Settings")]
    [Tooltip("Master music volume")]
    [Range(0f, 1f)]
    public float masterVolume = 0.7f;
    
    [Tooltip("Fade duration when changing tracks")]
    public float fadeDuration = 2f;
    
    [Tooltip("Default track to play on start")]
    public string defaultTrackName = "Main Theme";
    
    [Header("Random Play Settings")]
    [Tooltip("Enable random track selection")]
    public bool enableRandomPlay = false;
    
    [Tooltip("Avoid repeating the same track")]
    public bool avoidRepeats = true;
    
    [Tooltip("Play random track when current track ends (non-looping tracks only)")]
    public bool autoPlayNextRandom = true;
    
    [Header("Current State")]
    [SerializeField] private MusicTrack currentTrack;
    [SerializeField] private MusicZoneType currentZone = MusicZoneType.Menu;
    
    // Private variables
    private AudioSource primarySource;
    private AudioSource secondarySource;
    private Coroutine fadeCoroutine;
    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private bool isFading = false;
    private List<MusicTrack> recentlyPlayedTracks = new List<MusicTrack>();
    private int maxRecentTracks = 3; // Number of tracks to remember for avoid repeats
    
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
    }
    
    void Start()
    {
        // Play default track or random if enabled
        if (enableRandomPlay)
        {
            PlayRandomTrack();
        }
        else if (!string.IsNullOrEmpty(defaultTrackName))
        {
            PlayTrackByName(defaultTrackName);
        }
        else if (musicTracks.Count > 0)
        {
            PlayTrack(musicTracks[0]);
        }
    }
    
    void Update()
    {
        // Check if we need to play next random track
        if (autoPlayNextRandom && enableRandomPlay && activeSource != null && currentTrack != null)
        {
            // If track is not looping and has finished playing
            if (!currentTrack.loop && !activeSource.isPlaying && !isFading)
            {
                PlayRandomTrack();
            }
        }
    }
    
    public void PlayTrackByName(string trackName)
    {
        MusicTrack track = musicTracks.FirstOrDefault(t => t.trackName == trackName);
        if (track != null)
        {
            PlayTrack(track);
        }
    }
    
    public void PlayTrack(MusicTrack track)
    {
        if (track == null || track.audioClip == null)
            return;
        
        if (currentTrack == track && activeSource.isPlaying)
            return;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(CrossfadeToTrack(track));
    }
    
    IEnumerator CrossfadeToTrack(MusicTrack newTrack)
    {
        isFading = true;
        
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
    }
    
    public void SetZone(MusicZoneType newZone)
    {
        if (currentZone == newZone)
            return;
        
        currentZone = newZone;
        
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
            victoryTrack.loop = false;
            PlayTrack(victoryTrack);
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
        }
    }
    
    public void ResumeMusic()
    {
        if (!activeSource.isPlaying && currentTrack != null)
        {
            activeSource.UnPause();
        }
    }
    
    public void PlayRandomTrack()
    {
        if (musicTracks == null || musicTracks.Count == 0)
            return;
        
        List<MusicTrack> availableTracks = new List<MusicTrack>(musicTracks);
        
        // Remove recently played tracks if avoiding repeats
        if (avoidRepeats && availableTracks.Count > maxRecentTracks)
        {
            foreach (var recentTrack in recentlyPlayedTracks)
            {
                availableTracks.Remove(recentTrack);
            }
        }
        
        // Remove current track from available tracks
        if (currentTrack != null)
        {
            availableTracks.Remove(currentTrack);
        }
        
        // If no tracks available (all recently played), reset
        if (availableTracks.Count == 0)
        {
            availableTracks = new List<MusicTrack>(musicTracks);
            if (currentTrack != null)
                availableTracks.Remove(currentTrack);
        }
        
        // Select random track
        if (availableTracks.Count > 0)
        {
            MusicTrack randomTrack = availableTracks[Random.Range(0, availableTracks.Count)];
            PlayTrack(randomTrack);
            
            // Update recently played list
            if (avoidRepeats)
            {
                recentlyPlayedTracks.Add(randomTrack);
                if (recentlyPlayedTracks.Count > maxRecentTracks)
                {
                    recentlyPlayedTracks.RemoveAt(0);
                }
            }
        }
    }
    
    public void PlayRandomTrackFromZone(MusicZoneType zone)
    {
        var zoneTracks = musicTracks.Where(t => t.playInZones != null && t.playInZones.Contains(zone)).ToList();
        
        if (zoneTracks.Count > 0)
        {
            MusicTrack randomTrack = zoneTracks[Random.Range(0, zoneTracks.Count)];
            PlayTrack(randomTrack);
        }
    }
    
    public void ToggleRandomPlay()
    {
        enableRandomPlay = !enableRandomPlay;
    }
    
    public void PlayNextTrack()
    {
        if (enableRandomPlay)
        {
            PlayRandomTrack();
        }
        else
        {
            // Play next in list
            if (musicTracks.Count > 0 && currentTrack != null)
            {
                int currentIndex = musicTracks.IndexOf(currentTrack);
                int nextIndex = (currentIndex + 1) % musicTracks.Count;
                PlayTrack(musicTracks[nextIndex]);
            }
        }
    }
    
    public void PlayPreviousTrack()
    {
        if (musicTracks.Count > 0 && currentTrack != null)
        {
            int currentIndex = musicTracks.IndexOf(currentTrack);
            int prevIndex = currentIndex - 1;
            if (prevIndex < 0) prevIndex = musicTracks.Count - 1;
            PlayTrack(musicTracks[prevIndex]);
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
}

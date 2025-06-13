using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("===== UI ELEMENTS =====")]
    
    [Header("Volume Sliders")]
    [Tooltip("Master volume slider")]
    public Slider masterVolumeSlider;
    
    [Tooltip("Music volume slider")]
    public Slider musicVolumeSlider;
    
    [Tooltip("SFX volume slider")]
    public Slider sfxVolumeSlider;
    
    [Tooltip("Ambient volume slider")]
    public Slider ambientVolumeSlider;
    
    [Header("Volume Labels")]
    [Tooltip("Master volume text")]
    public TextMeshProUGUI masterVolumeText;
    
    [Tooltip("Music volume text")]
    public TextMeshProUGUI musicVolumeText;
    
    [Tooltip("SFX volume text")]
    public TextMeshProUGUI sfxVolumeText;
    
    [Tooltip("Ambient volume text")]
    public TextMeshProUGUI ambientVolumeText;
    
    [Header("Buttons")]
    [Tooltip("Apply settings button")]
    public Button applyButton;
    
    [Tooltip("Reset to defaults button")]
    public Button resetButton;
    
    [Tooltip("Test sound button")]
    public Button testSoundButton;
    
    [Header("Settings")]
    [Tooltip("Test sound name to play")]
    public string testSoundName = "UI_Click";
    
    // Private
    private float masterVolume = 1f;
    private float musicVolume = 0.7f;
    private float sfxVolume = 1f;
    private float ambientVolume = 0.8f;
    
    void Start()
    {
        LoadSettings();
        SetupUI();
    }
    
    void SetupUI()
    {
        // Setup sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = masterVolume;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = musicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        if (ambientVolumeSlider != null)
        {
            ambientVolumeSlider.minValue = 0f;
            ambientVolumeSlider.maxValue = 1f;
            ambientVolumeSlider.value = ambientVolume;
            ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
        }
        
        // Setup buttons
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefaults);
        }
        
        if (testSoundButton != null)
        {
            testSoundButton.onClick.AddListener(PlayTestSound);
        }
        
        UpdateVolumeLabels();
    }
    
    void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.8f);
    }
    
    void OnMasterVolumeChanged(float value)
    {
        masterVolume = value;
        UpdateVolumeLabels();
        
        // Apply master volume to all audio
        AudioListener.volume = masterVolume;
    }
    
    void OnMusicVolumeChanged(float value)
    {
        musicVolume = value;
        UpdateVolumeLabels();
        
        // Apply to music manager if it exists
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMasterVolume(musicVolume);
        }
    }
    
    void OnSFXVolumeChanged(float value)
    {
        sfxVolume = value;
        UpdateVolumeLabels();
        
        // Apply to SFX manager if it exists
        if (SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.SetMasterVolume(sfxVolume);
        }
    }
    
    void OnAmbientVolumeChanged(float value)
    {
        ambientVolume = value;
        UpdateVolumeLabels();
        
        // Apply to all AmbientSoundZones in the scene
        AmbientSoundZone[] ambientZones = FindObjectsOfType<AmbientSoundZone>();
        foreach (var zone in ambientZones)
        {
            zone.UpdateVolume();
        }
        
        // Save the preference immediately for new zones
        PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
    }
    
    void UpdateVolumeLabels()
    {
        if (masterVolumeText != null)
            masterVolumeText.text = $"Master: {Mathf.RoundToInt(masterVolume * 100)}%";
            
        if (musicVolumeText != null)
            musicVolumeText.text = $"Music: {Mathf.RoundToInt(musicVolume * 100)}%";
            
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"SFX: {Mathf.RoundToInt(sfxVolume * 100)}%";
            
        if (ambientVolumeText != null)
            ambientVolumeText.text = $"Ambient: {Mathf.RoundToInt(ambientVolume * 100)}%";
    }
    
    void ApplySettings()
    {
        // Save all settings
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
        PlayerPrefs.Save();
        
        Debug.Log("🎵 Audio settings saved!");
        
        // Play confirmation sound
        PlayTestSound();
    }
    
    void ResetToDefaults()
    {
        masterVolume = 1f;
        musicVolume = 0.7f;
        sfxVolume = 1f;
        ambientVolume = 0.8f;
        
        // Update sliders
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
        if (ambientVolumeSlider != null) ambientVolumeSlider.value = ambientVolume;
        
        // Apply immediately
        ApplySettings();
    }
    
    void PlayTestSound()
    {
        if (SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.PlaySound(testSoundName);
        }
    }
    
    void OnEnable()
    {
        // Reload settings when menu opens
        LoadSettings();
        
        // Update UI
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
        if (ambientVolumeSlider != null) ambientVolumeSlider.value = ambientVolume;
        
        UpdateVolumeLabels();
    }
}

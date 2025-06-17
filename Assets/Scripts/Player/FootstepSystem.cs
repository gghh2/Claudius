using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Footstep sound and particle system with surface detection
/// </summary>
public class FootstepSystem : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Default footstep sounds when no surface-specific sound is configured")]
    public AudioClip[] defaultFootstepSounds;
    public AudioSource audioSource;
    
    [Header("Surface Audio")]
    [Tooltip("Surface-specific sounds with alternative keywords")]
    public SurfaceAudioMapping[] surfaceAudio = new SurfaceAudioMapping[]
    {
        new SurfaceAudioMapping("grass", null) { alternativeKeywords = new string[] { "weed", "herbe", "lawn", "field" } },
        new SurfaceAudioMapping("dirt", null) { alternativeKeywords = new string[] { "soil", "mud", "earth", "ground" } },
        new SurfaceAudioMapping("stone", null) { alternativeKeywords = new string[] { "rock", "pierre", "concrete", "cement", "pavement" } },
        new SurfaceAudioMapping("metal", null) { alternativeKeywords = new string[] { "steel", "iron", "aluminum", "metallic" } },
        new SurfaceAudioMapping("sand", null) { alternativeKeywords = new string[] { "beach", "desert", "dune", "gravel" } },
        new SurfaceAudioMapping("water", null) { alternativeKeywords = new string[] { "eau", "liquid", "pool", "river", "ocean" } },
        new SurfaceAudioMapping("wood", null) { alternativeKeywords = new string[] { "timber", "plank", "oak", "pine", "bois" } }
    };
    
    [Header("Movement Detection")]
    [Range(0.01f, 1f)]
    public float movementThreshold = 0.1f;
    
    [Header("Footstep Timing")]
    [Range(0.1f, 1f)]
    public float stepInterval = 0.5f;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.7f;
    [Range(0f, 0.3f)]
    public float volumeVariation = 0.1f;
    
    [Header("Pitch Settings")]
    [Range(0.5f, 2f)]
    public float basePitch = 1f;
    [Range(0f, 0.3f)]
    public float pitchVariation = 0.2f;
    
    [Header("Surface Detection")]
    public bool enableSurfaceDetection = true;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayers = -1;
    
    [Header("Visual Effects")]
    public ParticleSystem footstepParticles;
    public bool autoCreateParticles = true;
    [Range(1, 50)]
    public int particlesPerStep = 10;
    public bool adaptParticleColor = true;
    
    [Header("Debug")]
    [Tooltip("Show detected surface in console")]
    public bool debugSurfaceDetection = false;
    
    [Space(5)]
    [Tooltip("Currently detected surface (Read-only)")]
    [SerializeField] private string _currentSurfaceDisplay = "default";
    
    [Header("Surface Colors")]
    public SurfaceColorMapping[] surfaceColors = new SurfaceColorMapping[]
    {
        new SurfaceColorMapping("grass", new Color(0.2f, 0.8f, 0.2f)),
        new SurfaceColorMapping("dirt", new Color(0.6f, 0.4f, 0.2f)),
        new SurfaceColorMapping("stone", new Color(0.7f, 0.7f, 0.7f)),
        new SurfaceColorMapping("metal", new Color(0.8f, 0.8f, 0.9f)),
        new SurfaceColorMapping("sand", new Color(0.9f, 0.8f, 0.6f)),
        new SurfaceColorMapping("water", new Color(0.3f, 0.6f, 1f)),
        new SurfaceColorMapping("wood", new Color(0.6f, 0.3f, 0.1f)),
        new SurfaceColorMapping("default", new Color(0.8f, 0.8f, 0.8f))
    };
    
    // Private variables
    private float stepTimer = 0f;
    private bool isMoving = false;
    private string currentSurface = "default";
    private Transform modelTransform;
    private TerrainLayerDetector terrainDetector;
    
    // Cache
    private RaycastHit groundHit;
    private Dictionary<string, Color> surfaceColorDict;
    private Dictionary<string, AudioClip[]> surfaceAudioDict;
    
    void Start()
    {
        SetupAudioSource();
        BuildDictionaries();
        SetupParticleSystem();
        
        // Find model for foot position
        modelTransform = transform.Find("space_man_model");
        if (modelTransform == null)
            modelTransform = transform;
        
        // Find terrain detector
        terrainDetector = GetComponent<TerrainLayerDetector>();
    }
    
    void Update()
    {
        CheckMovement();
        UpdateFootsteps();
    }
    
    void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = footstepVolume;
    }
    
    void BuildDictionaries()
    {
        // Build color dictionary
        surfaceColorDict = new Dictionary<string, Color>();
        foreach (var mapping in surfaceColors)
        {
            if (!string.IsNullOrEmpty(mapping.surfaceName))
            {
                surfaceColorDict[mapping.surfaceName.ToLower()] = mapping.color;
            }
        }
        
        // Ensure default color exists
        if (!surfaceColorDict.ContainsKey("default"))
        {
            surfaceColorDict["default"] = Color.white;
        }
        
        // Build audio dictionary
        surfaceAudioDict = new Dictionary<string, AudioClip[]>();
        foreach (var mapping in surfaceAudio)
        {
            if (!string.IsNullOrEmpty(mapping.surfaceName) && mapping.audioClips != null && mapping.audioClips.Length > 0)
            {
                var validClips = mapping.audioClips.Where(clip => clip != null).ToArray();
                if (validClips.Length > 0)
                {
                    surfaceAudioDict[mapping.surfaceName.ToLower()] = validClips;
                }
            }
        }
    }
    
    void SetupParticleSystem()
    {
        if (footstepParticles == null && autoCreateParticles)
        {
            CreateParticleSystem();
        }
        else if (footstepParticles != null)
        {
            ConfigureParticles();
        }
    }
    
    void CreateParticleSystem()
    {
        GameObject particleGO = new GameObject("FootstepParticles");
        particleGO.transform.SetParent(transform);
        particleGO.transform.localPosition = Vector3.zero;
        
        footstepParticles = particleGO.AddComponent<ParticleSystem>();
        
        // Create material
        var renderer = footstepParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        ConfigureParticles();
    }
    
    void ConfigureParticles()
    {
        if (footstepParticles == null) return;
        
        var main = footstepParticles.main;
        main.startLifetime = 1f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = Color.white;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var shape = footstepParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.2f;
        
        var velocityOverLifetime = footstepParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(-1f);
        
        var sizeOverLifetime = footstepParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var colorOverLifetime = footstepParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;
        
        var emission = footstepParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        
        var forceOverLifetime = footstepParticles.forceOverLifetime;
        forceOverLifetime.enabled = true;
        forceOverLifetime.y = -2f;
    }
    
    void CheckMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        bool currentlyMoving = Mathf.Abs(horizontal) > movementThreshold || Mathf.Abs(vertical) > movementThreshold;
        
        // First step immediately when starting to move
        if (!isMoving && currentlyMoving)
        {
            stepTimer = stepInterval;
        }
        
        isMoving = currentlyMoving;
    }
    
    void UpdateFootsteps()
    {
        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }
        
        if (enableSurfaceDetection && !IsGrounded())
        {
            stepTimer = 0f;
            return;
        }
        
        stepTimer += Time.deltaTime;
        
        if (stepTimer >= stepInterval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }
    
    void PlayFootstep()
    {
        // Get sound
        AudioClip stepSound = GetSurfaceAudioClip(currentSurface);
        if (stepSound == null) return;
        
        // Calculate volume
        float finalVolume = footstepVolume + Random.Range(-volumeVariation, volumeVariation);
        finalVolume = Mathf.Clamp01(finalVolume);
        
        // Apply camera distance multiplier
        if (AudioDistanceManager.Instance != null)
        {
            finalVolume *= AudioDistanceManager.Instance.GetCurrentMultiplier();
        }
        
        // Calculate pitch
        float finalPitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
        finalPitch = Mathf.Clamp(finalPitch, 0.1f, 3f);
        
        // Play sound
        audioSource.volume = finalVolume;
        audioSource.pitch = finalPitch;
        audioSource.PlayOneShot(stepSound);
        
        // Play particles
        PlayStepParticles();
    }
    
    void PlayStepParticles()
    {
        if (footstepParticles == null) return;
        
        if (adaptParticleColor)
        {
            Color particleColor = GetSurfaceColor(currentSurface);
            var main = footstepParticles.main;
            main.startColor = particleColor;
        }
        
        var emission = footstepParticles.emission;
        emission.SetBursts(new ParticleSystem.Burst[] 
        {
            new ParticleSystem.Burst(0f, particlesPerStep)
        });
        
        footstepParticles.Play();
    }
    
    bool IsGrounded()
    {
        Vector3 rayStart = modelTransform.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out groundHit, groundCheckDistance + 0.1f, groundLayers))
        {
            DetectSurface(groundHit);
            return true;
        }
        
        return false;
    }
    
    void DetectSurface(RaycastHit hit)
    {
        string detectedSurface = "default";
        
        // Priority 1: Check material
        Renderer renderer = hit.collider.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            string materialName = renderer.material.name.ToLower()
                .Replace(" (instance)", "")
                .Replace("_mat", "")
                .Replace("material", "");
            
            detectedSurface = FindSurfaceMatch(materialName);
        }
        
        // Priority 2: Check terrain
        if (detectedSurface == "default" && terrainDetector != null && hit.collider.GetComponent<Terrain>() != null)
        {
            string terrainSurface = terrainDetector.GetCurrentTerrainSurface(hit.point);
            if (!string.IsNullOrEmpty(terrainSurface))
            {
                detectedSurface = terrainSurface;
            }
        }
        
        // Priority 3: Check object name
        if (detectedSurface == "default")
        {
            string objectName = hit.collider.gameObject.name.ToLower();
            detectedSurface = FindSurfaceMatch(objectName);
        }
        
        // Only update if surface changed
        if (currentSurface != detectedSurface)
        {
            currentSurface = detectedSurface;
            _currentSurfaceDisplay = currentSurface; // Update display in Inspector
            
            if (debugSurfaceDetection)
            {
                Debug.Log($"🦶 Surface changed to: {currentSurface}");
            }
        }
    }
    
    string FindSurfaceMatch(string name)
    {
        // Convert name to lowercase for comparison
        string lowerName = name.ToLower();
        
        // Check each surface mapping
        foreach (var mapping in surfaceAudio)
        {
            // Check main surface name
            if (lowerName.Contains(mapping.surfaceName.ToLower()))
            {
                return mapping.surfaceName;
            }
            
            // Check alternative keywords
            if (mapping.alternativeKeywords != null)
            {
                foreach (string keyword in mapping.alternativeKeywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && lowerName.Contains(keyword.ToLower()))
                    {
                        return mapping.surfaceName;
                    }
                }
            }
        }
        
        return "default";
    }
    
    Color GetSurfaceColor(string surface)
    {
        string lowerSurface = surface.ToLower();
        
        if (surfaceColorDict.TryGetValue(lowerSurface, out Color color))
        {
            return color;
        }
        
        return surfaceColorDict["default"];
    }
    
    AudioClip GetSurfaceAudioClip(string surface)
    {
        if (audioSource == null) return null;
        
        string lowerSurface = surface.ToLower();
        
        // Try surface-specific sounds first
        if (surfaceAudioDict.TryGetValue(lowerSurface, out AudioClip[] clips))
        {
            if (clips.Length > 0)
            {
                return clips[Random.Range(0, clips.Length)];
            }
        }
        
        // Fallback to default sounds
        if (defaultFootstepSounds != null && defaultFootstepSounds.Length > 0)
        {
            var validSounds = defaultFootstepSounds.Where(clip => clip != null).ToArray();
            if (validSounds.Length > 0)
            {
                return validSounds[Random.Range(0, validSounds.Length)];
            }
        }
        
        return null;
    }
    
    // Public methods
    public void SetFootstepsEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            stepTimer = 0f;
            isMoving = false;
        }
    }
    
    public void SetFootstepVolume(float volume)
    {
        footstepVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            float finalVolume = footstepVolume;
            if (AudioDistanceManager.Instance != null)
            {
                finalVolume *= AudioDistanceManager.Instance.GetCurrentMultiplier();
            }
            audioSource.volume = finalVolume;
        }
    }
    
    public void ForceFootstep()
    {
        PlayFootstep();
    }
    
    public void PlayJumpParticles()
    {
        if (footstepParticles == null) return;
        
        if (adaptParticleColor)
        {
            Color particleColor = GetSurfaceColor(currentSurface);
            var main = footstepParticles.main;
            main.startColor = particleColor;
        }
        
        var emission = footstepParticles.emission;
        emission.SetBursts(new ParticleSystem.Burst[] 
        {
            new ParticleSystem.Burst(0f, particlesPerStep * 2)
        });
        
        footstepParticles.Play();
    }
    
    public void PlayLandingParticles()
    {
        if (footstepParticles == null) return;
        
        if (adaptParticleColor)
        {
            Color particleColor = GetSurfaceColor(currentSurface);
            var main = footstepParticles.main;
            main.startColor = particleColor;
        }
        
        var emission = footstepParticles.emission;
        emission.SetBursts(new ParticleSystem.Burst[] 
        {
            new ParticleSystem.Burst(0f, particlesPerStep * 3)
        });
        
        footstepParticles.Play();
    }
    
    // Context menu methods for easier setup
    [ContextMenu("Add Common Keywords to All Surfaces")]
    void AddCommonKeywords()
    {
        foreach (var mapping in surfaceAudio)
        {
            switch (mapping.surfaceName.ToLower())
            {
                case "grass":
                    AddKeywordsIfMissing(mapping, "weed", "herbe", "lawn", "field", "meadow", "turf");
                    break;
                case "dirt":
                    AddKeywordsIfMissing(mapping, "soil", "mud", "earth", "ground", "clay", "dust");
                    break;
                case "stone":
                    AddKeywordsIfMissing(mapping, "rock", "pierre", "concrete", "cement", "pavement", "brick", "cobble");
                    break;
                case "metal":
                    AddKeywordsIfMissing(mapping, "steel", "iron", "aluminum", "metallic", "copper", "tin");
                    break;
                case "sand":
                    AddKeywordsIfMissing(mapping, "beach", "desert", "dune", "gravel", "pebble");
                    break;
                case "water":
                    AddKeywordsIfMissing(mapping, "eau", "liquid", "pool", "river", "ocean", "lake", "puddle");
                    break;
                case "wood":
                    AddKeywordsIfMissing(mapping, "timber", "plank", "oak", "pine", "bois", "log", "bark");
                    break;
            }
        }
        
        Debug.Log("✅ Common keywords added to all surfaces");
    }
    
    void AddKeywordsIfMissing(SurfaceAudioMapping mapping, params string[] keywords)
    {
        var keywordsList = new System.Collections.Generic.List<string>(mapping.alternativeKeywords ?? new string[0]);
        
        foreach (string keyword in keywords)
        {
            if (!keywordsList.Contains(keyword))
            {
                keywordsList.Add(keyword);
            }
        }
        
        mapping.alternativeKeywords = keywordsList.ToArray();
    }
    
    // Serializable classes
    [System.Serializable]
    public class SurfaceColorMapping
    {
        public string surfaceName;
        public Color color;
        
        public SurfaceColorMapping(string name, Color col)
        {
            surfaceName = name;
            color = col;
        }
    }
    
    [System.Serializable]
    public class SurfaceAudioMapping
    {
        [Tooltip("Main surface name")]
        public string surfaceName;
        
        [Tooltip("Alternative keywords that also match this surface")]
        public string[] alternativeKeywords;
        
        [Tooltip("Audio clips for this surface")]
        public AudioClip[] audioClips;
        
        public SurfaceAudioMapping(string name, AudioClip[] clips)
        {
            surfaceName = name;
            audioClips = clips;
            alternativeKeywords = new string[0];
        }
    }
}

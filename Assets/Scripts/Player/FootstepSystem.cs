using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Système de bruits de pas avec particules - Version finale optimisée
/// </summary>
public class FootstepSystem : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Sons de pas par défaut (utilisés quand aucun son spécifique n'est configuré pour la surface)")]
    public AudioClip[] defaultFootstepSounds;
    public AudioSource audioSource;
    
    [Header("Surface Audio")]
    [Tooltip("Sons spécifiques selon les surfaces détectées")]
    public SurfaceAudioMapping[] surfaceAudio = new SurfaceAudioMapping[]
    {
        new SurfaceAudioMapping("grass", null),
        new SurfaceAudioMapping("dirt", null),
        new SurfaceAudioMapping("stone", null),
        new SurfaceAudioMapping("metal", null),
        new SurfaceAudioMapping("sand", null),
        new SurfaceAudioMapping("water", null),
        new SurfaceAudioMapping("wood", null),
        new SurfaceAudioMapping("floor", null),
        new SurfaceAudioMapping("ground", null)
    };
    
    [Header("Movement Detection")]
    [Range(0.01f, 1f)]
    public float movementThreshold = 0.1f;
    public bool useSmoothing = false;
    [Range(0.05f, 0.3f)]
    public float smoothingDuration = 0.1f;
    
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
    public LayerMask groundLayers = 1;
    
    [Header("Visual Effects")]
    public ParticleSystem footstepParticles;
    public bool autoCreateParticles = true;
    [Range(1, 50)]
    public int particlesPerStep = 10;
    public bool adaptParticleColor = true;
    
    [Header("Surface Colors")]
    [Tooltip("Couleurs des particules selon les surfaces détectées")]
    public SurfaceColorMapping[] surfaceColors = new SurfaceColorMapping[]
    {
        new SurfaceColorMapping("grass", new Color(0.2f, 0.8f, 0.2f)),
        new SurfaceColorMapping("dirt", new Color(0.6f, 0.4f, 0.2f)),
        new SurfaceColorMapping("stone", new Color(0.7f, 0.7f, 0.7f)),
        new SurfaceColorMapping("metal", new Color(0.8f, 0.8f, 0.9f)),
        new SurfaceColorMapping("sand", new Color(0.9f, 0.8f, 0.6f)),
        new SurfaceColorMapping("water", new Color(0.3f, 0.6f, 1f)),
        new SurfaceColorMapping("wood", new Color(0.6f, 0.3f, 0.1f)),
        new SurfaceColorMapping("floor", new Color(0.5f, 0.5f, 0.5f)),
        new SurfaceColorMapping("ground", new Color(0.4f, 0.3f, 0.2f)),
        new SurfaceColorMapping("default", new Color(0.8f, 0.8f, 0.8f))
    };
    
    // Debug est maintenant géré par GlobalDebugManager
    
    // Variables privées
    private float stepTimer = 0f;
    private bool isMoving = false;
    private Vector3 lastPosition;
    private string currentSurface = "default";
    
    // Lissage
    private float smoothTimer = 0f;
    private bool wasMoving = false;
    
    // Buffer de positions
    private Vector3[] positionBuffer = new Vector3[3];
    private int bufferIndex = 0;
    
    // Cache
    private RaycastHit groundHit;
    private Dictionary<string, Color> surfaceColorDict;
    private Dictionary<string, AudioClip[]> surfaceAudioDict;
    private Material particleMaterial;
    
    // NOUVEAU : Référence au modèle pour position des pieds
    private Transform modelTransform;
    
    // NOUVEAU : Référence au détecteur de terrain
    private TerrainLayerDetector terrainDetector;
    
    void Start()
    {
        SetupAudioSource();
        BuildSurfaceColorDictionary();
        BuildSurfaceAudioDictionary();
        SetupParticleSystem();
        InitializePositionBuffer();
        
        // NOUVEAU : Trouve le modèle pour la position des pieds
        modelTransform = transform.Find("space_man_model");
        if (modelTransform == null)
        {
            Debug.LogWarning("⚠️ space_man_model non trouvé - utilisation position Player");
            modelTransform = transform; // Fallback
        }
        
        // NOUVEAU : Cherche le détecteur de terrain
        terrainDetector = GetComponent<TerrainLayerDetector>();
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
            Debug.Log("🦶 FootstepSystem initialisé");
    }
    
    void Update()
    {
        CheckMovement();
        UpdateFootsteps();
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep) && enableSurfaceDetection)
        {
            DrawGroundRaycast();
        }
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
    
    void BuildSurfaceColorDictionary()
    {
        surfaceColorDict = new Dictionary<string, Color>();
        
        foreach (var mapping in surfaceColors)
        {
            if (!string.IsNullOrEmpty(mapping.surfaceName))
            {
                surfaceColorDict[mapping.surfaceName.ToLower()] = mapping.color;
            }
        }
        
        // Assure-toi qu'il y a toujours une couleur par défaut
        if (!surfaceColorDict.ContainsKey("default"))
        {
            surfaceColorDict["default"] = Color.white;
        }
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
            Debug.Log($"🎨 {surfaceColorDict.Count} couleurs de surface chargées depuis l'Inspector");
    }
    
    void BuildSurfaceAudioDictionary()
    {
        surfaceAudioDict = new Dictionary<string, AudioClip[]>();
        
        foreach (var mapping in surfaceAudio)
        {
            if (!string.IsNullOrEmpty(mapping.surfaceName) && mapping.audioClips != null && mapping.audioClips.Length > 0)
            {
                // Filtre les clips null
                var validClips = new List<AudioClip>();
                foreach (var clip in mapping.audioClips)
                {
                    if (clip != null)
                        validClips.Add(clip);
                }
                
                if (validClips.Count > 0)
                {
                    surfaceAudioDict[mapping.surfaceName.ToLower()] = validClips.ToArray();
                }
            }
        }
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
            Debug.Log($"🎵 {surfaceAudioDict.Count} mappings audio de surface chargés depuis l'Inspector");
    }
    
    void SetupParticleSystem()
    {
        if (footstepParticles == null && autoCreateParticles)
        {
            CreateParticleSystem();
        }
        else if (footstepParticles != null)
        {
            ConfigureExistingParticles();
        }
    }
    
    void CreateParticleSystem()
    {
        GameObject particleGO = new GameObject("FootstepParticles");
        particleGO.transform.SetParent(transform);
        particleGO.transform.localPosition = Vector3.zero;
        
        footstepParticles = particleGO.AddComponent<ParticleSystem>();
        
        // SOLUTION : Crée et assigne un matériel
        CreateParticleMaterial();
        
        ConfigureParticles();
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
            Debug.Log("✨ Système de particules créé avec matériel");
    }
    
    void ConfigureExistingParticles()
    {
        if (footstepParticles == null) return;
        
        // Vérifie si le renderer a un matériel
        var renderer = footstepParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && renderer.material == null)
        {
            CreateParticleMaterial();
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                Debug.Log("🔧 Matériel assigné au système de particules existant");
        }
        
        ConfigureParticles();
    }
    
    void CreateParticleMaterial()
    {
        // Crée un matériel simple pour les particules
        particleMaterial = new Material(Shader.Find("Sprites/Default"));
        particleMaterial.name = "FootstepParticleMaterial";
        
        // Assigne le matériel au renderer
        var renderer = footstepParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = particleMaterial;
        }
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
    
    void InitializePositionBuffer()
    {
        // CORRECTION : Utilise la position du modèle
        Vector3 initialPosition = modelTransform != null ? modelTransform.position : transform.position;
        lastPosition = initialPosition;
        for (int i = 0; i < positionBuffer.Length; i++)
        {
            positionBuffer[i] = initialPosition;
        }
    }
    
    void CheckMovement()
    {
        positionBuffer[bufferIndex] = transform.position;
        bufferIndex = (bufferIndex + 1) % positionBuffer.Length;
        
        float totalDistance = 0f;
        for (int i = 0; i < positionBuffer.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(positionBuffer[i], positionBuffer[i + 1]);
        }
        
        float averageSpeed = totalDistance / (positionBuffer.Length * Time.deltaTime);
        bool currentlyMoving = averageSpeed > movementThreshold;
        
        // SOLUTION : Détecte le début du mouvement pour pas immédiat
        bool wasMovingPreviously = isMoving;
        
        if (useSmoothing)
        {
            if (currentlyMoving)
            {
                isMoving = true;
                smoothTimer = 0f;
                wasMoving = true;
            }
            else if (wasMoving)
            {
                smoothTimer += Time.deltaTime;
                if (smoothTimer >= smoothingDuration)
                {
                    isMoving = false;
                    wasMoving = false;
                    smoothTimer = 0f;
                }
            }
            else
            {
                isMoving = false;
                smoothTimer = 0f;
            }
        }
        else
        {
            isMoving = currentlyMoving;
        }
        
        // NOUVEAU : Premier pas immédiat quand on commence à bouger
        if (!wasMovingPreviously && isMoving)
        {
            stepTimer = stepInterval; // Force le premier pas immédiatement
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                Debug.Log("🦶 Premier pas immédiat détecté !");
        }
        
        lastPosition = transform.position;
    }
    
    void UpdateFootsteps()
    {
        if (!isMoving)
        {
            stepTimer = 0f; // Reset du timer quand on s'arrête
            return;
        }
        
        if (enableSurfaceDetection && !IsGrounded())
        {
            stepTimer = 0f;
            return;
        }
        
        stepTimer += Time.deltaTime;
        
        // SOLUTION : Premier pas immédiat quand on commence à bouger
        if (stepTimer >= stepInterval)
        {
            PlayFootstep();
            stepTimer = 0f; // Reset après chaque pas
        }
    }
    
    void PlayFootstep()
    {
        // Son
        AudioClip stepSound = GetSurfaceAudioClip(currentSurface);
        if (stepSound == null) return;
        
        float finalVolume = footstepVolume + Random.Range(-volumeVariation, volumeVariation);
        finalVolume = Mathf.Clamp01(finalVolume);
        
        // NOUVEAU : Applique le multiplicateur de distance de la caméra
        if (AudioDistanceManager.Instance != null)
        {
            finalVolume *= AudioDistanceManager.Instance.GetCurrentMultiplier();
        }
        
        float finalPitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
        finalPitch = Mathf.Clamp(finalPitch, 0.1f, 3f);
        
        audioSource.volume = finalVolume;
        audioSource.pitch = finalPitch;
        audioSource.PlayOneShot(stepSound);
        
        // Particules
        PlayStepParticles();
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
        {
            Debug.Log($"🦶 PAS: {stepSound.name} | Surface: {currentSurface} | Volume: {finalVolume:F2}");
        }
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
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
        {
            Debug.Log($"💨 Particules: {particlesPerStep} ({currentSurface})");
        }
    }
    
    bool IsGrounded()
    {
        // CORRECTION : Raycast depuis les pieds du modèle
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
        
        // MÉTHODE 1: PRIORITÉ HAUTE - Vérifie d'abord le Material (pour les objets posés sur le terrain)
        Renderer renderer = hit.collider.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            string materialName = renderer.material.name.ToLower()
                .Replace(" (instance)", "") // Unity ajoute souvent ceci
                .Replace("_mat", "")        // Suffixe commun
                .Replace("material", "");   // Mot "material"
            
            if (IsSurfaceNameRecognized(materialName))
            {
                detectedSurface = materialName;
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                    Debug.Log($"🦶 Surface détectée par matériel: '{materialName}' (priorité haute)");
            }
            else
            {
                // Recherche par mots-clés dans le nom du matériel
                string keywordSurface = FindSurfaceByKeywords(materialName);
                if (!string.IsNullOrEmpty(keywordSurface))
                {
                    detectedSurface = keywordSurface;
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                        Debug.Log($"🦶 Surface détectée par mot-clé matériel: '{keywordSurface}' depuis '{materialName}'");
                }
            }
        }
        
        // MÉTHODE 2: Si pas trouvé par matériel, vérifie si c'est un terrain
        if (detectedSurface == "default" && terrainDetector != null && hit.collider.GetComponent<Terrain>() != null)
        {
            string terrainSurface = terrainDetector.GetCurrentTerrainSurface(hit.point);
            if (!string.IsNullOrEmpty(terrainSurface))
            {
                detectedSurface = terrainSurface;
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                    Debug.Log($"🏔️ Surface détectée par TERRAIN LAYER: '{terrainSurface}' (priorité moyenne)");
            }
        }
        
        // MÉTHODE 3: En dernier recours, essaie le nom du GameObject (priorité basse)
        if (detectedSurface == "default")
        {
            string objectName = hit.collider.gameObject.name.ToLower();
            if (IsSurfaceNameRecognized(objectName))
            {
                detectedSurface = objectName;
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                    Debug.Log($"🦶 Surface détectée par nom d'objet: '{objectName}' (fallback)");
            }
            else
            {
                // Recherche par mots-clés dans le nom d'objet
                string keywordSurface = FindSurfaceByKeywords(objectName);
                if (!string.IsNullOrEmpty(keywordSurface))
                {
                    detectedSurface = keywordSurface;
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                        Debug.Log($"🦶 Surface détectée par mot-clé objet: '{keywordSurface}' depuis '{objectName}'");
                }
                else if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                {
                    string materialName = renderer?.material?.name ?? "aucun";
                    Debug.Log($"🦶 Surface non reconnue - Matériel: '{materialName}' | Objet: '{objectName}' → default");
                }
            }
        }
        
        // Met à jour seulement si différent
        if (detectedSurface != currentSurface)
        {
            currentSurface = detectedSurface;
        }
    }
    
    /// <summary>
    /// Vérifie si un nom de surface est reconnu dans notre dictionnaire
    /// </summary>
    bool IsSurfaceNameRecognized(string surfaceName)
    {
        foreach (var kvp in surfaceColorDict)
        {
            if (surfaceName.Contains(kvp.Key))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Recherche par mots-clés dans le nom du matériel
    /// </summary>
    string FindSurfaceByKeywords(string materialName)
    {
        // Mots-clés pour chaque type de surface
        var surfaceKeywords = new Dictionary<string, string[]>
        {
            ["grass"] = new[] { "grass", "herbe", "lawn", "field" },
            ["stone"] = new[] { "stone", "rock", "pierre", "concrete", "cement" },
            ["metal"] = new[] { "metal", "steel", "iron", "aluminum", "chrome" },
            ["wood"] = new[] { "wood", "timber", "plank", "oak", "pine" },
            ["water"] = new[] { "water", "eau", "liquid", "pool" },
            ["sand"] = new[] { "sand", "beach", "desert", "dune" },
            ["dirt"] = new[] { "dirt", "soil", "mud", "earth", "ground" }
        };
        
        foreach (var surface in surfaceKeywords)
        {
            foreach (string keyword in surface.Value)
            {
                if (materialName.Contains(keyword))
                {
                    return surface.Key;
                }
            }
        }
        
        return ""; // Rien trouvé
    }
    
    Color GetSurfaceColor(string surface)
    {
        string lowerSurface = surface.ToLower();
        
        foreach (var kvp in surfaceColorDict)
        {
            if (lowerSurface.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }
        
        return surfaceColorDict["default"];
    }
    
    AudioClip GetSurfaceAudioClip(string surface)
    {
        if (audioSource == null) return null;
        
        string lowerSurface = surface.ToLower();
        
        // 1. Cherche d'abord un son spécifique à la surface
        foreach (var kvp in surfaceAudioDict)
        {
            if (lowerSurface.Contains(kvp.Key))
            {
                AudioClip[] clips = kvp.Value;
                if (clips != null && clips.Length > 0)
                {
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                        Debug.Log($"🎵 Son spécifique trouvé pour '{lowerSurface}' → {kvp.Key}");
                    return clips[Random.Range(0, clips.Length)];
                }
            }
        }
        
        // 2. Fallback vers les sons par défaut
        if (defaultFootstepSounds != null && defaultFootstepSounds.Length > 0)
        {
            var validSounds = defaultFootstepSounds.Where(clip => clip != null).ToArray();
            if (validSounds.Length > 0)
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
                    Debug.Log($"🎵 Son par défaut utilisé pour '{lowerSurface}'");
                return validSounds[Random.Range(0, validSounds.Length)];
            }
        }
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
            Debug.LogWarning($"🎵 Aucun son trouvé pour '{lowerSurface}'");
        return null;
    }
    
    void DrawGroundRaycast()
    {
        // CORRECTION : Debug raycast depuis les pieds du modèle
        Vector3 rayStart = modelTransform.position + Vector3.up * 0.1f;
        Debug.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.1f), 
                     IsGrounded() ? Color.green : Color.red);
    }
    
    void OnGUI()
    {
        if (!GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep)) return;
        
        GUILayout.BeginArea(new Rect(10, 150, 300, 180));
        GUILayout.Label("=== FOOTSTEP DEBUG ===");
        GUILayout.Label($"En mouvement: {(isMoving ? "✅" : "❌")}");
        GUILayout.Label($"Au sol: {(IsGrounded() ? "✅" : "❌")}");
        GUILayout.Label($"Timer: {stepTimer:F2}s / {stepInterval:F2}s");
        GUILayout.Label($"Surface: {currentSurface}");
        GUILayout.Label($"Sons par défaut: {defaultFootstepSounds?.Length ?? 0}");
        GUILayout.Label($"Sons de surface: {surfaceAudioDict?.Count ?? 0}");
        GUILayout.Label($"Particules: {(footstepParticles != null ? "✅" : "❌")}");
        
        if (useSmoothing)
        {
            GUILayout.Label($"Lissage: {smoothTimer:F2}s");
        }
        
        if (GUILayout.Button("Force un pas"))
        {
            PlayFootstep();
        }
        
        if (GUILayout.Button("Rebuild Colors"))
        {
            BuildSurfaceColorDictionary();
        }
        
        if (GUILayout.Button("Rebuild Audio"))
        {
            BuildSurfaceAudioDictionary();
        }
        
        GUILayout.EndArea();
    }
    
    // Structure pour l'Inspector
    [System.Serializable]
    public class SurfaceColorMapping
    {
        [Tooltip("Nom de la surface (contenu dans le nom du GameObject)")]
        public string surfaceName;
        
        [Tooltip("Couleur des particules pour cette surface")]
        public Color color;
        
        public SurfaceColorMapping(string name, Color col)
        {
            surfaceName = name;
            color = col;
        }
        
        public SurfaceColorMapping()
        {
            surfaceName = "";
            color = Color.white;
        }
    }
    
    [System.Serializable]
    public class SurfaceAudioMapping
    {
        [Tooltip("Nom de la surface (contenu dans le nom du GameObject)")]
        public string surfaceName;
        
        [Tooltip("Sons de pas spécifiques à cette surface")]
        public AudioClip[] audioClips;
        
        public SurfaceAudioMapping(string name, AudioClip[] clips)
        {
            surfaceName = name;
            audioClips = clips;
        }
        
        public SurfaceAudioMapping()
        {
            surfaceName = "";
            audioClips = new AudioClip[0];
        }
    }
    
    // Méthodes publiques pour contrôle externe
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
            // NOUVEAU : Applique aussi le multiplicateur de distance
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
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
        {
            Debug.Log($"🦘💨 Particules saut: {particlesPerStep * 2}");
        }
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
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep))
        {
            Debug.Log($"🎯💨 Particules atterrissage: {particlesPerStep * 3}");
        }
    }
}
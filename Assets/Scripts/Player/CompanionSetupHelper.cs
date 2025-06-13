using UnityEngine;

[System.Serializable]
public class CompanionPreset
{
    public string presetName = "Custom";
    public float followDistance = 3f;
    public float stoppingDistance = 1.5f;
    public float moveSpeed = 3f;
    public float hopHeight = 0.5f;
    public float hopDuration = 0.5f;
    public CompanionController.MovementType movementType = CompanionController.MovementType.Hopping;
}

public class CompanionSetupHelper : MonoBehaviour
{
    public enum PresetType
    {
        Custom,
        Chicken,
        Rabbit,
        Dog,
        Cat,
        Bird,
        Frog
    }
    
    [Header("Quick Presets")]
    public PresetType selectedPreset = PresetType.Custom;
    
    [ContextMenu("Apply Preset")]
    public void ApplyPreset()
    {
        CompanionController companion = GetComponent<CompanionController>();
        if (companion == null)
        {
            Debug.LogError("CompanionController not found!");
            return;
        }
        
        switch (selectedPreset)
        {
            case PresetType.Chicken:
                ApplyChickenPreset(companion);
                break;
                
            case PresetType.Rabbit:
                ApplyRabbitPreset(companion);
                break;
                
            case PresetType.Dog:
                ApplyDogPreset(companion);
                break;
                
            case PresetType.Cat:
                ApplyCatPreset(companion);
                break;
                
            case PresetType.Bird:
                ApplyBirdPreset(companion);
                break;
                
            case PresetType.Frog:
                ApplyFrogPreset(companion);
                break;
        }
        
        Debug.Log($"✅ Preset '{selectedPreset}' appliqué!");
    }
    
    void ApplyChickenPreset(CompanionController c)
    {
        c.movementType = CompanionController.MovementType.AnimationDriven;
        c.followDistance = 3f;
        c.stoppingDistance = 1.5f;
        c.moveSpeed = 7.2f; // 90% de la vitesse du joueur (8 * 0.9)
        c.hopHeight = 0f; // Pas utilisé en AnimationDriven
        c.hopDuration = 0.4f; // Durée de l'animation Jump
        c.hopInterval = 0.2f;
        c.wanderRadius = 2f;
        c.idleTimeBeforeWander = 3f;
        
        // Animations typiques poule
        c.animations.idleAnimation = "Idle";
        c.animations.moveAnimation = "Jump";
        c.animations.happyAnimation = "Jump";
        
        Debug.Log("🐔 Preset Chicken appliqué avec mouvement basé sur l'animation");
        Debug.Log($"  Vitesse: {c.moveSpeed} m/s (comme le joueur)");
    }
    
    void ApplyRabbitPreset(CompanionController c)
    {
        c.movementType = CompanionController.MovementType.Hopping;
        c.followDistance = 4f;
        c.stoppingDistance = 2f;
        c.moveSpeed = 8.8f; // 110% de la vitesse du joueur (8 * 1.1)
        c.hopHeight = 0.6f;
        c.hopDuration = 0.5f;
        c.hopInterval = 0.3f;
        c.wanderRadius = 3f;
    }
    
    void ApplyDogPreset(CompanionController c)
    {
        c.movementType = CompanionController.MovementType.Continuous;
        c.followDistance = 4f;
        c.stoppingDistance = 1.5f;
        c.moveSpeed = 8f; // Même vitesse que le joueur
        c.wanderRadius = 4f;
        c.idleTimeBeforeWander = 5f;
    }
    
    void ApplyCatPreset(CompanionController c)
    {
        c.movementType = CompanionController.MovementType.Continuous;
        c.followDistance = 5f;
        c.stoppingDistance = 2.5f;
        c.moveSpeed = 6.4f; // 80% de la vitesse du joueur (8 * 0.8)
        c.wanderRadius = 5f;
        c.idleTimeBeforeWander = 2f;
    }
    
    void ApplyBirdPreset(CompanionController c)
    {
        c.movementType = CompanionController.MovementType.Hopping;
        c.followDistance = 3f;
        c.stoppingDistance = 2f;
        c.moveSpeed = 5.6f; // 70% de la vitesse du joueur (8 * 0.7)
        c.hopHeight = 0.4f;
        c.hopDuration = 0.3f;
        c.hopInterval = 0.5f;
    }
    
    void ApplyFrogPreset(CompanionController c)
    {
        c.movementType = CompanionController.MovementType.Hopping;
        c.followDistance = 2.5f;
        c.stoppingDistance = 1f;
        c.moveSpeed = 4.8f; // 60% de la vitesse du joueur (8 * 0.6)
        c.hopHeight = 0.8f;
        c.hopDuration = 0.6f;
        c.hopInterval = 1f;
    }
    
    [ContextMenu("Setup Audio Source")]
    public void SetupAudioSource()
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = gameObject.AddComponent<AudioSource>();
        }
        
        audio.spatialBlend = 1f;
        audio.minDistance = 1f;
        audio.maxDistance = 10f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.playOnAwake = false;
        
        Debug.Log("✅ AudioSource configuré pour son 3D");
    }
    
    [ContextMenu("Remove All Generated Sounds")]
    public void RemoveAllGeneratedSounds()
    {
        CompanionController companion = GetComponent<CompanionController>();
        if (companion == null) return;
        
        companion.sounds.idleSounds = null;
        companion.sounds.moveSounds = null;
        companion.sounds.happySounds = null;
        
        Debug.Log("🗑️ Tous les sons ont été retirés. Utilisez de vrais fichiers audio.");
    }
}

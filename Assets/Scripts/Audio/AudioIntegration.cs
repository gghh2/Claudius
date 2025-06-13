using UnityEngine;

// Extension class to add sound support to existing systems
public static class AudioIntegration
{
    // Quest sounds
    public static class QuestSounds
    {
        public const string QUEST_START = "Quest_Start";
        public const string QUEST_COMPLETE = "Quest_Complete";
        public const string QUEST_ITEM_COLLECT = "Quest_ItemCollect";
        public const string QUEST_OBJECTIVE_UPDATE = "Quest_ObjectiveUpdate";
        public const string QUEST_FAILED = "Quest_Failed";
    }
    
    // UI sounds
    public static class UISounds
    {
        public const string BUTTON_CLICK = "UI_Click";
        public const string BUTTON_HOVER = "UI_Hover";
        public const string MENU_OPEN = "UI_MenuOpen";
        public const string MENU_CLOSE = "UI_MenuClose";
        public const string ERROR = "UI_Error";
        public const string SUCCESS = "UI_Success";
        public const string NOTIFICATION = "UI_Notification";
    }
    
    // Player sounds
    public static class PlayerSounds
    {
        public const string FOOTSTEP_WALK = "Player_FootstepWalk";
        public const string FOOTSTEP_RUN = "Player_FootstepRun";
        public const string JUMP = "Player_Jump";
        public const string LAND = "Player_Land";
        public const string DAMAGE = "Player_Damage";
        public const string HEAL = "Player_Heal";
        public const string DEATH = "Player_Death";
    }
    
    // Combat sounds
    public static class CombatSounds
    {
        public const string WEAPON_SWING = "Combat_WeaponSwing";
        public const string WEAPON_HIT = "Combat_WeaponHit";
        public const string SHIELD_BLOCK = "Combat_ShieldBlock";
        public const string CRITICAL_HIT = "Combat_CriticalHit";
        public const string MISS = "Combat_Miss";
    }
    
    // Helper methods
    public static void PlayQuestSound(string soundName)
    {
        if (SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.PlaySound(soundName);
        }
    }
    
    public static void PlayUISound(string soundName)
    {
        if (SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.PlaySound(soundName);
        }
    }
    
    public static void PlaySoundAt(string soundName, Vector3 position)
    {
        if (SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.PlaySound(soundName, position);
        }
    }
    
    // Note: Pour les sons d'ambiance, utilisez directement AmbientSoundZone
    // en attachant le script à un GameObject avec un collider trigger
    
    public static void PlayMusicForZone(MusicZoneType zone)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetZone(zone);
        }
    }
    
    public static void PlayCombatMusic()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayCombatMusic();
        }
    }
    
    public static void PlayVictoryMusic()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayVictoryMusic();
        }
    }
}

// Example component showing how to use the audio system
public class AudioExample : MonoBehaviour
{
    [Header("Example Sound Triggers")]
    public bool playFootstepOnMove = true;
    public float footstepInterval = 0.5f;
    
    private float lastFootstepTime;
    private bool isMoving;
    
    void Update()
    {
        // Example: Play footstep sounds when moving
        if (playFootstepOnMove)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
            
            if (isMoving && Time.time - lastFootstepTime > footstepInterval)
            {
                AudioIntegration.PlaySoundAt(AudioIntegration.PlayerSounds.FOOTSTEP_WALK, transform.position);
                lastFootstepTime = Time.time;
            }
        }
        
        // Example: Play jump sound
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AudioIntegration.PlaySoundAt(AudioIntegration.PlayerSounds.JUMP, transform.position);
        }
    }
    
    // Example: Play sound when entering a trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("QuestItem"))
        {
            AudioIntegration.PlayQuestSound(AudioIntegration.QuestSounds.QUEST_ITEM_COLLECT);
        }
    }
}

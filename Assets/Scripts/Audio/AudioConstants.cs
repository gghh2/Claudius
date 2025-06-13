using UnityEngine;

/// <summary>
/// Central audio constants and helper methods
/// </summary>
public static class AudioConstants
{
    // Quest sounds
    public static class Quest
    {
        public const string START = "Quest_Start";
        public const string COMPLETE = "Quest_Complete";
        public const string ITEM_COLLECT = "Quest_ItemCollect";
        public const string UPDATE = "Quest_ObjectiveUpdate";
        public const string FAILED = "Quest_Failed";
    }
    
    // UI sounds
    public static class UI
    {
        public const string CLICK = "UI_Click";
        public const string HOVER = "UI_Hover";
        public const string OPEN = "UI_MenuOpen";
        public const string CLOSE = "UI_MenuClose";
        public const string ERROR = "UI_Error";
        public const string SUCCESS = "UI_Success";
    }
    
    // Player sounds
    public static class Player
    {
        public const string JUMP = "Player_Jump";
        public const string LAND = "Player_Land";
        public const string DAMAGE = "Player_Damage";
    }
    
    // Quick play methods
    public static void PlaySound(string soundName)
    {
        SoundEffectsManager.Instance?.PlaySound(soundName);
    }
    
    public static void PlaySoundAt(string soundName, Vector3 position)
    {
        SoundEffectsManager.Instance?.PlaySound(soundName, position);
    }
}

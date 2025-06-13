using UnityEngine;

public class CompanionAnimatorDebug : MonoBehaviour
{
    private CompanionController companion;
    private Animator animator;
    private Animation legacyAnimation;
    
    void Start()
    {
        companion = GetComponent<CompanionController>();
        animator = GetComponentInChildren<Animator>();
        legacyAnimation = GetComponentInChildren<Animation>();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            DebugAnimationSystem();
        }
    }
    
    void DebugAnimationSystem()
    {
        Debug.Log("=== COMPANION ANIMATION DEBUG ===");
        
        if (animator != null)
        {
            Debug.Log("✅ Utilise ANIMATOR Controller");
            Debug.Log($"  Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "AUCUN")}");
            Debug.Log($"  Apply Root Motion: {animator.applyRootMotion}");
            
            Debug.Log("  Paramètres:");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        Debug.Log($"    - {param.name} (Bool) = {animator.GetBool(param.name)}");
                        break;
                    case AnimatorControllerParameterType.Float:
                        Debug.Log($"    - {param.name} (Float) = {animator.GetFloat(param.name)}");
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        Debug.Log($"    - {param.name} (Trigger)");
                        break;
                }
            }
            
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"  État actuel: {stateInfo.shortNameHash}");
        }
        else if (legacyAnimation != null)
        {
            Debug.Log("✅ Utilise LEGACY Animation");
            Debug.Log("  Clips disponibles:");
            foreach (AnimationState state in legacyAnimation)
            {
                Debug.Log($"    - {state.name} (durée: {state.length}s)");
            }
            Debug.Log($"  En cours: {(legacyAnimation.isPlaying ? legacyAnimation.clip?.name : "Aucune")}");
            Debug.Log($"  Play Automatically: {legacyAnimation.playAutomatically}");
        }
        else
        {
            Debug.LogError("❌ AUCUN système d'animation trouvé!");
            Debug.Log("  Ajoutez un composant Animation ou Animator sur votre prefab Chicken");
        }
        
        if (companion != null)
        {
            Debug.Log("\n📋 Configuration CompanionController:");
            Debug.Log($"  Movement Type: {companion.movementType}");
            Debug.Log($"  Animations configurées:");
            Debug.Log($"    - Idle: '{companion.animations.idleAnimation}'");
            Debug.Log($"    - Move: '{companion.animations.moveAnimation}'");
            Debug.Log($"    - Happy: '{companion.animations.happyAnimation}'");
        }
    }
    
    void OnGUI()
    {
        if (!Input.GetKey(KeyCode.F8)) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Box("=== ANIMATION DEBUG (F8) ===");
        
        if (animator != null)
        {
            GUILayout.Label("MODE: Animator Controller");
            if (animator.GetBool("IsMoving"))
            {
                GUILayout.Label("État: EN MOUVEMENT", GUI.skin.box);
            }
            else
            {
                GUILayout.Label("État: IDLE", GUI.skin.box);
            }
        }
        else if (legacyAnimation != null)
        {
            GUILayout.Label("MODE: Legacy Animation");
            GUILayout.Label($"Animation: {(legacyAnimation.isPlaying ? legacyAnimation.clip?.name : "Aucune")}");
        }
        else
        {
            GUILayout.Label("ERREUR: Aucun système d'animation!");
        }
        
        if (companion != null)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Movement: {companion.movementType}");
            GUILayout.Label($"Is Moving: {(companion.enabled ? "OUI" : "NON")}");
        }
        
        GUILayout.EndArea();
    }
}

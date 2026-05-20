using UnityEngine;

/// <summary>
/// Contrôleur de curseur — responsabilité unique : maintenir l'état du curseur
/// cohérent avec l'état de l'UI.
///
/// Règle unique : un panel UI est ouvert → curseur visible et libre ;
/// sinon (en jeu) → curseur caché et verrouillé.
///
/// Source de vérité : <see cref="UnifiedUIManager.IsShowingPanel"/>.
/// AUCUNE autre classe ne doit toucher Cursor.visible / Cursor.lockState.
/// </summary>
public class SmartCursorManager : MonoBehaviour
{
    private UnifiedUIManager uiManager;

    void Start()
    {
        uiManager = FindFirstObjectByType<UnifiedUIManager>();
        Apply();
    }

    void Update()
    {
        // Appliqué chaque frame : auto-correctif si l'état dérive.
        Apply();
    }

    void Apply()
    {
        bool uiOpen = uiManager != null && uiManager.IsShowingPanel;
        Cursor.visible = uiOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void OnDisable()
    {
        // Composant désactivé ou sortie du Play mode : on libère le curseur
        // pour qu'il ne reste pas caché/verrouillé dans l'éditeur.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

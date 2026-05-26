using UnityEngine;

/// <summary>
/// Contrôleur de curseur — responsabilité unique : maintenir l'état du curseur
/// cohérent avec l'état de l'UI.
///
/// Règle : un panel UI est ouvert → curseur visible et libre ;
/// sinon (en jeu) → curseur caché et verrouillé.
/// Scène sans UnifiedUIManager (ex. MainMenu) → on n'est pas en jeu :
/// curseur visible et libre.
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
        // Pas de UnifiedUIManager dans la scène (ex. MainMenu) → on n'est pas
        // en jeu : curseur libre. En jeu, l'état suit l'ouverture des panels.
        bool cursorFree = uiManager == null || uiManager.IsShowingPanel;

        // Cas spécial : UIs hors UnifiedUIManager (boutique, panneau de lecture,
        // console dev) doivent aussi libérer le curseur. On consulte leurs
        // singletons directement.
        if (!cursorFree && IsExternalUiActive()) cursorFree = true;

        Cursor.visible = cursorFree;
        Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
    }

    static bool IsExternalUiActive()
    {
        if (ShopUI.Instance != null && ShopUI.Instance.IsOpen) return true;
        if (ReaderPanel.Instance != null && ReaderPanel.Instance.IsOpen) return true;
        if (DevConsole.Instance != null && DevConsole.Instance.IsOpen) return true;
        return false;
    }

    void OnDisable()
    {
        // Composant désactivé ou sortie du Play mode : on libère le curseur
        // pour qu'il ne reste pas caché/verrouillé dans l'éditeur.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

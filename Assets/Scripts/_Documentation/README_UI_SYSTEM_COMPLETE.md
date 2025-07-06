# Système de Navigation UI - Documentation Complète

## Vue d'ensemble

Le système de navigation UI gère l'interface utilisateur du jeu avec une architecture séparée entre le menu principal et le jeu.

### Architecture par scène

```
MainMenu (scène)
├── MainMenuUI (gère ses panels directement)
├── PAS de UnifiedUIManager
└── Navigation simple sans historique

Game (scène)
├── UnifiedUIManager (singleton DontDestroyOnLoad)
├── Navigation avec pile (historique)
└── Gestion centralisée de tous les panels UI
```

## ⚠️ IMPORTANT : Séparation MainMenu / Game

### MainMenu NE DOIT PAS utiliser UnifiedUIManager
- MainMenuUI gère directement ses panels (MainMenuPanel, OptionsPanel, LoadGamePanel, etc.)
- Pas de navigation complexe nécessaire
- Évite la création d'un singleton vide

### Game utilise UnifiedUIManager
- Créé dans la scène Game avec toutes les références
- Devient DontDestroyOnLoad après création
- Gère toute la navigation in-game

## Architecture détaillée

### 1. UnifiedUIManager (Game uniquement)
- **Singleton avec DontDestroyOnLoad**
- Navigation en pile (stack) pour l'historique
- Gestion automatique de la touche ESCAPE
- Support des dialogues modaux
- Contrôle du Time.timeScale et du joueur
- Gestion des layers UI

### 2. MainMenuUI (MainMenu uniquement)
- Gestion directe des panels du menu principal
- Pas de navigation complexe
- Transitions vers la scène Game

### 3. UINavigationButton (Helper universel)
- Configuration facile dans l'Inspector
- Prévention des double-clics
- Support de NavigateTo et NavigateBack

## Configuration dans Unity

### Dans la scène MainMenu

1. **GameObject Canvas** avec MainMenuUI
2. **Panels directement dans Canvas** :
   - MainMenuPanel (actif par défaut)
   - OptionsPanel (inactif)
   - LoadGamePanel (inactif)
   - CreditsPanel (inactif)
3. **PAS de UnifiedUIManager !**

### Dans la scène Game

1. **UnifiedUIManager DOIT être dans la scène** :
   - Il devrait être sous Manager/UnifiedUIManager
   - Vérifiez que le composant UnifiedUIManager est attaché
   - DontDestroyOnLoad est géré automatiquement par le script
   
   **IMPORTANT** : Ne pas compter sur la création automatique du Singleton !
   L'UnifiedUIManager doit exister dans la scène avec toutes ses références.

2. **Assignez TOUS les panels** dans l'Inspector :
   - **Pause Menu Panel** : Canvas/PauseMenuPanel
   - **Options Panel** : Canvas/OptionsPanel
   - **Save Menu Panel** : Canvas/SaveMenuPanel
   - **Dialogue Panel** : Canvas/DialoguePanel
   - **History Panel** : Canvas/HistoryPanel
   - **Inventory Panel** : Canvas/InventoryPanel
   - **Quest Journal Panel** : Canvas/QuestJournalPanel
   - **Confirm Dialog** : Canvas/ConfirmDialog
   - **Notification Panel** : Canvas/NotificationPanel

3. **Configuration des layers** (automatique) :
   - Base UI : 0
   - Dialogue : 10
   - Pause Menu : 20
   - Save Menu : 30
   - Confirm Dialog : 40
   - Notification : 50

## Navigation dans le jeu

### Flux de navigation

```
GAME (État de jeu)
├── [ESC] → PauseMenu
│   ├── Options → [ESC/Back] → PauseMenu
│   ├── Save/Load → [ESC/Back] → PauseMenu
│   └── [ESC/Resume] → GAME
├── [J] → QuestJournal → [ESC] → GAME
├── [I] → Inventory → [ESC] → GAME
└── Dialogue → [ESC] → GAME
    └── History → [ESC] → Dialogue
```

### Raccourcis clavier (Game uniquement)
- **ESC** : Retour/Ouvre PauseMenu
- **J** : Journal de quêtes
- **I** : Inventaire

### Configuration des boutons avec UINavigationButton

1. Ajoutez le composant UINavigationButton
2. Configurez :
   - **Navigation Type** : NavigateTo ou NavigateBack
   - **Panel Choice** : Sélectionnez dans le dropdown
   - **Target Panel** : S'affiche automatiquement

## API et utilisation

### Dans MainMenu (sans UnifiedUIManager)

```csharp
// Ouvrir un panel directement
GameObject optionsPanel = GameObject.Find("OptionsPanel");
if (optionsPanel != null)
{
    optionsPanel.SetActive(true);
    mainMenuPanel.SetActive(false);
}

// Transition vers le jeu
Time.timeScale = 1f;
Cursor.visible = false;
Cursor.lockState = CursorLockMode.Locked;
```

### Dans Game (avec UnifiedUIManager)

```csharp
// Navigation standard
UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.Inventory);
UnifiedUIManager.Instance.NavigateBack();

// Vérifications
if (UnifiedUIManager.Instance.IsPanelOpen(UnifiedUIPanelNames.PauseMenu))
{
    // Le menu pause est ouvert
}

// Reset complet
UnifiedUIManager.Instance.ResetToGame();
```

## Résolution de problèmes

### ESC ne fonctionne pas après chargement

**Cause** : Conflit de singletons si MainMenu utilise UnifiedUIManager

**Solution** : 
1. MainMenu ne doit PAS appeler UnifiedUIManager.Instance
2. Vérifiez que UnifiedUIManager existe uniquement dans Game
3. Lancez depuis MainMenu pour tester

### "Panel not found in configs: MainMenu"

**Cause** : MainMenuUI essaie d'utiliser UnifiedUIManager

**Solution** : MainMenuUI doit gérer ses panels directement

### UnifiedUIManager dans DontDestroyOnLoad au lieu de la hiérarchie

**Cause** : Il a été créé automatiquement (singleton vide)

**Solution** : 
1. Supprimez-le des DontDestroyOnLoad
2. Relancez depuis MainMenu (pas directement Game)
3. Vérifiez que MainMenuUI n'appelle pas UnifiedUIManager

### Le joueur ne peut plus bouger

**Solutions** :
1. Menu contextuel sur UnifiedUIManager → "Reset to Game"
2. Vérifiez Time.timeScale = 1
3. Vérifiez que PlayerControllerCC est activé

## Bonnes pratiques

### DO ✅
- Créez UnifiedUIManager UNIQUEMENT dans Game
- Assignez TOUTES les références dans l'Inspector
- Utilisez UINavigationButton pour la navigation
- Gardez MainMenu simple sans UnifiedUIManager

### DON'T ❌
- N'appelez pas UnifiedUIManager depuis MainMenu
- Ne créez pas UnifiedUIManager dans MainMenu
- Ne mélangez pas navigation directe et UnifiedUIManager
- N'oubliez pas d'assigner les références

## Architecture des fichiers

```
Scripts/
├── UI/
│   ├── Navigation/
│   │   ├── UnifiedUIManager.cs      (Game only)
│   │   ├── UINavigationButton.cs    (Universal)
│   │   └── ConfirmationDialogManager.cs
│   ├── MainMenu/
│   │   └── MainMenuUI.cs           (MainMenu only)
│   └── [Autres UI scripts...]
```

## Historique des corrections

### v3.0 - Séparation MainMenu/Game (actuel)
- MainMenu n'utilise plus UnifiedUIManager
- Fix du conflit de singletons
- ESC fonctionne correctement après transitions

### v2.0 - Navigation unifiée
- Support ESCAPE complet
- Dialogues modaux
- Navigation en pile

### v1.0 - Version initiale
- Navigation basique
- Multiples systèmes non unifiés

## Notes techniques

- UnifiedUIManager utilise DontDestroyOnLoad
- Les références GameObject ne survivent pas aux changements de scène
- MainMenu est une scène "stateless" (pas d'historique de navigation)
- Game maintient l'état de navigation avec la pile

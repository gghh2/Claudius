# Système de Navigation UI - Documentation Finale

## Vue d'ensemble

Le système de navigation UI utilise **UnifiedUIManager** comme contrôleur central avec **UINavigationButton** pour configurer facilement les boutons dans l'éditeur Unity.

## Architecture

### Composants principaux

1. **UnifiedUIManager** : Gestionnaire central de navigation
   - Navigation en pile (stack) pour l'historique
   - Gestion automatique de la touche ESCAPE
   - Support des dialogues modaux
   - Contrôle du Time.timeScale et du joueur

2. **UINavigationButton** : Helper pour les boutons
   - Configuration facile dans l'Inspector
   - Prévention des double-clics
   - Support de NavigateTo et NavigateBack

3. **UnifiedUIPanelNames** : Constantes pour les noms de panels
   - Évite les erreurs de frappe
   - Centralise les noms

## Configuration dans Unity

### 1. Setup UnifiedUIManager

1. Créez un GameObject vide "UnifiedUIManager"
2. Ajoutez le composant UnifiedUIManager
3. Dans l'Inspector, assignez tous vos panels :
   - Pause Menu Panel
   - Options Panel
   - Save Menu Panel
   - Dialogue Panel
   - History Panel
   - Inventory Panel
   - Quest Journal Panel
   - Confirm Dialog
   - Notification Panel

### 2. Configuration des boutons

Pour chaque bouton de navigation :

1. Ajoutez le composant **UINavigationButton**
2. Configurez :
   - **Navigation Type** :
     - `NavigateTo` : Pour aller vers un panel spécifique
     - `NavigateBack` : Pour revenir au panel précédent
   - **Panel Choice** : Sélectionnez le panel cible dans le dropdown
   - **Target Panel** : S'affiche automatiquement

### 3. Exemples de configuration

#### Bouton Options dans PauseMenu
- Navigation Type : `NavigateTo`
- Panel Choice : `Settings`

#### Bouton Back dans Options
- Navigation Type : `NavigateBack`
- Panel Choice : `Custom` (laissez vide)

#### Bouton Resume dans PauseMenu
- Navigation Type : `NavigateBack`
- Panel Choice : `Custom`

## Navigation

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

### Raccourcis clavier

- **ESC** : Retour au panel précédent (ou ouvre PauseMenu depuis le jeu)
- **J** : Ouvre le journal de quêtes (uniquement depuis le jeu)
- **I** : Ouvre l'inventaire (uniquement depuis le jeu)

### Dialogues modaux

Les dialogues de confirmation bloquent toute interaction :
- ESCAPE désactivé
- Raccourcis désactivés
- L'utilisateur DOIT cliquer Oui ou Non

## API Code

### Navigation basique

```csharp
// Ouvrir un panel
UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.Inventory);

// Retour au panel précédent
UnifiedUIManager.Instance.NavigateBack();

// Vérifier si un panel est ouvert
if (UnifiedUIManager.Instance.IsPanelOpen(UnifiedUIPanelNames.PauseMenu))
{
    // Le menu pause est ouvert
}
```

### Dialogues modaux

```csharp
// Utiliser ConfirmationDialogManager (si configuré)
ConfirmationDialogManager.Instance.ShowYesNoDialog(
    "Êtes-vous sûr?",
    onYes: () => { /* Action */ },
    onNo: () => { /* Annulé */ }
);

// Ou directement via UnifiedUIManager
UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.Confirmation);
// Puis fermer avec :
UnifiedUIManager.Instance.CloseModalDialog(UnifiedUIPanelNames.Confirmation);
```

## Résolution de problèmes

### Le joueur ne peut plus bouger après fermeture d'un menu
- Vérifiez que Time.timeScale est bien à 1
- Utilisez le menu contextuel "Debug - Reset to Game" sur UnifiedUIManager

### Double navigation (retour direct au jeu)
- Vérifiez que le bouton n'a qu'UN SEUL listener
- Supprimez les anciens listeners OnClick dans Button
- Gardez seulement UINavigationButton

### Les raccourcis J/I ne fonctionnent pas
- Ils ne fonctionnent QUE depuis l'état de jeu
- Vérifiez qu'aucun panel n'est ouvert

### ESCAPE ne fonctionne pas
- Vérifiez qu'aucun dialogue modal n'est ouvert
- Certains panels peuvent bloquer ESCAPE (ex: Confirmation)

## Notes importantes

1. **Ne pas mélanger** les systèmes de navigation :
   - Utilisez SOIT UINavigationButton
   - SOIT des appels directs à UnifiedUIManager
   - Pas les deux sur le même bouton

2. **Time.timeScale** est géré automatiquement :
   - Mis à 0 quand un panel bloque le gameplay
   - Remis à 1 quand on retourne au jeu

3. **Le contrôle du joueur** est géré automatiquement :
   - Désactivé quand un panel est ouvert
   - Réactivé quand on retourne au jeu

## Fichiers du système

- `UnifiedUIManager.cs` : Gestionnaire principal
- `UINavigationButton.cs` : Helper pour les boutons
- `UnifiedUIPanelNames.cs` : Dans UnifiedUIManager.cs
- `ModernPauseMenu.cs` : Contrôleur du menu pause (utilise UnifiedUIManager)
- Autres scripts UI : Utilisent OpenPanel/ClosePanel pour compatibilité

## Version

v2.0 - Système de navigation unifié avec support complet ESCAPE et dialogues modaux

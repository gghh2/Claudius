# Système de Notifications - Documentation Complète

## Vue d'ensemble

Le système de notifications permet d'afficher des messages temporaires à l'écran pour informer le joueur des événements importants (quêtes, collecte d'objets, erreurs, etc.).

## Architecture

### Composants principaux

1. **NotificationManager.cs** : Gestionnaire singleton des notifications
   - Gère l'affichage des messages
   - Contrôle les animations de fade in/out
   - Supporte différents types de notifications (Success, Error, Info, Warning)
   - Assure le layer ordering automatique

2. **NotificationPanel** : UI GameObject dans Canvas
   - Contient le TextMeshProUGUI pour le message
   - Image component pour le fond coloré
   - CanvasGroup pour les animations de transparence

3. **NotificationTester.cs** : Script de test (optionnel)
   - Permet de tester rapidement le système avec les touches 1-5

## Configuration dans Unity

### Setup du NotificationManager

1. **Dans la scène Game**, créez un GameObject "NotificationManager"
2. Ajoutez le composant `NotificationManager`
3. Dans l'Inspector, assignez :
   - **Notification Panel** : Canvas/NotificationPanel
   - **Notification Text** : Auto-détecté (TextMeshProUGUI dans NotificationPanel)
   - **Background Image** : Auto-détecté (Image component du NotificationPanel)

### Structure requise du NotificationPanel

```
Canvas
└── NotificationPanel (GameObject)
    ├── Image Component (fond coloré)
    ├── CanvasGroup Component (ajouté automatiquement)
    └── Text (TextMeshProUGUI) (message)
```

### Paramètres configurables

- **Default Duration** : 2 secondes (durée d'affichage par défaut)
- **Fade In Time** : 0.3 secondes
- **Fade Out Time** : 0.3 secondes
- **Couleurs par type** :
  - Success : Vert (0.2, 0.8, 0.2, 0.9)
  - Error : Rouge (0.8, 0.2, 0.2, 0.9)
  - Info : Bleu (0.2, 0.5, 0.8, 0.9)
  - Warning : Orange (0.8, 0.6, 0.2, 0.9)

## Utilisation

### API basique

```csharp
// Vérifier que l'instance existe toujours
if (NotificationManager.Instance != null)
{
    // Success
    NotificationManager.Instance.ShowSuccess("Quête complétée !");
    
    // Error
    NotificationManager.Instance.ShowError("Action impossible !");
    
    // Info
    NotificationManager.Instance.ShowInfo("Nouvel objet découvert");
    
    // Warning
    NotificationManager.Instance.ShowWarning("Attention, zone dangereuse !");
}
```

### API avancée

```csharp
// Notification avec durée personnalisée
NotificationManager.Instance.ShowNotification(
    "Message personnalisé", 
    NotificationType.Info, 
    5f  // 5 secondes
);

// Cacher la notification actuelle
NotificationManager.Instance.HideNotification();
```

### Intégration avec les systèmes

Le système est déjà intégré dans :
- **SaveSystemUI** : Notifications de sauvegarde/chargement
- **QuestManager** : Notifications de début/fin de quête
- **DialogueUI** : Notifications d'acceptation de quête
- **ConfirmationDialogManager** : Confirmation de suppression

## Tests et Debug

### Méthode 1 : NotificationTester
1. Ajoutez `NotificationTester` sur n'importe quel GameObject
2. En jeu, utilisez les touches :
   - **1** : Test Success
   - **2** : Test Error
   - **3** : Test Info
   - **4** : Test Warning
   - **5** : Test longue durée

### Méthode 2 : Context Menu
1. Sélectionnez le GameObject avec NotificationManager
2. Clic droit sur le composant dans l'Inspector
3. Choisissez un test dans le menu contextuel

### Debug logs
- Activez `Enable Debug Logs` dans l'Inspector
- Surveillez la console pour les messages [NotificationManager]

## Résolution de problèmes

### "No instance found in scene!"
**Cause** : NotificationManager absent de la scène
**Solution** : Ajoutez un GameObject avec le composant NotificationManager

### Notifications invisibles
**Causes possibles** :
- NotificationPanel masqué par d'autres UI
- Canvas inactif
- Mauvaise position du panel

**Solutions** :
- Vérifiez la hiérarchie UI
- Le système met automatiquement sortingOrder à 100
- Vérifiez que NotificationPanel est bien positionné à l'écran

### Références non assignées
**Solution** : Utilisez le bouton "Auto-Assign References" dans le contexte menu du composant

## Historique des corrections

### Bug après LOAD (résolu)
- **Problème** : NotificationManager n'existait pas dans la scène Game
- **Solution** : Ajout manuel du GameObject NotificationManager
- **Date** : Janvier 2025

## Notes importantes

1. **Singleton** : Une seule instance par scène
2. **Layer ordering** : Automatiquement mis à 100 pour être au-dessus
3. **Animations** : Utilise Time.unscaledDeltaTime (fonctionne même si le jeu est en pause)
4. **Queue** : Une seule notification à la fois (les nouvelles remplacent les anciennes)

## Extensions possibles

- File d'attente pour plusieurs notifications
- Sons par type de notification
- Animations d'entrée/sortie personnalisées
- Icônes par type de notification
- Positionnement configurable (haut/bas/coins)

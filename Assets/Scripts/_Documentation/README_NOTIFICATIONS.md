# Système de Notifications - Guide de Configuration

## 🔧 Configuration dans Unity

### 1. Localiser ou créer le NotificationManager

Le `NotificationManager` doit être attaché à un GameObject dans votre scène. Deux options :

**Option A : Sur le CursorManager (recommandé)**
- Sélectionnez le GameObject `CursorManager` dans la hiérarchie
- Ajoutez le component `NotificationManager` (Add Component > Scripts > NotificationManager)

**Option B : Sur un GameObject dédié**
- Créez un GameObject vide : `GameObject > Create Empty`
- Renommez-le `NotificationManager`
- Ajoutez le component `NotificationManager`

### 2. Assigner les références dans l'inspecteur

Dans le component `NotificationManager`, assignez :

1. **Notification Panel** : Glissez le `NotificationPanel` depuis la hiérarchie
2. **Notification Text** : 
   - Trouvez le TextMeshProUGUI enfant du NotificationPanel
   - Ou laissez vide, le système le trouvera automatiquement
3. **Background Image** :
   - L'Image component du NotificationPanel
   - Ou laissez vide, le système le trouvera automatiquement

### 3. Activer les logs de debug

- Cochez `Enable Debug Logs` dans l'inspecteur du NotificationManager
- Cela vous aidera à diagnostiquer les problèmes

## 🧪 Tester le système

### Méthode 1 : Script de test (recommandé)

1. Ajoutez le component `NotificationTester` à n'importe quel GameObject actif
2. Lancez le jeu
3. Utilisez les touches :
   - **1** : Notification de succès
   - **2** : Notification d'erreur
   - **3** : Notification d'info
   - **4** : Notification d'avertissement
   - **5** : Notification longue (5 secondes)

### Méthode 2 : Menu contextuel

1. En mode Play ou Edit
2. Sélectionnez le GameObject avec le NotificationManager
3. Clic droit sur le component NotificationManager
4. Choisissez une des options de test :
   - Test Success Notification
   - Test Error Notification
   - Test Info Notification
   - Test Warning Notification

## 🎯 Structure du NotificationPanel

Votre `NotificationPanel` devrait avoir cette structure :

```
NotificationPanel (GameObject)
├── Image Component (pour le fond coloré)
├── CanvasGroup Component (ajouté automatiquement pour le fade)
└── Text (TextMeshProUGUI) (pour afficher le message)
```

## 🔍 Diagnostic des problèmes

### Le NotificationManager n'est pas trouvé

**Symptôme** : `[NotificationManager] No instance found in scene!`

**Solution** :
1. Assurez-vous qu'un GameObject a le component NotificationManager
2. Ce GameObject doit être actif dans la scène

### Les références ne sont pas assignées

**Symptôme** : `[NotificationManager] NotificationPanel is not assigned and couldn't be found!`

**Solution** :
1. Assignez manuellement le NotificationPanel dans l'inspecteur
2. Ou assurez-vous qu'il s'appelle exactement "NotificationPanel"

### Les notifications ne s'affichent pas

**Symptômes possibles** :
- Pas d'erreur mais rien ne s'affiche
- Le panel est activé mais invisible

**Solutions** :
1. Vérifiez que le NotificationPanel n'est pas masqué par d'autres UI
2. Vérifiez la position du NotificationPanel (devrait être visible à l'écran)
3. Vérifiez que le Canvas parent est actif
4. Vérifiez le sorting order (le système le met à 100 automatiquement)

## 📝 Utilisation dans votre code

Une fois configuré, utilisez les notifications ainsi :

```csharp
// Notification de succès
if (NotificationManager.Instance != null)
{
    NotificationManager.Instance.ShowSuccess("Quête terminée !");
}

// Notification d'erreur
if (NotificationManager.Instance != null)
{
    NotificationManager.Instance.ShowError("Action impossible !");
}

// Notification d'info
if (NotificationManager.Instance != null)
{
    NotificationManager.Instance.ShowInfo("Nouvel objet découvert");
}

// Notification personnalisée avec durée
if (NotificationManager.Instance != null)
{
    NotificationManager.Instance.ShowNotification("Message custom", NotificationType.Warning, 5f);
}
```

## ✅ Prochaines étapes

Une fois que le système fonctionne, nous pourrons ajouter des notifications pour :
- Début de quête
- Collecte d'objets
- Fin de quête
- Autres événements importants
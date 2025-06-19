# Système de Sauvegarde - Guide d'utilisation

## Vue d'ensemble

Le système de sauvegarde permet de sauvegarder et charger l'état complet du jeu incluant :
- Position et état du joueur
- Companion (position, type)
- Quêtes actives et complétées
- Positions des NPCs
- Inventaire
- Paramètres audio

## Installation

### 1. Ajouter le SaveGameManager

1. Créez un GameObject vide nommé "SaveGameManager"
2. Ajoutez le script `SaveGameManager`
3. Configurez :
   - **Save File Name** : "savegame" (nom par défaut)
   - **Auto Save Interval** : 60 (sauvegarde auto toutes les 60 secondes)
   - **Debug Mode** : ✓ (pour voir les logs)

### 2. Interface utilisateur (optionnel)

Pour une interface de sauvegarde :

1. Créez un Canvas si vous n'en avez pas
2. Créez la structure UI suivante :
```
Canvas
├── SaveMenu (Panel)
│   ├── SaveSlotContainer
│   │   └── SaveSlotPrefab (x10)
│   └── CloseButton
├── ConfirmDialog (Panel)
│   ├── ConfirmText
│   ├── YesButton
│   └── NoButton
└── NotificationPanel
    └── NotificationText
```

3. Ajoutez `SaveGameUI` sur un GameObject
4. Assignez toutes les références UI

### 3. Raccourcis clavier (optionnel)

1. Ajoutez `SaveGameKeybinds` sur un GameObject
2. Configurez les touches :
   - **F5** : Sauvegarde rapide
   - **F9** : Chargement rapide
   - **Ctrl+S** : Menu de sauvegarde

## Utilisation

### Sauvegarder par code

```csharp
// Sauvegarde simple
SaveGameManager.Instance.SaveGame();

// Sauvegarde avec nom spécifique
SaveGameManager.Instance.SaveGame("checkpoint_1");

// Vérifier si une sauvegarde existe
if (SaveGameManager.Instance.SaveExists("checkpoint_1"))
{
    // La sauvegarde existe
}
```

### Charger par code

```csharp
// Charger la sauvegarde par défaut
SaveGameManager.Instance.LoadGame();

// Charger une sauvegarde spécifique
SaveGameManager.Instance.LoadGame("checkpoint_1");
```

### Zones de sauvegarde automatique

1. Créez un GameObject avec un Collider (Trigger)
2. Ajoutez le script `AutoSaveTrigger`
3. Configurez :
   - **Save Name** : Nom de la sauvegarde
   - **One Time Only** : Sauvegarde unique
   - **Trigger Message** : Message affiché

## Ce qui est sauvegardé

### Joueur
- Position et rotation
- Stamina actuelle
- Santé (si implémentée)

### Companion
- Présence du companion
- Type de companion
- Position et rotation

### Quêtes
- Toutes les quêtes actives
- Progression de chaque quête
- Quête suivie actuellement
- Quêtes complétées

### NPCs
- Position de chaque NPC
- Rotation
- État actif/inactif

### Inventaire
- Tous les objets
- Quantités
- Association avec les quêtes

### Paramètres
- Volume principal
- Volume musique
- Volume effets sonores

## Format de sauvegarde

Les sauvegardes sont stockées en JSON dans :
- **Windows** : `%APPDATA%/../LocalLow/[CompanyName]/[GameName]/saves/`
- **Mac** : `~/Library/Application Support/[CompanyName]/[GameName]/saves/`
- **Linux** : `~/.config/unity3d/[CompanyName]/[GameName]/saves/`

Format : `[saveName].json`

## Étendre le système

### Ajouter des données personnalisées

1. Créez une nouvelle classe de données :
```csharp
[System.Serializable]
public class MyCustomData
{
    public int score;
    public float playTime;
}
```

2. Ajoutez-la à `SaveData` :
```csharp
public MyCustomData customData;
```

3. Collectez les données dans `CollectSaveData()`
4. Appliquez les données dans `ApplySaveData()`

### Rendre un objet sauvegardable

1. Ajoutez le composant `SaveableObject` sur l'objet
2. Configurez :
   - **Save Position** : ✓
   - **Save Rotation** : ✓
   - **Save Active** : ✓

## Bonnes pratiques

1. **Sauvegarde automatique** : Utilisez les zones de sauvegarde avant les combats difficiles
2. **Multiple slots** : Encouragez les joueurs à utiliser plusieurs slots
3. **Notifications** : Affichez toujours quand le jeu sauvegarde
4. **Validation** : Vérifiez l'intégrité des données au chargement

## Dépannage

### La sauvegarde ne fonctionne pas
- Vérifiez que `SaveGameManager` existe dans la scène
- Vérifiez les permissions d'écriture
- Regardez la console pour les erreurs

### Les quêtes ne se chargent pas
- Assurez-vous que `QuestJournal` a la méthode `ClearAllQuests()`
- Vérifiez que les IDs de quête sont uniques

### Les NPCs ne se restaurent pas
- Les NPCs doivent avoir des noms uniques
- Vérifiez que les NPCs existent dans la scène au chargement

## Performance

- Les sauvegardes sont asynchrones (pas de freeze)
- Taille moyenne d'une sauvegarde : ~10-50 KB
- Temps de sauvegarde : < 100ms
- Temps de chargement : < 200ms

## Sécurité

Les sauvegardes sont en texte clair (JSON). Pour un jeu commercial :
1. Chiffrez les données sensibles
2. Ajoutez une somme de contrôle
3. Compressez les fichiers volumineux
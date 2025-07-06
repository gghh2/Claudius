# 🎮 Système de Sauvegarde - Documentation

## Vue d'ensemble

Le système de sauvegarde permet de sauvegarder et charger l'état complet du jeu incluant :
- Position et état du joueur
- Stamina et santé
- **Zoom de la caméra** (orthographicSize ou FOV)
- Companion (position, type)
- **Quêtes actives, complétées ET annulées** (NOUVEAU)
- **Positions exactes des objets de quête** (NOUVEAU)
- Positions des NPCs
- Inventaire
- Paramètres audio

## Architecture

### Fichiers principaux (4 scripts seulement)

1. **SaveGameManager.cs** : Gestionnaire principal
   - Gère la sauvegarde/chargement des données
   - Sérialise en JSON
   - Pas d'autosave (supprimé)
   - **Gère la recréation des quêtes et leurs objets** (NOUVEAU)

2. **SaveGameUI.cs** : Interface utilisateur
   - Intégré avec UnifiedUIManager
   - Boîtes de dialogue de confirmation
   - Notifications

3. **SaveMenuIntegration.cs** : Intégration menu pause
   - Utilise UnifiedUIManager.NavigateTo()
   - Gère la navigation entre menus

4. **ProgressiveSaveSlots.cs** : Gestion des slots
   - Mode progressif : affiche seulement les slots utilisés + 1
   - Mode complet : affiche tous les slots
   - Mise à jour automatique de l'affichage

## Installation

### 1. SaveGameManager

```
GameObject vide → "SaveGameManager"
Ajouter composant → SaveGameManager
```

**Configuration :**
- Save File Name : "savegame" (nom par défaut)
- Debug Mode : ✓ (pour voir les logs)

### 2. Interface utilisateur

Sur l'objet avec SaveGameUI :
- Ajouter `SaveGameUI`
- Ajouter `ProgressiveSaveSlots`

**Structure UI requise :**
```
SaveMenuPanel
├── Title (Text: "Save/Load Game")
├── SaveSlotContainer
│   ├── SaveSlot_0
│   ├── SaveSlot_1
│   ├── SaveSlot_2
│   ├── SaveSlot_3
│   └── SaveSlot_4
├── CloseButton
├── ConfirmDialog
└── NotificationPanel
```

### 3. Intégration avec UnifiedUIManager

Le SaveMenuPanel doit être assigné dans UnifiedUIManager !

## Utilisation

### Navigation
1. **ESC** → Menu pause
2. **Save/Load** → Menu de sauvegarde (via UnifiedUIManager)
3. **Save** → Sauvegarde et retour au jeu
4. **Load** → Chargement et retour au jeu (met à jour la "dernière sauvegarde utilisée")
5. **Delete** → Suppression avec confirmation
6. **Close/ESC** → Retour au menu pause

### Comportement du bouton "Continuer" (MainMenu)
- Charge la **dernière sauvegarde utilisée** (sauvegardée ou chargée)
- Si aucune "dernière sauvegarde utilisée", charge la plus récente par date
- La "dernière sauvegarde utilisée" est conservée dans PlayerPrefs
- Nouvelle partie ne réinitialise PAS cette valeur (sécurité si le joueur quitte sans sauvegarder)

### Comportement des slots
- **Mode progressif** (par défaut) :
  - Début : 1 slot vide
  - Après sauvegarde : slots utilisés + 1 vide
  - Maximum : 10 slots

- **Noms des sauvegardes** :
  - Slot vide : "Empty"
  - Slot utilisé : Date et heure de la sauvegarde

## Ce qui est sauvegardé

### Joueur
- Position et rotation exactes
- Stamina actuelle
- Santé (si implémentée)
- Zoom de la caméra (orthographicSize ou fieldOfView)
- Position de spawn pour le respawn

### Companion
- Présence du companion
- Type de companion
- Position et rotation

### Quêtes (SYSTÈME AMÉLIORÉ)
- **Quêtes actives** :
  - Toutes les infos de la quête (id, titre, description, type, etc.)
  - Progression actuelle
  - **Nom exact de la zone** (ex: "Zone_Ruins_Temple_3")
  - **Noms des objets/cibles** (objectName, targetName)
  - **Positions des objets spawnés** (NOUVEAU)
  - Quête actuellement suivie (tracked)
  
- **Quêtes complétées** : Infos complètes sauvegardées
- **Quêtes annulées** : Infos complètes sauvegardées (NOUVEAU)

- **Au chargement** :
  - Les quêtes sont recréées dans QuestManager
  - Les objets de quête sont respawnés aux positions sauvegardées
  - La progression est restaurée
  - Le suivi (tracking) est restauré

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

## Format et emplacement

**Format** : JSON lisible

**Emplacement des fichiers** :
- Windows : `%APPDATA%/../LocalLow/[CompanyName]/[GameName]/saves/`
- Mac : `~/Library/Application Support/[CompanyName]/[GameName]/saves/`
- Linux : `~/.config/unity3d/[CompanyName]/[GameName]/saves/`

**Noms des fichiers** : `save_0.json`, `save_1.json`, etc.

## Debug des quêtes

Si les quêtes ne se rechargent pas correctement :
1. Vérifiez les logs pour "[SaveGame]"
2. Assurez-vous que les zones de quête ont les bons "supportedObjects"
3. Vérifiez que le nom de zone est exact (pas juste "ruins" mais "Zone_Ruins_Temple_3")

## Personnalisation

### Changer le mode d'affichage des slots

Dans `ProgressiveSaveSlots` :
- `Show All Slots` : ❌ = progressif, ✓ = tous les slots
- `Max Slots` : nombre maximum de sauvegardes

### Ajouter des données à sauvegarder

1. Modifier la classe de données dans SaveGameManager :
```csharp
[System.Serializable]
public class MyCustomData
{
    public int score;
    public float playTime;
}
```

2. L'ajouter à SaveData
3. Implémenter dans CollectSaveData()
4. Implémenter dans ApplySaveData()

## Dépannage

### Les boutons ne fonctionnent pas
→ Vérifier que ProgressiveSaveSlots est actif

### Le menu ne s'ouvre pas
→ Vérifier SaveMenuPanel dans UnifiedUIManager ET SaveGameUI

### Les slots ne se mettent pas à jour
→ Clic droit sur ProgressiveSaveSlots → "Force Update"

### Les quêtes ne se rechargent pas
→ Vérifier les logs "[SaveGame]"
→ Vérifier que les zones supportent les bons types d'objets
→ S'assurer que QuestManager et QuestJournal sont DontDestroyOnLoad

### Position du joueur ne se restaure pas
→ Vérifier que CharacterController est bien désactivé/réactivé

### Le bouton Continuer charge la mauvaise sauvegarde
→ Le système utilise maintenant PlayerPrefs("LastLoadedSave")
→ Cette valeur est mise à jour à chaque Save/Load
→ Si elle n'existe pas, le système utilise la sauvegarde la plus récente par date

## Notes importantes

- **Pas de quicksave F5/F9** (supprimé pour simplifier)
- **Pas d'autosave** (supprimé)
- **Save/Load retourne directement au jeu**
- Le système utilise les événements pour se mettre à jour automatiquement
- **IMPORTANT** : Les noms de zones doivent être exacts pour les quêtes

## Performance

- Sauvegarde : < 100ms
- Chargement : < 200ms (+ temps de recréation des quêtes)
- Taille moyenne : 10-50 KB par sauvegarde
- Mise à jour UI : instantanée avec plusieurs passes

## Bugs connus et solutions

### LoadingScreen reste bloqué après chargement depuis MainMenu
**Symptôme** : Après avoir cliqué sur LOAD dans le MainMenu, l'écran de chargement reste affiché et le jeu semble gelé.

**Cause** : Le LoadingScreen du MainMenu n'existe plus dans la scène Game, et le Time.timeScale pouvait rester à 0.

**Solution** : Le GameLoadingManager restaure maintenant automatiquement :
- Time.timeScale = 1
- Contrôles du joueur activés
- Curseur en mode jeu
- Fermeture des panneaux UI bloquants

## Changelog

### v2.3 (Version actuelle - Juillet 2025)
- **NOUVEAU** : Le bouton "Continuer" se souvient maintenant de la dernière sauvegarde utilisée
- **FIX** : Continuer charge maintenant la dernière sauvegarde chargée/sauvegardée, pas forcément la plus récente
- **AMÉLIORATION** : Nouvelle partie ne réinitialise plus la "dernière sauvegarde utilisée"
- **AJOUT** : Méthodes `GetLastUsedSave()` et `ClearLastUsedSave()` dans SaveGameManager

### v2.2 (Juillet 2025)
- **FIX** : Correction du bug du LoadingScreen bloqué lors du chargement depuis MainMenu
- **AMÉLIORATION** : GameLoadingManager gère maintenant complètement l'état du jeu après chargement
- **REFACTORING** : Nettoyage des logs de debug excessifs
- **REFACTORING** : Code simplifié et plus robuste

### v2.1 (Décembre 2024)
- **NOUVEAU** : Sauvegarde complète des quêtes annulées
- **NOUVEAU** : Sauvegarde des positions exactes des objets de quête
- **NOUVEAU** : Stockage du nom exact des zones (fix du bug "ruins")
- **FIX** : Recréation correcte des quêtes au chargement
- **FIX** : Restauration de la progression des quêtes
- Amélioration des logs de debug

### v2.0 
- Ajout sauvegarde du zoom caméra
- Système de slots progressif
- Suppression autosave et quicksave
- Interface épurée
- Architecture simplifiée (4 scripts au lieu de 10+)

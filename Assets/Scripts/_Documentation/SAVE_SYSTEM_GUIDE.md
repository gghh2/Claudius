# 🎮 Système de Sauvegarde - Documentation

## Vue d'ensemble

Le système de sauvegarde permet de sauvegarder et charger l'état complet du jeu incluant :
- Position et état du joueur
- Stamina et santé
- **Zoom de la caméra** (nouveau)
- Companion (position, type)
- Quêtes actives et complétées
- Positions des NPCs
- Inventaire
- Paramètres audio

## Architecture

### Fichiers principaux (4 scripts seulement)

1. **SaveGameManager.cs** : Gestionnaire principal
   - Gère la sauvegarde/chargement des données
   - Sérialise en JSON
   - Pas d'autosave (supprimé)

2. **SaveGameUI.cs** : Interface utilisateur
   - Gère l'affichage du menu de sauvegarde
   - Boîtes de dialogue de confirmation
   - Notifications

3. **SaveMenuIntegration.cs** : Intégration menu pause
   - Ajoute le bouton Save/Load au menu pause
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

### 3. Intégration menu pause

Sur l'objet avec ModernPauseMenu :
- Ajouter `SaveMenuIntegration`
- Assigner le bouton Save/Load

## Utilisation

### Navigation
1. **ESC** → Menu pause
2. **Save/Load** → Menu de sauvegarde
3. **Save** → Sauvegarde et retour au jeu
4. **Load** → Chargement et retour au jeu
5. **Delete** → Suppression avec confirmation
6. **Close** → Retour au menu pause

### Comportement des slots
- **Mode progressif** (par défaut) :
  - Début : 1 slot vide
  - Après sauvegarde : slots utilisés + 1 vide
  - Maximum : 10 slots

- **Noms des sauvegardes** :
  - Slot vide : "Empty"
  - Slot utilisé : "Claudius-1", "Claudius-2", etc.

- **Boutons dynamiques** :
  - Save : toujours visible
  - Load/Delete : visibles seulement si sauvegarde existe

## Ce qui est sauvegardé

### Joueur
- Position et rotation
- Stamina actuelle
- Santé (si implémentée)
- **Zoom de la caméra** (orthographicSize)

### Companion
- Présence du companion
- Type de companion
- Position et rotation

### Quêtes
- Toutes les quêtes actives avec progression
- Quête actuellement suivie
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

## Format et emplacement

**Format** : JSON lisible

**Emplacement des fichiers** :
- Windows : `%APPDATA%/../LocalLow/[CompanyName]/[GameName]/saves/`
- Mac : `~/Library/Application Support/[CompanyName]/[GameName]/saves/`
- Linux : `~/.config/unity3d/[CompanyName]/[GameName]/saves/`

**Noms des fichiers** : `save_0.json`, `save_1.json`, etc.

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
→ Vérifier SaveMenuPanel dans l'Inspector de SaveGameUI

### Les slots ne se mettent pas à jour
→ Clic droit sur ProgressiveSaveSlots → "Force Update"

### La suppression ne rafraîchit pas l'affichage
→ Le système fait plusieurs mises à jour automatiques, attendez 0.5s

## Notes importantes

- **Pas de quicksave F5/F9** (supprimé pour simplifier)
- **Pas d'autosave** (supprimé)
- **Save/Load retourne directement au jeu** (pas au menu pause)
- Le système utilise les événements pour se mettre à jour automatiquement

## Performance

- Sauvegarde : < 100ms
- Chargement : < 200ms
- Taille moyenne : 10-50 KB par sauvegarde
- Mise à jour UI : instantanée avec plusieurs passes

## Changelog

### v2.0 (Version actuelle)
- Ajout sauvegarde du zoom caméra
- Système de slots progressif
- Suppression autosave et quicksave
- Interface épurée
- Architecture simplifiée (4 scripts au lieu de 10+)

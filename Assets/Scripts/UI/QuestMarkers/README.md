# Système de Marqueurs de Quête

## Vue d'ensemble

Le système affiche des marqueurs visuels sur les bords de l'écran qui pointent vers les objectifs de quête actifs.

## Installation

1. Créez un GameObject vide dans votre scène
2. Ajoutez le composant `QuestMarkerSystem`
3. Configurez les paramètres dans l'inspecteur

## Configuration

### QuestMarkerSystem
- **Hide Distance** (10m) : Distance en dessous de laquelle les marqueurs disparaissent
- **Edge Offset** (50px) : Marge depuis le bord de l'écran
- **Marker Size** (50px) : Taille des marqueurs
- **Marker Color** : Couleur des indicateurs
- **Show Distance** : Affiche la distance en mètres
- **Enable Pulse** : Active l'animation de pulsation

### QuestZone
- **Zone Name** : Nom de la zone
- **Zone Type** : Type de zone (Laboratory, Hangar, etc.)
- **Supported Objects** : Types d'objets qui peuvent spawner ici
- **Spawn Radius** : Rayon de spawn des objets
- **Obstacle Layer** : Layer des objets qui bloquent le spawn (optionnel)

## Utilisation

### Prérequis
- Le joueur doit avoir le tag "Player"
- QuestManager doit être actif
- Les zones doivent avoir le composant QuestZone

### Types de marqueurs
- **FETCH** : Pointe vers les objets à collecter
- **DELIVERY** : Pointe vers le NPC destinataire
- **EXPLORE** : Pointe vers la zone à explorer
- **TALK** : Pointe vers le NPC à qui parler
- **RETOUR** : Pointe vers le NPC donneur quand la quête est complétée

## API

```csharp
// Rafraîchir les marqueurs
QuestMarkerSystem.Instance.RefreshMarkers();

// Activer/Désactiver
QuestMarkerSystem.Instance.SetMarkersVisible(bool visible);
```

## Fichiers

- `QuestMarkerSystem.cs` : Système principal de marqueurs
- `QuestZone.cs` : Zones où les objets de quête peuvent spawner
- `QuestMarkerDebugger.cs` : Outil de debug (optionnel, F9 en jeu)

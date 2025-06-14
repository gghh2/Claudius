# Système de Marqueurs de Quête

## Vue d'ensemble

Le système de marqueurs de quête affiche des indicateurs visuels sur les bords de l'écran qui pointent vers les objectifs de quête actifs. Il utilise une approche directe en pointant vers les objets de quête plutôt que vers les zones.

## Installation

1. Créez un GameObject vide dans votre scène
2. Ajoutez le composant `QuestMarkerSystem`
3. Configurez les paramètres dans l'inspecteur

## Configuration

### Paramètres dans l'inspecteur

- **Hide Distance** (10m) : Distance en dessous de laquelle les marqueurs disparaissent
- **Edge Offset** (50px) : Marge depuis le bord de l'écran
- **Marker Size** (50px) : Taille des marqueurs
- **Marker Color** (Jaune) : Couleur des indicateurs
- **Show Distance** : Affiche la distance en mètres
- **Enable Pulse** : Active l'animation de pulsation
- **Pulse Speed** (2) : Vitesse de l'animation
- **Pulse Amount** (0.1) : Intensité de la pulsation

## Fonctionnalités

### Types de marqueurs

1. **Objectifs de quête** : Pointe vers les objets à collecter, NPCs à rencontrer, zones à explorer
2. **Retour de quête** : Quand tous les objectifs sont complétés, pointe vers le NPC donneur

### Comportement

- Les marqueurs apparaissent automatiquement pour les quêtes actives
- Ils disparaissent quand le joueur est proche de l'objectif
- Ils restent toujours sur les bords de l'écran
- Ils gèrent correctement les objets derrière la caméra

## API Publique

```csharp
// Obtenir l'instance
QuestMarkerSystem.Instance

// Forcer le rafraîchissement des marqueurs
QuestMarkerSystem.Instance.RefreshMarkers();

// Activer/Désactiver les marqueurs
QuestMarkerSystem.Instance.SetMarkersVisible(bool visible);
```

## Prérequis

- Le joueur doit avoir le tag "Player"
- QuestManager doit être actif dans la scène
- Les quêtes doivent utiliser le système ActiveQuest/QuestObject

## Debug

Pour débugger le système :

1. Ajoutez `QuestMarkerDebugger` sur un GameObject
2. Appuyez sur F9 en jeu pour un debug complet
3. Le debug affiche :
   - Les quêtes actives
   - Les objets de quête
   - Les distances
   - L'état du système

## Architecture

Le système utilise une architecture modulaire :

- **QuestMarkerSystem** : Composant principal
- **MarkerTarget** : Structure de données pour les cibles
- **QuestMarker** : Structure pour gérer l'UI des marqueurs

## Performance

- Utilise un pool de marqueurs réutilisables
- Met à jour uniquement les marqueurs visibles
- Nettoie automatiquement les marqueurs obsolètes

## Nettoyage des anciens fichiers

Pour nettoyer les anciens fichiers :

1. Dans Unity : `Tools > Quest System > Clean Quest Marker Files`
2. Vérifiez la liste des fichiers à supprimer
3. Cliquez sur "Supprimer tous les fichiers obsolètes"

## Support

En cas de problème :

1. Vérifiez que le joueur a le tag "Player"
2. Vérifiez que des quêtes sont actives
3. Utilisez QuestMarkerDebugger pour diagnostiquer
4. Vérifiez la console pour les erreurs

## Changelog

### v2.0 (Version actuelle)
- Refonte complète du système
- Pointe directement vers les objets de quête
- Code optimisé et factorisé
- Suppression des dépendances inutiles

### v1.0 (Obsolète)
- Version initiale avec plusieurs systèmes
- Recherche par zones
- Configuration via ScriptableObject

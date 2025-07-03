# Système de Quêtes - Documentation

## Vue d'ensemble

Le système de quêtes est un système modulaire et extensible permettant de créer et gérer différents types de quêtes dans le jeu. Il supporte le suivi automatique, la gestion des objectifs, et l'intégration avec l'IA pour la génération dynamique de quêtes.

## Architecture

### Fichiers principaux

- **QuestManager.cs** : Gestionnaire principal des quêtes actives
- **QuestJournal.cs** : Journal des quêtes avec système de suivi automatique
- **QuestObject.cs** : Composant pour les objets interactifs de quête
- **QuestMarkerSystem.cs** : Système de marqueurs UI directionnels
- **QuestSystemConfig.cs** : Configuration centralisée (constantes, couleurs, paramètres)
- **QuestManagerHelper.cs** : Méthodes utilitaires pour la factorisation

### Classes de support

- **QuestZone.cs** : Zones où les objets de quête peuvent apparaître
- **QuestZoneManager.cs** : Gestionnaire des zones de quête
- **QuestToken.cs** : Structure de données pour définir une quête (dans AI/Core)
- **NPCQuestTurnIn.cs** : Gestion du rendu des quêtes auprès des NPCs

## Types de quêtes supportés

1. **FETCH** : Collecter des objets
2. **DELIVERY** : Livrer un objet à un NPC
3. **EXPLORE** : Explorer une zone spécifique
4. **TALK** : Parler à un NPC
5. **INTERACT** : Interagir avec un objet/terminal
6. **ESCORT** : Escorter un NPC (en développement)

## Fonctionnalités clés

### Suivi automatique des quêtes

- **Nouvelle quête** : Automatiquement suivie (remplace la quête actuellement suivie)
- **Quête terminée** : La première quête active devient automatiquement suivie
- **Système intelligent** : Gère automatiquement les marqueurs UI

### Système de marqueurs

- Indique la direction des objectifs de quête
- Cache automatiquement les marqueurs proches
- Animation de pulsation pour l'attention
- Support des caméras orthographiques et perspectives

### Intégration IA

- Les NPCs peuvent générer des quêtes dynamiquement
- Support des prompts personnalisés par type de NPC
- Validation automatique des quêtes générées

## Configuration

### QuestSystemConfig.cs

Toutes les constantes sont centralisées :

```csharp
// Couleurs UI
TrackedQuestBackgroundColor = Jaune semi-transparent
TrackedButtonColor = Jaune
UntrackedButtonColor = Gris

// Paramètres des marqueurs
DefaultMarkerSize = 50f
MarkerHideDistance = 10f
MarkerPulseSpeed = 2f

// Sons (volumes par défaut)
DefaultQuestStartVolume = 0.5f
DefaultQuestCompleteVolume = 0.5f
```

## Utilisation

### Créer une nouvelle quête

```csharp
// Créer un token de quête
QuestToken token = new QuestToken
{
    questId = System.Guid.NewGuid().ToString(),
    questType = QuestType.FETCH,
    description = "Trouvez 3 cristaux dans la zone minière",
    objectName = "cristal_energie",
    quantity = 3,
    zoneName = "zone_miniere",
    zoneType = QuestZoneType.ResourceArea
};

// Créer la quête
QuestManager.Instance.CreateQuestFromToken(token, "Marchand_01");
```

### Vérifier le statut d'une quête

```csharp
// Obtenir la quête suivie
JournalQuest trackedQuest = QuestJournal.Instance.GetTrackedQuest();

// Vérifier si une quête est active
bool hasActiveQuest = QuestJournal.Instance.HasActiveQuestWithNPC("Marchand_01");

// Obtenir toutes les quêtes actives
List<JournalQuest> activeQuests = QuestJournal.Instance.GetActiveQuests();
```

## Workflow d'une quête

1. **Création** : NPC génère un QuestToken via l'IA
2. **Validation** : Le système valide les paramètres
3. **Spawn** : Les objets/NPCs sont créés dans les zones appropriées
4. **Suivi** : Le joueur suit la quête via les marqueurs UI
5. **Progression** : Les actions du joueur mettent à jour la progression
6. **Complétion** : Retour au NPC donneur pour valider
7. **Nettoyage** : Les ressources sont libérées

## Optimisations appliquées

### Factorisation du code

- Méthodes helper pour éviter la duplication
- Configuration centralisée
- Extensions pour simplifier les logs debug

### Performance

- Pooling des marqueurs UI
- Mise à jour conditionnelle des marqueurs
- Nettoyage automatique des ressources

### Maintenabilité

- Séparation des responsabilités
- Code auto-documenté
- Constantes nommées explicitement

## Debug et troubleshooting

### Commandes de debug

- `[Context Menu] Debug AI Fields` : Vérifie les prefabs assignés
- `[Context Menu] Show All Quests` : Affiche toutes les quêtes du journal
- `QuestDebugger.cs` : Outil de debug visuel (si activé)

### Problèmes courants

1. **Marqueurs UI non visibles**
   - Vérifier que QuestMarkerSystem est dans la scène
   - Vérifier que la quête est bien suivie (tracked)
   - Vérifier la distance du marqueur (MarkerHideDistance)

2. **Objets de quête non interactifs**
   - Vérifier le tag "Player" sur le joueur
   - Vérifier les colliders et triggers
   - Vérifier le rayon d'interaction

3. **Quêtes non créées**
   - Vérifier QuestJournal.Instance existe
   - Vérifier les zones supportent le type d'objet
   - Vérifier les prefabs sont assignés

## Extensions futures

- Système de récompenses intégré
- Quêtes à embranchements multiples
- Conditions de déverrouillage
- Sauvegarde/chargement persistant
- Quêtes chronométrées
- Objectifs optionnels

## Notes pour les développeurs

- Toujours utiliser `TextFormatter.FormatName()` pour l'affichage
- Ne pas formater les noms dans les données (garder avec underscores)
- Utiliser les helpers de QuestManagerHelper pour la factorisation
- Respecter les conventions de nommage des constantes dans QuestSystemConfig

# Refactorisation du Système de Dialogue IA - Documentation

## Vue d'ensemble des changements

Le système de dialogue a été complètement refactorisé pour utiliser **uniquement l'IA** (OpenAI) pour tous les dialogues avec les NPCs. Tous les dialogues pré-écrits ont été supprimés.

## Changements principaux

### 1. DialogueUI.cs
- **Supprimé** :
  - Le bouton "Continuer" 
  - Le bouton "Passer en mode IA"
  - Le système de `dialogueStep`
  - Les méthodes `GetWelcomeMessage()` et `GetFollowUpDialogue()`
  - Le mode classique vs mode IA
  - La touche M pour demander du travail

- **Modifié** :
  - `StartDialogue()` lance toujours une conversation IA
  - L'input field est visible dès le début
  - Gestion d'erreur si l'API n'est pas configurée

- **Conservé** :
  - Système de quêtes (accepter/refuser)
  - Système de livraison
  - Historique des conversations
  - Raccourcis A/R pour accepter/refuser les quêtes

### 2. NPC.cs
- **Simplifié** :
  - `StartDialogue()` appelle directement l'IA
  - Suppression de toute référence au mode classique
  - Conservation des cas spéciaux (delivery, fetch quest)

### 3. AIDialogueManager.cs
- **Supprimé** :
  - Toutes les méthodes de fallback (`UseFallback`, `GetFallbackWelcome`, etc.)
  - Le système de fallback en cas d'erreur API

- **Modifié** :
  - Affichage d'erreurs claires en cas de problème API
  - Messages d'erreur critiques si la clé API n'est pas configurée

## Configuration requise

### OBLIGATOIRE : Clé API OpenAI
Le système **ne fonctionnera pas** sans une clé API OpenAI valide configurée dans :
```
Assets/Scripts/Config/APIConfig.cs
```

Format :
```csharp
public static class APIConfig
{
    public const string OPENAI_API_KEY = "sk-VOTRE_CLE_ICI";
}
```

### Dans Unity Inspector

#### Sur l'objet avec DialogueUI :
- **dialoguePanel** : Panel principal du dialogue
- **npcNameText** : Texte pour le nom du NPC
- **dialogueText** : Texte principal du dialogue
- **closeButton** : Bouton fermer
- **loadingIndicator** : Indicateur de chargement
- **playerInputField** : Champ de saisie du joueur
- **sendButton** : Bouton envoyer
- **historyButton** : Bouton historique
- **acceptQuestButton** : Bouton accepter quête
- **declineQuestButton** : Bouton refuser quête
- **deliverButton** : Bouton livrer/remettre

#### Sur chaque NPC :
- **npcName** : Nom du personnage
- **npcRole** : Rôle (Marchand, Scientifique, Garde Impérial, etc.)
- **npcDescription** : Description détaillée pour l'IA
- **interactionRange** : Distance d'interaction
- **npcColor** : Couleur du personnage

## Flux d'interaction

1. **Joueur approche** → Affichage "[E] Parler"
2. **Joueur appuie sur E** → Vérification API
3. **Si API OK** → L'IA génère un message de bienvenue contextuel
4. **Si API KO** → Message d'erreur "ERREUR API"
5. **Conversation** → Le joueur tape ses messages, l'IA répond
6. **Quêtes** → L'IA peut proposer des quêtes avec les tokens [QUEST:...]
7. **Fin** → ESC ou bouton fermer

## Comportements contextuels de l'IA

L'IA adapte automatiquement ses réponses selon le contexte :

### 1. Première rencontre
- Salutation naturelle selon la personnalité du NPC
- Présentation du rôle/métier

### 2. Quête active avec ce NPC
- L'IA demande des nouvelles de la quête
- Encourage selon la progression
- Ne propose PAS de nouvelle quête

### 3. Quête complétée (objets à remettre)
- L'IA félicite le joueur
- Propose de remettre les objets
- Affichage du bouton "Remettre les objets"

### 4. NPC destinataire de livraison
- Si le joueur a le colis : joie et remerciements
- Si pas de colis : rappel de l'attente

### 5. Après une quête terminée
- L'IA peut proposer une nouvelle quête
- Remerciements pour le travail accompli

## Gestion des erreurs

### Pas de clé API configurée
```
ERREUR SYSTÈME
ERREUR API : Le système de dialogue IA n'est pas configuré correctement.
Vérifiez que la clé API OpenAI est configurée dans APIConfig.cs
```

### Erreur de connexion API
```
ERREUR API: [détails de l'erreur]
Code: [code HTTP]
```

### Réponse vide ou erreur de parsing
```
ERREUR: [message d'erreur]
```

## Système de quêtes intégré

L'IA peut créer des quêtes en incluant des tokens spéciaux :

### Types de quêtes supportés
- `[QUEST:FETCH:objet:zone:quantité]` - Collecter des objets
- `[QUEST:DELIVERY:objet:destinataire:zone]` - Livrer un objet
- `[QUEST:EXPLORE:zone]` - Explorer une zone
- `[QUEST:TALK:personnage:zone]` - Parler à quelqu'un
- `[QUEST:INTERACT:objet:zone]` - Interagir avec un objet

### Exemple de dialogue avec quête
```
Joueur: "Avez-vous du travail pour moi ?"
Marchand: "Justement ! J'ai besoin que vous récupériez mes cristaux précieux [QUEST:FETCH:cristal_precieux:laboratory:3] dans le laboratoire."
```

Le token est automatiquement détecté et retiré du texte affiché. Les boutons Accepter/Refuser apparaissent.

## Raccourcis clavier

- **Enter** : Envoyer le message
- **ESC** : Fermer le dialogue (géré par UnifiedUIManager)
- **A** : Accepter une quête proposée
- **R** : Refuser une quête proposée

## Configuration des prompts IA

Les prompts sont configurés dans des ScriptableObjects :
- `DefaultPrompt.asset` : Prompt par défaut
- `MarchandPrompts.asset` : Pour les marchands
- `ScientifiquePrompts.asset` : Pour les scientifiques
- `GardePrompts.asset` : Pour les gardes

Chaque prompt contient :
- **npcPersonality** : Description de la personnalité
- **globalInstructions** : Instructions générales
- **roleSpecificExamples** : Exemples spécifiques au rôle

## Migration depuis l'ancien système

### Pour les développeurs

1. **Supprimer** tous les dialogues pré-écrits dans le code
2. **Configurer** la clé API OpenAI
3. **Vérifier** que tous les NPCs ont :
   - Un nom
   - Un rôle
   - Une description
4. **Tester** avec et sans connexion internet

### Pour les game designers

1. **Enrichir** les descriptions des NPCs pour l'IA
2. **Définir** clairement les personnalités
3. **Configurer** les prompts pour chaque type de NPC
4. **Tester** les dialogues générés

## Limitations actuelles

1. **Dépendance internet** : Pas de dialogue sans connexion
2. **Latence** : Délai de 1-3 secondes pour chaque réponse
3. **Coût** : Chaque message consomme des tokens OpenAI
4. **Pas de fallback** : Si l'API échoue, pas de dialogue possible

## Optimisations possibles

1. **Cache local** : Sauvegarder les réponses fréquentes
2. **Pré-génération** : Générer des dialogues à l'avance
3. **Modèle local** : Utiliser un LLM local pour le fallback
4. **Compression** : Optimiser la taille des prompts

## Debug et logs

### Logs importants
- `✅ Clé API OpenAI chargée` : API configurée
- `❌ ERREUR CRITIQUE : Clé API OpenAI non configurée !` : Pas de clé
- `🤖 Réponse IA brute:` : Réponse complète de l'IA
- `🎯 X quête(s) détectée(s)` : Détection de tokens de quête

### Context Menu
Sur AIDialogueManager :
- **Reload API Key** : Recharger la clé
- **Show API Status** : Afficher le statut

Sur les NPCs :
- **Debug NPC Info** : Afficher les infos du NPC

## Notes importantes

- **Pas de dialogues sans API** : Le jeu nécessite une clé OpenAI valide
- **Personnalisation** : Chaque NPC doit avoir une description riche
- **Contexte** : L'IA tient compte du contexte (quêtes, historique)
- **Cohérence** : L'IA maintient la cohérence grâce à l'historique

## Support et dépannage

En cas de problème :
1. Vérifier la clé API dans `APIConfig.cs`
2. Vérifier la connexion internet
3. Consulter les logs Unity
4. Vérifier les descriptions des NPCs
5. Tester avec le Context Menu "Show API Status"

## Changements dans le code

### Avant (mode mixte)
```csharp
// DialogueUI.cs
if (isAIMode) {
    // Mode IA
} else {
    // Mode classique avec dialogues pré-écrits
    dialogueStep++;
    ShowText(GetFollowUpDialogue(npc, dialogueStep));
}
```

### Après (100% IA)
```csharp
// DialogueUI.cs
public void StartDialogue(NPCData npcData)
{
    // Vérifie que l'IA est configurée
    if (AIDialogueManager.Instance == null || !AIDialogueManager.Instance.IsConfigured())
    {
        ShowAPIError();
        return;
    }
    
    // Lance directement la conversation IA
    AIDialogueManager.Instance.StartAIConversation(npcData);
}
```

## Impact sur le gameplay

1. **Plus de variété** : Chaque conversation est unique
2. **Contexte dynamique** : L'IA s'adapte à la situation
3. **Personnalités riches** : Chaque NPC a sa propre façon de parler
4. **Quêtes naturelles** : Les quêtes sont intégrées dans la conversation

## Prochaines étapes recommandées

1. **Enrichir les descriptions des NPCs** pour des personnalités plus marquées
2. **Créer des prompts spécialisés** pour des types de NPCs spécifiques
3. **Ajouter un système de cache** pour les réponses fréquentes
4. **Implémenter un fallback local** pour le mode hors-ligne
5. **Optimiser les prompts** pour réduire les coûts API
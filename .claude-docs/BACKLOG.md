# BACKLOG — Claudius

> Travail à faire, priorisé. Sources : `Assets/TOTOLIST.txt` (todo de l'auteur)
> + audit code mort du 2026-05-20.
>
> Légende : 🔴 important · 🟡 moyen · 🟢 polish · 🔧 dette technique

## Bugs

- 🔴 La zone de fall-back n'a pas de vrai nom de zone (affiche « Laboratory »).
- 🟡 `F5` et `F9` ne font rien (raccourcis save/load rapide attendus ?).
- 🟡 Build&Run : le compagnon est très lent.

## Quêtes

- 🔴 L'IA ne génère pas toujours de token `[QUEST:...]` quand le PNJ propose
  pourtant une mission (3 cas observés). Elle a aussi émis `[QUEST:PROTECT:ruines]`
  — type non supporté. → fiabiliser le prompt et/ou le parsing.
- 🔴 Empêcher la création d'un **doublon de PNJ**.
- 🔴 DELIVERY : demander une quête crée un doublon du PNJ donneur ailleurs.
- 🟡 DELIVERY : le PNJ de destination doit obligatoirement avoir un nom inventé.
- 🟡 EXPLORE : après une exploration demandée par un PNJ, fournir une explication /
  des « données » d'exploration au retour.
- 🟡 EXPLORE : zones à explorer de longueurs variables.
- 🟡 TALK : chaîne de quête — un PNJ A envoie voir B ; trouver B doit déclencher
  une mission de B (« TODO à rallonge »).
- 🟡 Écran des quêtes : redesign en cours (chevauchement des entrées).
- 🟡 Quand il y a beaucoup de quêtes, le scroll masque une partie de la liste.
- 🟡 Notification écran (toast) à : nouvelle quête, quête terminée, nouvelle
  entrée de journal. Le `NotificationManager` existe déjà — à câbler.

## Système de sauvegarde

- 🟡 L'historique de conversation avec les PNJ n'est pas sauvegardé.
- 🟡 Au reload : les items de quête déjà ramassés ne doivent pas être recréés
  dans le monde.
- 🟡 Quête EXPLORE déjà explorée mais non rendue : comportement au reload à définir.

## Dette technique — code mort / non câblé (audit 2026-05-20)

### Décisions en attente
- **AdventureJournal** — feature complète mais non branchée : finir ou supprimer ?
- **DynamicAssets / CSM / Meshy** (~12 fichiers) — sous-système instancié en scène
  mais déconnecté du jeu (Phase 2 jamais faite) : reprendre ou geler proprement ?
- `MeshyGenerator` vs `CSMGenerator` — deux générateurs concurrents : lequel garder ?
- Garder `QuestMarkerDebugger` / `QuestDebugger` comme outils de debug, ou retirer ?

### 🔧 À nettoyer
- Consolider la gestion du curseur (3 contrôleurs : `SmartCursorManager`,
  `UnifiedUIManager`, `PauseMenuUI`) derrière `UnifiedUIManager`.
- Retirer les blocs de code commentés : auto-save (`SaveGameManager`),
  quick-save (`SaveMenuIntegration`), validation (`QuestTokenDetector`).
- `PauseMenuUI.Pause()` marquée *deprecated* — vérifier le câblage Inspector puis retirer.
- `UnifiedUIManager.OpenPanel` / `ClosePanel` — passe-plats *legacy*, à fusionner.
- Nettoyer les `Debug.Log` restants : CSM*, DynamicAsset*, *Tester, NPCQuestTurnIn,
  QuestJournal, PlayerController, StaminaUI, CompanionSpeedSync, DialogueUI,
  AIDialogueManager.
- Passe « bonnes pratiques Unity » : auditer le code à l'aune des e-books de
  `UnityBestPractice/` (C# style guide, architecture ScriptableObjects).

### 🔧 Code mort — supprimables (vérifier en scène avant ; dumps datés 2025-07)
- `NotificationTester`, `InventoryDebugger` — scaffolding de debug, non attachés.
- `QuestMarkerCustomizer` — non câblé (rend mortes 2 méthodes de `QuestMarkerSystem`).
- `AudioSettingsUI` — remplacé par `PauseMenuUI` (constante `AudioSettings` inutilisée).
- `MusicZoneTrigger` — jamais placé en scène (ou WIP à assumer ?).
- `APITester` — attaché à la scène `Game` mais inutilisé → retirer le composant.

## Évolutions

- 🟡 **LLM embarqué** — option Cloud (OpenAI) / Local dans les Options, pour
  jouer sans coût d'API. Spec détaillée : `.claude-docs/SPEC_LLM_local.md`.
  Prérequis : abstraction `IAIProvider` — ✅ **fait**. Reste : runtime local +
  toggle Options. Gain rapide possible : passer `gpt-3.5-turbo` → `gpt-4o-mini`.

## Polish / assets

- 🟢 Matériaux : wood FX + material ; shader pour l'eau.
- 🟢 Assigner les sons au compagnon Poule.

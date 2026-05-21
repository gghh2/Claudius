# CLAUDE.md — Claudius

Jeu d'aventure 3D (space-opera) sous Unity. Le joueur explore une planète alien, dialogue avec des PNJ pilotés par IA, et accomplit des
quêtes générées dynamiquement par analyse LLM.

> Ce fichier est le **point d'entrée** de la documentation projet. Il est chargé
> automatiquement à chaque session et **référence tous les autres documents**
> (voir « Système de documentation » ci-dessous).

## 📚 Système de documentation

`CLAUDE.md` est le hub. Les documents vivants sont dans `.claude-docs/` :

| Document | Question à laquelle il répond | Quand le lire / mettre à jour |
|---|---|---|
| `CLAUDE.md` *(ce fichier)* | Qu'est-ce que le projet, **en permanence** ? | Mettre à jour quand l'architecture ou une règle permanente change |
| `.claude-docs/STATE.md` | Où en est-on, **maintenant** ? | **Lire au début de chaque session** ; mettre à jour à la fin |
| `.claude-docs/BACKLOG.md` | Que reste-t-il à faire ? | Lire avant de planifier ; mettre à jour quand une tâche entre/sort |
| `.claude-docs/FEATURES.md` | Qu'est-ce qui est **déjà livré** ? | Consulter pour savoir ce qui existe ; ajouter chaque feature finie |
| `.claude-docs/journal/AAAA-MM-JJ.md` | Que s'est-il passé, session par session ? | Un fichier par session de travail |

`STATE.md` est importé automatiquement ci-dessous (toujours pertinent). Les autres
se lisent à la demande, selon le tableau.

@.claude-docs/STATE.md

## Stack technique

- **Unity 6000.2.10f1** (Unity 6.2) — migré depuis Unity 2021.3 en mai 2026
- **URP 17.3.0** comme pipeline de rendu (des ressources HDRP existent mais URP est actif)
- **Input** : ancien Input Manager (`Input.GetKeyDown`, `KeyCode`) — **pas** le nouveau Input System
- Caméra **orthographique** avec suivi du joueur + zoom
- Dialogue IA : **API OpenAI** (`gpt-4o-mini`) — abstraite derrière `IAIProvider`
  (un moteur local via Ollama est sélectionnable, voir `SPEC_LLM_local.md`)
- Génération d'assets 3D : **Meshy / CSM**
- Dépôt : `github.com/gghh2/Claudius`, branche `main`, commits directs (pas de PR)

## Lancer / tester

Projet Unity standard : ouvrir dans Unity 6000.2.10f1, charger `Assets/Scenes/MainMenu.unity`,
Play. Pas de tests automatisés — la validation se fait en **Play mode** dans l'éditeur.

## Architecture

### Scènes (`Assets/Scenes/`)
- **MainMenu.unity** — menu principal. Utilise `MainMenuUI` directement (**pas** de `UnifiedUIManager`).
- **Game.unity** — gameplay. Contient `UnifiedUIManager` (obligatoire).

### Pattern managers / singletons
La plupart des managers sont des singletons `Instance` + `DontDestroyOnLoad` :
`if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }`.

⚠️ `DontDestroyOnLoad` n'agit que sur des GameObjects **racine**. Dans les scènes, les
managers sont regroupés sous un parent `Manager` — les scripts concernés se détachent
donc eux-mêmes (`transform.SetParent(null)`) avant l'appel. À reproduire pour tout
nouveau manager persistant.

### Systèmes principaux
Documentation détaillée dans `Assets/Scripts/_Documentation/README_*.md`. Vue d'ensemble :

- **Quête** — `QuestManager`, `QuestJournal`, `QuestObject`, `QuestZone`. Types :
  FETCH / DELIVERY / EXPLORE / TALK / INTERACT / ESCORT. Les quêtes sont générées
  par l'IA via des tokens `[QUEST:TYPE:...]` parsés par `QuestTokenDetector`.
  → `README_QUEST_SYSTEM.md`, `README_MARKER_SYSTEM.md`
- **Sauvegarde** — `SaveGameManager` (JSON dans `persistentDataPath/saves/`),
  `GameLoadingManager` (chargement depuis le menu). → `README_SAVE_SYSTEM_GUIDE.md`
- **Dialogue IA** — `AIDialogueManager` (`Assets/Scripts/AI/`). 100 % IA, aucun
  dialogue pré-écrit. Personnalités via `AIPromptConfig` (ScriptableObject par
  type de PNJ). Backend abstrait derrière `IAIProvider` (OpenAI / Ollama local).
  **Architecture « B2 »** : deux appels IA séparés — un appel de *chat* (roleplay
  pur, ne produit jamais de quête) et un appel d'*analyse* qui décide seul si une
  quête `[QUEST:...]` doit être proposée. → `README_AI_DIALOGUE_REFACTORING.md`
- **Navigation UI** — `UnifiedUIManager` : pile de panels, gestion de la touche ESC,
  layers, contrôle de `Time.timeScale` et de l'input joueur. Chaque panel a son
  propre sous-Canvas. → `README_UI_SYSTEM_COMPLETE.md`
- **PNJ** — `NPC`, `NPCMovement`, `NPCNameDisplay`, `NPCQuestTurnIn`.
- **Joueur / caméra** — `PlayerControllerCC` (CharacterController : marche, sprint,
  saut, stamina), `CameraFollow` (orthographique, zoom 2-15).
- **Compagnon** — `CompanionController` (suit le joueur, wander). Presets dans
  `CompanionSetupHelper`. → `README_COMPANION_SYSTEM.md`
- **Assets dynamiques** — `DynamicAssetManager` + générateurs Meshy/CSM
  (`Assets/Scripts/DynamicAssets/`).
- **Audio** — `MusicManager`, `SoundEffectsManager`, `AudioDistanceManager`
  (volume selon le zoom caméra). → `README_AUDIO_SYSTEM.md`
- **Notifications** — `NotificationManager` (toasts). → `README_NOTIFICATIONS.md`
- **Debug** — `GlobalDebugManager` : flags par système. Usage :
  `GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest)`.

### Inspecter la hiérarchie d'une scène
Claude ne peut pas lire utilement les fichiers `.unity` (YAML opaque, références
par fileID/GUID). Pour lui fournir la structure d'une scène, lancer le composant
`HierarchyDebugger` dans l'éditeur : il exporte un arbre lisible dans
`Assets/Scripts/_Documentation/SceneHierarchy_<scène>_<date>.txt`.
**Toujours régénérer ce dump avant de s'y fier** — un dump ancien ne reflète plus
la scène réelle.

## Conventions

- **Noms d'objets** : `snake_case` dans les données (`crystal_energy`, `zone_ruins_temple`).
  Ne **jamais** formater dans les données — l'affichage passe par `TextFormatter.FormatName()`.
- **Logs** : préfixés par catégorie — `[QUEST]`, `[SaveGame]`, etc. — et conditionnés
  par `GlobalDebugManager`.
- **Config** : classes statiques (`QuestSystemConfig`, `AudioConstants`) ou
  ScriptableObjects (`AIPromptConfig`).
- **Commits** : préfixe en majuscules — `FIX`, `FEATURE_OK`, `CLEANUP`, `UPDATE`,
  `UPGRADED`. Message descriptif.

## Règles permanentes / pièges

- **API Unity 6** : utiliser `FindFirstObjectByType` /
  `FindObjectsByType(FindObjectsSortMode.None)` (pas `FindObjectOfType`) ;
  `Rigidbody.linearVelocity` / `linearDamping` / `angularDamping`
  (pas `velocity` / `drag` / `angularDrag`).
- **Clés API** : dans `Assets/Scripts/Config/APIConfig.cs` — fichier **gitignored**,
  jamais commité.
- **Fichiers étrangers** : `Assets/Scripts/RentalDashboard_Updated.js` et
  `server_streaming_update.js` appartiennent à un **autre projet** de l'utilisateur,
  déposés ici par erreur. **Ne pas les toucher, ne pas les committer**
  (ignorés localement via `.git/info/exclude`).
- **`.gitattributes`** : configuré pour Unity (merge YAML, marquage des binaires).
  Le driver `unityyamlmerge` est déclaré en config locale (`.git/config`) — à
  reconfigurer en cas de changement de version Unity ou de machine.
- **Gitignored** : `Assets/Scripts/unity-bridge/` (outil d'indexation),
  `Assets/Prefabs/Buildings/ORIGINAL/` (backup), `Assets/_Recovery/`.

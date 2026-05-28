# BACKLOG — Claudius

> Travail à faire, priorisé. Sources : `Assets/TOTOLIST.txt` (todo de l'auteur)
> + audit code mort du 2026-05-20.
>
> Légende : 🔴 important · 🟡 moyen · 🟢 polish · 🔧 dette technique

## Bugs

- 🔴 La zone de fall-back n'a pas de vrai nom de zone (affiche « Laboratory »).
- 🔴 **Securite contenu — interdire dialogue impliquant des enfants** :
  contrainte a poser dans le system prompt commun et la generation de
  PNJ procedural. Aucun NPC enfant, aucune evocation d'enfants en
  contexte sensible. A cabler dans `BuildSystemPrompt` et a verifier
  cote `ProceduralNPCSpawner` que les roles generes ne produisent pas
  d'enfants.
- ✅ *(2026-05-25)* `F5` / `F9` quick save/load fonctionnels en jeu.
- 🟡 Build&Run : le compagnon est très lent.
- ✅ *(2026-05-24)* Composant orphelin retiré de `NPC_Quest.prefab` (était
  instancié à chaque spawn de PNJ de quête → 10+ erreurs console au Play).
- 🟡 Script manquant sur `Assets/Resources/QuestMarkerConfig.asset`
  (à vérifier — l'audit ScriptableObject de cette session n'a rien remonté).
- ✅ *(2026-05-24)* `URPCameraObstacleHandler` : exclut le collider sur
  lequel le joueur est posé (raycast vertical descendant) du raycast
  caméra→joueur, pour ne pas le rendre transparent. Toggle
  `excludeGroundUnderPlayer` dans l'Inspector si besoin de désactiver.
- 🟡 Dialogue : re-parler à un PNJ affiche d'abord brièvement le **texte du
  dialogue précédent**, puis l'efface et le remplace par la nouvelle réponse.
  À étudier (`DialogueUI` / `AIDialogueManager` — sans doute un `ShowText` avec
  l'historique affiché avant l'arrivée de la réponse IA).
- ✅ *(2026-05-26)* 14 erreurs `DontDestroyOnLoad` au Play : le pattern
  `if (transform.parent != null) transform.SetParent(null);` est en place
  dans tous les managers concernés (MeshyGenerator, CSMGenerator,
  CSMModelImporter, DynamicAssetManager, InventoryManager,
  SoundEffectsManager, MusicManager, AudioDistanceManager,
  QuestTokenDetector, QuestJournal, QuestManager, QuestZoneManager,
  QuestMarkerSystem, UnifiedUIManager, GlobalDebugManager,
  AIDialogueManager). Plus d'erreurs au Play, confirmé en jeu.
- ✅ *(2026-05-24)* Double tag "Player" résolu — `space_man_model` est
  désormais `Untagged`, seul le root porte le tag "Player".
- ✅ *(2026-05-24)* Raccourcis I / J / L désormais *toggle* — re-presser
  la touche du panneau ouvert le referme (`UnifiedUIManager.Update`).
- 🟡 **Emojis dans les strings affichées** (toasts, titres de quête, etc.)
  rendent des carrés placeholder dans le jeu — la police TMP ne contient pas
  les glyphes. Soit : (a) purger les emojis des chaînes user-facing (laisser
  ceux des `Debug.Log` console), soit (b) ajouter une fallback font emoji à
  TMP, soit (c) remplacer par des sprites/icônes via TMP `<sprite=...>`.
  Les `Debug.Log` console ne sont pas concernés (rendu OS, pas TMP).

## Peuplement / Shop IA

- 🟡 **Trop de NPC ont un shop (30/31 observe 2026-05-28)**. Le prompt
  ShopCatalogGenerator.GenerateFor (mode non-marchand) liste des examples
  de qui peut vendre (druide herbes, archeologue trouvailles...) mais le
  modele genere quand meme pour la plupart. Pistes :
  - Renforcer les exemples NEGATIFS et durcir le critere ("uniquement si
    OBVIOUS, dans le doute VIDE").
  - Probabilite mecanique : pour les non-marchands, tirer un coin flip
    (~30% accept) AVANT l'appel IA — si fail, VIDE direct sans appel.
  - Variante hybride : seuls les roles dans une liste "peut vendre"
    (druide, alchimiste, archeologue, mecanicien, herboriste, ferrailleur,
    artisan...) sont consultes ; les autres -> VIDE direct.

## Quêtes

- ✅ *(2026-05-21 — refonte B2)* La génération de quête ne dépend plus du
  dialogue libre : un appel d'analyse séparé décide. Sur-proposition éradiquée,
  tokens absurdes rejetés par validation sémantique.
- ✅ *(2026-05-24)* Doublon de PNJ — désormais **réutilisé** comme cible
  de la quête (TALK / DELIVERY) au lieu d'être rejeté. PNJ existant
  reçoit un QuestObject temporaire, restitué à la complétion.
- 🟡 DELIVERY : le PNJ de destination doit avoir un nom propre inventé.
- ✅ *(2026-05-24)* EXPLORE : un rapport flavor est injecté dans le contexte
  IA du PNJ donneur au retour (`ExplorationReport` + `InjectContextForNPC`).
- ✅ *(2026-05-24)* EXPLORE : durée d'exploration variable (range 3-8s
  tirée aléatoirement à la création de la quête).
- 🟡 TALK : chaîne de quête A→B partiellement câblée. Quand TALK
  complète, un message system est injecté dans le contexte IA du PNJ cible
  ("le voyageur vient de la part de X, c'est l'occasion de proposer une
  mission") — la suite est gérée naturellement par le pipeline d'analyse
  IA. Reste à affiner les prompts pour exploiter au mieux.
- 🟡 **Memoire des transactions PNJ asymetrique (vu 2026-05-27)** : un
  PNJ donneur de quete FETCH (ex. Arcan demande des cristaux d'energie)
  ne se souvient PAS d'avoir recu la livraison au dialogue suivant — il
  parle au conditionnel (« si tu avais ces cristaux »). La conversation
  history existe, mais aucun fait n'est injecte au turn-in. Le mecanisme
  `AIDialogueManager.InjectContextForNPC` existe deja et est cable pour
  les retours EXPLORE — etendre a FETCH / DELIVERY / TALK / INTERACT au
  moment du `QuestJournal.CompleteQuest` : injecter un fait type
  « Le voyageur t'a remis {item} x{qty}, mission accomplie » dans le
  contexte du giver.
- ✅ *(2026-05-24)* Nouveau type **TREASURE** ajouté (token
  `[QUEST:TREASURE:nom]`, location aléatoire, récompense 200 crédits).
  Prompts IA mis à jour pour le proposer naturellement.
- ✅ *(2026-05-24)* **TREASURE — UX du déterrage** revue : marker en mode
  trésor (flag `isTreasure` sur `QuestObject`). Phase 1 : "Appuyer sur E
  pour creuser" affiché au-dessus du marker. Phase 2 : barre de progression
  ASCII en pourcentage, le marker se rétracte progressivement (scale
  vers sol). À 100 % : trésor déterré, toast + crédits. Sortir de la zone
  pendant le creusement reset le progrès et restaure l'échelle.
- 🟡 Écran des quêtes : redesign en cours (chevauchement des entrées).
- 🟡 Quand il y a beaucoup de quêtes, le scroll masque une partie de la liste.
- ✅ *(2026-05-25)* Notification écran (toast) à : entrée de zone, nouvelle
  mission, objectif accompli, trésor déterré, crédits gagnés. Câblé via
  `NotificationManager` aux bons hooks (quête, zone, wallet).

## Système de sauvegarde

- ✅ *(2026-05-24)* Mémoire de conversation des PNJ persistée dans le
  save/load (`ConversationsSaveData` ajoutée à `SaveData` ; serialise
  `conversationHistories` ET `conversationsByNpc` côté `AIDialogueManager`).
- ✅ *(2026-05-24)* Au reload : `SaveGameManager.ApplySaveData` détruit
  les items déjà ramassés (`isActive=false` au save) après la recréation
  full de la quête. À retester si bug reproduit.
- 🟡 Quête EXPLORE déjà explorée mais non rendue : comportement au reload à définir.

## Rendu / graphismes

> Issus de la session graphismes du 2026-05-22 (passage à un look type PoE).

- 🟡 Matériaux **magenta** : shaders cassés (non-URP / HDRP) → à reskinner en URP.
- ✅ *(2026-05-24)* `OrthographicFogAdapter` recalibré (défauts du code) :
  point1 (size 15, start 30, end 200) / point2 (size 2, start 10, end 60),
  aligné sur le vrai range de zoom 2-15. L'instance en scène garde
  toutefois ses anciennes valeurs sérialisées — `Reset` sur le composant
  via Inspector pour les réappliquer.
- 🟢 Lightmap baké unique en 1024² : basse résolution, AO/GI bakés grossiers.
  Monter la résolution / le nombre de lightmaps pour un baké net.
- 🔧 **Shader stripping** désactivé (`GraphicsSettings`) → Unity compile toutes
  les variantes. L'activer réduirait fortement le temps **et** la taille du build.

## Dette technique — code mort / non câblé (audit 2026-05-20)

### Décisions en attente
- ✅ *(2026-05-25)* **AdventureJournal** = journal d'aventure narratif IA (touche L,
  panel "AdventureJournal" dans UnifiedUIManager). Branché et actif — pas du code mort.
- **DynamicAssets / CSM / Meshy** (~12 fichiers) — sous-système instancié en scène
  mais déconnecté du jeu (Phase 2 jamais faite) : reprendre ou geler proprement ?
- `MeshyGenerator` vs `CSMGenerator` — deux générateurs concurrents : lequel garder ?
- Garder `QuestMarkerDebugger` / `QuestDebugger` comme outils de debug, ou retirer ?

### 🔧 À nettoyer
- Package `com.unity.ai.toolkit` : appelle en boucle une API beta Unity retirée
  (`generators-beta.ai.unity.com` → `ApiNoLongerSupported`) → **pollue le log
  éditeur** même au repos. Le projet ne s'en sert pas → retirer ce package (et
  vérifier les autres `com.unity.ai.*` inutilisés).
- ✅ *(2026-05-24)* Curseur consolidé derrière `SmartCursorManager`
  (autorité unique, déjà en place). Restaient des `Cursor.visible`/
  `Cursor.lockState` parasites dans `GameLoadingManager` et `MainMenuUI` —
  retirés. `PauseMenuUI` était déjà clean.
- ✅ *(2026-05-24)* Blocs de code commentés retirés (auto-save dans
  `SaveGameManager`, quick-save dans `SaveMenuIntegration`).
- Supprimer le code mort de `AIDialogueManager` rendu inutile par la refonte B2 :
  `GetQuestInstructionsForNPC`, `GetAvailableQuestOptionsForAI`,
  `GetRoleSpecificQuestExamples` (~175 lignes).
- ✅ *(2026-05-25)* `PauseMenuUI.Pause()` retirée (aucun caller externe,
  pas de wire scène). `Resume()` conservée (utilisée par `SaveSystemUI`).
- ✅ *(2026-05-25)* `UnifiedUIManager.OpenPanel` / `ClosePanel` retirées
  (passe-plats sans caller externe).
- Nettoyer les `Debug.Log` restants : CSM*, DynamicAsset*, *Tester, NPCQuestTurnIn,
  QuestJournal, PlayerController, StaminaUI, CompanionSpeedSync, DialogueUI,
  AIDialogueManager.
- Audit « bonnes pratiques » **fait** → `.claude-docs/AUDIT_CODE.md` (C# style
  guide + architecture ScriptableObjects). Action concrète qui en ressort :
  remplacer les `FindObjectsByType` répétés (NPC, QuestZone, QuestObject) par
  des **SO Runtime Set** (enregistrement en `OnEnable`/`OnDisable`).
  ⏸️ *(2026-05-25)* Évalué : 14 occurrences, dont 2 dans `QuestMarkerSystem`
  appelées depuis Update. Pas urgent (aucun bottleneck profilé) — à
  reprendre si perf devient un sujet.

### 🔧 Code mort — supprimables (vérifier en scène avant ; dumps datés 2025-07)
- ✅ *(2026-05-25)* `NotificationTester`, `InventoryDebugger`, `AudioSettingsUI`,
  `MusicZoneTrigger`, `QuestMarkerCustomizer` supprimés (0 instance en scène,
  aucun caller).
- ✅ *(2026-05-24)* `APITester` retiré de `Game.unity`.
- ✅ *(2026-05-24)* `AssetManagerTester` retiré de `Game.unity`.

## Évolutions

- 🟡 **LLM embarqué** — un LLM dans le build pour jouer sans coût d'API.
  Spec : `.claude-docs/SPEC_LLM_local.md`. ✅ Faits : abstraction `IAIProvider`,
  multi-backend (OpenAI / Ollama), `gpt-4o-mini`, refonte B2 du dialogue,
  validation des tokens, mémoire de conversation. **Reste le cœur** : intégrer
  **LLMUnity** + un modèle GGUF dans le build, sélecteur Cloud/Local dans les
  Options, et choisir/mesurer le modèle embarquable sur PC modeste.
- 🔴 **Sécurité — clé API dans le build** : `APIConfig.OPENAI_API_KEY` est en
  dur, donc compilée dans le build → extractible par décompilation. Acceptable
  en dev, **interdit pour une distribution** (zip/Steam) : la clé se ferait
  pomper. À résoudre avec le LLM embarqué, ou un schéma « bring-your-own-key ».
- ✅ *(2026-05-24)* **Système de récompense / crédits** — `PlayerWallet`,
  `QuestRewardScale`, attribution au turn-in, save/load, toast au gain,
  **affichage dans l'inventaire** (ligne crédits auto-générée en tête).
- 🟡 **Mécanique d'escorte / PNJ qui se déplacent** — actuellement
  l'IA a interdiction de dire « suivez-moi » dans les prompts globaux
  parce qu'aucun PNJ ne se déplace réellement avec le joueur. Quand on
  aura une vraie mécanique de PNJ qui marche jusqu'à un lieu (escort),
  retirer cette règle dans les `AIPromptConfig` pour libérer ce type de
  formule. Pré-requis : path-following PNJ, état "en route", animation.

## Polish / assets

- 🟢 Matériaux : wood FX + material ; shader pour l'eau.
- 🟢 Assigner les sons au compagnon Poule.

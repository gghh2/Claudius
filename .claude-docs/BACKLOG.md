# BACKLOG — Claudius

> Travail à faire, priorisé. Sources : `Assets/TOTOLIST.txt` (todo de l'auteur)
> + audit code mort du 2026-05-20.
>
> Légende : 🔴 important · 🟡 moyen · 🟢 polish · 🔧 dette technique

## Bugs

- 🔴 La zone de fall-back n'a pas de vrai nom de zone (affiche « Laboratory »).
- 🟡 `F5` et `F9` ne font rien (raccourcis save/load rapide attendus ?).
- 🟡 Build&Run : le compagnon est très lent.
- 🟡 Script manquant sur le prefab `NPC_Quest` (`Assets/Prefabs/Quest/NPC_Quest.prefab`)
  — composant dont le script est cassé/supprimé (signalé au build).
- 🟡 Script manquant sur `Assets/Resources/QuestMarkerConfig.asset`.
- 🟡 Transparence caméra (`URPCameraObstacleHandler`) : un objet sur lequel le
  joueur **se tient** (ex. une Tombe) devient transparent. Le fade d'obstacle
  ne distingue pas « objet entre la caméra et le joueur » de « surface sous le
  joueur » — avec la caméra ortho inclinée, l'objet porteur est sur la ligne
  caméra→joueur. Derrière un objet (colonne) le comportement est correct.
  Piste : exclure du raycast l'objet sur lequel le joueur repose (sol courant).
- 🟡 Dialogue : re-parler à un PNJ affiche d'abord brièvement le **texte du
  dialogue précédent**, puis l'efface et le remplace par la nouvelle réponse.
  À étudier (`DialogueUI` / `AIDialogueManager` — sans doute un `ShowText` avec
  l'historique affiché avant l'arrivée de la réponse IA).
- 🔧 **14 erreurs `DontDestroyOnLoad`** au Play : la plupart des managers ne
  font pas `transform.SetParent(null)` avant `DontDestroyOnLoad` (seul
  `SaveGameManager` le fait). Liste : MeshyGenerator, CSMGenerator,
  CSMModelImporter, DynamicAssetManager, InventoryManager, SoundEffectsManager,
  MusicManager, AudioDistanceManager, QuestTokenDetector, QuestJournal,
  QuestManager, QuestZoneManager, QuestMarkerSystem, UnifiedUIManager,
  GlobalDebugManager, AIDialogueManager. Aligner sur le pattern de
  `SaveGameManager`.
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

## Quêtes

- ✅ *(2026-05-21 — refonte B2)* La génération de quête ne dépend plus du
  dialogue libre : un appel d'analyse séparé décide. Sur-proposition éradiquée,
  tokens absurdes rejetés par validation sémantique.
- 🟡 Doublon de PNJ : un token TALK/DELIVERY ciblant un PNJ déjà existant est
  désormais **rejeté** (mitigation). Mieux à terme : **réutiliser** ce PNJ comme
  cible de la quête plutôt que de rejeter le token.
- 🟡 DELIVERY : le PNJ de destination doit avoir un nom propre inventé.
- 🟡 EXPLORE : après une exploration demandée par un PNJ, fournir une explication /
  des « données » d'exploration au retour.
- 🟡 EXPLORE : zones à explorer de longueurs variables.
- 🟡 TALK : chaîne de quête — un PNJ A envoie voir B ; trouver B doit déclencher
  une mission de B (« TODO à rallonge »).
- 🟡 Nouveau type de quête **« Déterrer un trésor »** : place un marqueur à un
  endroit aléatoire de la carte ; le joueur s'y rend et déterre le trésor sur place.
- 🟡 Écran des quêtes : redesign en cours (chevauchement des entrées).
- 🟡 Quand il y a beaucoup de quêtes, le scroll masque une partie de la liste.
- 🟡 Notification écran (toast) à : nouvelle quête, quête terminée, nouvelle
  entrée de journal, **entrée dans une zone / un lieu** — aussi bien un nouveau
  lieu que le retour dans un lieu déjà connu. Le `NotificationManager` existe
  déjà — à câbler.

## Système de sauvegarde

- ✅ *(2026-05-24)* Mémoire de conversation des PNJ persistée dans le
  save/load (`ConversationsSaveData` ajoutée à `SaveData` ; serialise
  `conversationHistories` ET `conversationsByNpc` côté `AIDialogueManager`).
- 🟡 Au reload : les items de quête déjà ramassés ne doivent pas être recréés
  dans le monde.
- 🟡 Quête EXPLORE déjà explorée mais non rendue : comportement au reload à définir.

## Rendu / graphismes

> Issus de la session graphismes du 2026-05-22 (passage à un look type PoE).

- 🟡 Matériaux **magenta** : shaders cassés (non-URP / HDRP) → à reskinner en URP.
- 🟢 Feuillage **trop saturé** : matériaux de végétation d'un vert lime trop vif
  (ACES l'amplifie). Désaturer les matériaux, ou courbe Hue vs Sat ciblée sur le
  vert dans le Volume.
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
- **AdventureJournal** — feature complète mais non branchée : finir ou supprimer ?
- **DynamicAssets / CSM / Meshy** (~12 fichiers) — sous-système instancié en scène
  mais déconnecté du jeu (Phase 2 jamais faite) : reprendre ou geler proprement ?
- `MeshyGenerator` vs `CSMGenerator` — deux générateurs concurrents : lequel garder ?
- Garder `QuestMarkerDebugger` / `QuestDebugger` comme outils de debug, ou retirer ?

### 🔧 À nettoyer
- Package `com.unity.ai.toolkit` : appelle en boucle une API beta Unity retirée
  (`generators-beta.ai.unity.com` → `ApiNoLongerSupported`) → **pollue le log
  éditeur** même au repos. Le projet ne s'en sert pas → retirer ce package (et
  vérifier les autres `com.unity.ai.*` inutilisés).
- Consolider la gestion du curseur (3 contrôleurs : `SmartCursorManager`,
  `UnifiedUIManager`, `PauseMenuUI`) derrière `UnifiedUIManager`.
- ✅ *(2026-05-24)* Blocs de code commentés retirés (auto-save dans
  `SaveGameManager`, quick-save dans `SaveMenuIntegration`).
- Supprimer le code mort de `AIDialogueManager` rendu inutile par la refonte B2 :
  `GetQuestInstructionsForNPC`, `GetAvailableQuestOptionsForAI`,
  `GetRoleSpecificQuestExamples` (~175 lignes).
- `PauseMenuUI.Pause()` marquée *deprecated* — vérifier le câblage Inspector puis retirer.
- `UnifiedUIManager.OpenPanel` / `ClosePanel` — passe-plats *legacy*, à fusionner.
- Nettoyer les `Debug.Log` restants : CSM*, DynamicAsset*, *Tester, NPCQuestTurnIn,
  QuestJournal, PlayerController, StaminaUI, CompanionSpeedSync, DialogueUI,
  AIDialogueManager.
- Audit « bonnes pratiques » **fait** → `.claude-docs/AUDIT_CODE.md` (C# style
  guide + architecture ScriptableObjects). Action concrète qui en ressort :
  remplacer les `FindObjectsByType` répétés (NPC, QuestZone, QuestObject) par
  des **SO Runtime Set** (enregistrement en `OnEnable`/`OnDisable`).

### 🔧 Code mort — supprimables (vérifier en scène avant ; dumps datés 2025-07)
- `NotificationTester`, `InventoryDebugger` — scaffolding de debug, non attachés.
- `QuestMarkerCustomizer` — non câblé (rend mortes 2 méthodes de `QuestMarkerSystem`).
- `AudioSettingsUI` — remplacé par `PauseMenuUI` (constante `AudioSettings` inutilisée).
- `MusicZoneTrigger` — jamais placé en scène (ou WIP à assumer ?).
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
- 🟡 **Système de récompense / crédits** — quand le joueur termine une quête,
  une vraie récompense (crédits). Décision actée : le **jeu** fixe le montant
  (barème), pas l'IA — l'IA reste vague, ne cite aucun chiffre. À construire :
  porte-monnaie joueur, barème, attribution au turn-in, UI, save/load.

## Polish / assets

- 🟢 Matériaux : wood FX + material ; shader pour l'eau.
- 🟢 Assigner les sons au compagnon Poule.

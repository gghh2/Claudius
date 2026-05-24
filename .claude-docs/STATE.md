# STATE — Claudius

> Où en est le projet **maintenant**. Ce fichier est importé automatiquement par
> `CLAUDE.md`. À relire en début de session, à mettre à jour en fin de session.

**Dernière mise à jour :** 2026-05-24

## Statut global

- Le projet **compile proprement** sous Unity 6.2.
- **Système de récompense / crédits en place (2026-05-24)** : `PlayerWallet`
  (singleton + event `OnCreditsChanged`), `QuestRewardScale` (barème fixé côté
  jeu, l'IA reste vague), attribution automatique au turn-in
  (`QuestJournal.CompleteQuest`), toast via `NotificationManager`, save/load
  (`WalletSaveData`). Validé en jeu (toast `+X crédits` reçu sur quête finie).
- **Toggle des raccourcis I / J / L (2026-05-24)** : re-presser la touche du
  panneau ouvert le referme.
- **Bridge MCP For Unity opérationnel (2026-05-24)** : `com.coplaydev.unity-mcp`
  (transport HTTP `127.0.0.1:8080/mcp`). Donne accès direct à `read_console`,
  `execute_code`, `find_gameobjects`, etc. depuis Claude Code. Doc dans CLAUDE.md.
- **Dialogue IA refondu — architecture « B2 »** : le chat (roleplay) et la
  génération de quête sont **deux appels séparés**. Le chat ne peut plus produire
  de quête ; un appel d'analyse dédié décide seul. La **sur-proposition de
  quêtes** — problème majeur — est **éradiquée** (validé en jeu). Code mort des
  3 anciennes méthodes de prompt **supprimé (2026-05-24)** (~175 lignes).
- **Abstraction IA multi-backend** : OpenAI (`gpt-5.4-mini`) / Ollama (local) /
  mock, interchangeables. Menu éditeur `Tools > Claudius > IA`.
- **Mémoire de conversation par PNJ** dans la session de jeu : les PNJ se
  souviennent du joueur.
- **Validation sémantique des tokens de quête** : tokens absurdes rejetés
  (zone inconnue, destinataire = lieu, destinataire = PNJ déjà existant).
- AdventureJournal, consolidation curseur, migration Unity 6.2 : OK.
- **Rendu** : URP Asset correct actif, post-process recalibré, éclairage type
  PoE (SSAO, soleil franc, ombres lisibles), lightmap baké. Validé en jeu.
- **Build Windows fonctionnel** : Render Graph, budget samplers Lit rétabli.
- **IA cloud opérationnelle** : `gpt-5.4-mini` + `max_completion_tokens`.
- **Nettoyage logs (2026-05-24)** : NPC, NPCQuestTurnIn, DialogueUI,
  AIDialogueManager, UnifiedUIManager, GlobalDebugManager — tous gated derrière
  `GlobalDebugManager.IsDebugEnabled(...)`. Spam `OnValidate` éradiqué.
- **CharacterController réparé (2026-05-24)** : le projet avait basculé sur un
  backend physique non-PhysX après une migration Unity 6.2 → joueur bloqué et
  spam `Move called on inactive controller`. Backend remis sur PhysX dans
  Project Settings ; gardes défensifs ajoutés dans `PlayerControllerCC`.

## En cours / non terminé

- **LLM embarqué** *(prochaine grande étape)* — l'abstraction, le moteur local
  (Ollama, en dev) et toute l'architecture de dialogue sont prêts. Reste le
  **vrai** embarqué : intégrer **LLMUnity** + un modèle GGUF dans le build, et
  le sélecteur Cloud/Local dans les Options. Voir `SPEC_LLM_local.md`.
- **Mémoire cross-session** : les conversations PNJ persistent dans la session
  mais pas dans le save/load.
- **DynamicAssets / génération 3D** *(WIP en pause)* — décision à prendre :
  reprendre ou geler.

## Points d'attention connus

- Modèle de dev : `qwen2.5:7b` via Ollama. Le modèle réellement *embarquable*
  (PC modeste) reste à choisir et mesurer.
- Tics de petit modèle persistants (charabia occasionnel, mots étrangers) —
  limite du 3-7B, à remesurer avec le modèle embarqué final.
- **Polish graphismes restant** : quelques matériaux en magenta (shaders
  non-URP à reskinner), matériaux de feuillage trop saturés à la base,
  `OrthographicFogAdapter` mal calibré (réglé pour des tailles caméra 19-30
  alors que le zoom réel est 2-15), lightmap unique 1024² → AO/GI bakés grossiers.
- **Sécurité — clé API en dur** : `APIConfig.OPENAI_API_KEY` est compilée dans
  le build → extractible. Bloquant avant toute distribution (cf. BACKLOG).
- **Composants de test en scène** : `AssetManagerTester` / `APITester` tournent
  dans le build pour rien — à retirer (cf. BACKLOG).
- **Emojis en strings UI** rendent des carrés (police TMP sans glyphes). À
  purger ou remplacer par sprites TMP (cf. BACKLOG).
- **14 erreurs `DontDestroyOnLoad`** au Play : managers non détachés de leur
  parent `Manager` avant l'appel. Bénin mais bruyant (cf. BACKLOG).
- **Double tag "Player"** sur le mesh enfant en plus du root — à corriger
  un jour (cf. BACKLOG).

## Prochaine étape

- Au choix : **LLMUnity embarqué** (le cœur du « LLM local »), la **mémoire
  cross-session** (save/load des conversations PNJ), ou un **HUD crédits**
  (s'abonner à `PlayerWallet.OnCreditsChanged`).
- Hygiène : nettoyer les erreurs `DontDestroyOnLoad` (14 managers, mécanique).

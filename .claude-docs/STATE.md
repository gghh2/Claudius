# STATE — Claudius

> Où en est le projet **maintenant**. Ce fichier est importé automatiquement par
> `CLAUDE.md`. À relire en début de session, à mettre à jour en fin de session.

**Dernière mise à jour :** 2026-05-22

## Statut global

- Le projet **compile proprement** sous Unity 6.2.
- **Dialogue IA refondu — architecture « B2 »** : le chat (roleplay) et la
  génération de quête sont **deux appels séparés**. Le chat ne peut plus produire
  de quête ; un appel d'analyse dédié décide seul. La **sur-proposition de
  quêtes** — problème majeur — est **éradiquée** (validé en jeu).
- **Abstraction IA multi-backend** : OpenAI (`gpt-5.4-mini`) / Ollama (local) /
  mock, interchangeables. Menu éditeur `Tools > Claudius > IA`.
- **Mémoire de conversation par PNJ** dans la session de jeu : les PNJ se
  souviennent du joueur.
- **Validation sémantique des tokens de quête** : tokens absurdes rejetés
  (zone inconnue, destinataire = lieu, destinataire = PNJ déjà existant).
- AdventureJournal, consolidation curseur, migration Unity 6.2 : OK (sessions
  précédentes).
- **Refonte de l'éclairage (2026-05-22)** : le bon URP Asset est désormais actif
  (avant : celui d'un pack tiers), post-process recalibré (sursaturation
  éradiquée, grade ACES en mode HDR), éclairage retravaillé vers un rendu
  contrasté et atmosphérique type Path of Exile — SSAO, soleil franc, ombres
  lisibles, lightmap re-baké. Validé en jeu.
- **Build Windows fonctionnel (2026-05-22)** : passage à Render Graph (mode
  compatibilité déprécié retiré), budget de samplers du shader Lit rétabli
  (Light Cookies + ombres additionnelles désactivées). Le jeu se build et tourne.
- **IA cloud réparée (2026-05-22)** : `gpt-4o-mini` retiré par OpenAI →
  `gpt-5.4-mini` ; paramètre `max_completion_tokens` (exigé par GPT-5.x) ;
  accès au modèle ouvert côté projet OpenAI. Dialogue PNJ fonctionnel en build.
- **Bugs corrigés (2026-05-22)** : curseur absent sur MainMenu, chargement de
  save « Continue » (joueur introuvable), crash de build via `AssetManagerTester`.

## En cours / non terminé

- **LLM embarqué** *(prochaine grande étape)* — l'abstraction, le moteur local
  (Ollama, en dev) et toute l'architecture de dialogue sont prêts. Reste le
  **vrai** embarqué : intégrer **LLMUnity** + un modèle GGUF dans le build, et
  le sélecteur Cloud/Local dans les Options. Voir `SPEC_LLM_local.md`.
- **DynamicAssets / génération 3D** *(WIP en pause)* — décision à prendre :
  reprendre ou geler.

## Points d'attention connus

- Modèle de dev : `qwen2.5:7b` via Ollama. Le modèle réellement *embarquable*
  (PC modeste) reste à choisir et mesurer.
- Tics de petit modèle persistants (charabia occasionnel, mots étrangers) —
  limite du 3-7B, à remesurer avec le modèle embarqué final.
- Code mort dans `AIDialogueManager` (3 méthodes de prompt rendues inutiles par
  B2) — nettoyage prévu.
- **Polish graphismes restant** : quelques matériaux en magenta (shaders
  non-URP à reskinner), matériaux de feuillage trop saturés à la base,
  `OrthographicFogAdapter` mal calibré (réglé pour des tailles caméra 19-30
  alors que le zoom réel est 2-15), lightmap unique 1024² → AO/GI bakés grossiers.
- **Sécurité — clé API en dur** : `APIConfig.OPENAI_API_KEY` est compilée dans
  le build → extractible. Bloquant avant toute distribution (cf. BACKLOG).
- **Composants de test en scène** : `AssetManagerTester` / `APITester` tournent
  dans le build pour rien — à retirer (cf. BACKLOG).

## Prochaine étape

- Au choix : **LLMUnity embarqué** (le cœur du « LLM local »), le **système de
  récompense / crédits**, ou la **mémoire cross-session** (save/load).
- Plus petit : nettoyage du code mort de `AIDialogueManager`.

# STATE — Claudius

> Où en est le projet **maintenant**. Ce fichier est importé automatiquement par
> `CLAUDE.md`. À relire en début de session, à mettre à jour en fin de session.

**Dernière mise à jour :** 2026-05-21

## Statut global

- Le projet **compile proprement** sous Unity 6.2.
- **Dialogue IA refondu — architecture « B2 »** : le chat (roleplay) et la
  génération de quête sont **deux appels séparés**. Le chat ne peut plus produire
  de quête ; un appel d'analyse dédié décide seul. La **sur-proposition de
  quêtes** — problème majeur — est **éradiquée** (validé en jeu).
- **Abstraction IA multi-backend** : OpenAI (`gpt-4o-mini`) / Ollama (local) /
  mock, interchangeables. Menu éditeur `Tools > Claudius > IA`.
- **Mémoire de conversation par PNJ** dans la session de jeu : les PNJ se
  souviennent du joueur.
- **Validation sémantique des tokens de quête** : tokens absurdes rejetés
  (zone inconnue, destinataire = lieu, destinataire = PNJ déjà existant).
- AdventureJournal, consolidation curseur, migration Unity 6.2 : OK (sessions
  précédentes).

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

## Prochaine étape

- Au choix : **LLMUnity embarqué** (le cœur du « LLM local »), le **système de
  récompense / crédits**, ou la **mémoire cross-session** (save/load).
- Plus petit : nettoyage du code mort de `AIDialogueManager`.

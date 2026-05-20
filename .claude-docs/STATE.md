# STATE — Claudius

> Où en est le projet **maintenant**. Ce fichier est importé automatiquement par
> `CLAUDE.md`. À relire en début de session, à mettre à jour en fin de session.

**Dernière mise à jour :** 2026-05-20

## Statut global

- Le projet **compile proprement** sous Unity 6.2.
- Migration Unity 2021.3 → 6.2 terminée et poussée.
- **AdventureJournal livré** — journal de bord narratif IA : câblé, save/load OK.
- **Abstraction IA en place** (`IAIProvider`) — OpenAI / local / mock interchangeables.
- **Gestion du curseur consolidée** — une seule classe (`SmartCursorManager`) fait
  autorité, pilotée par `UnifiedUIManager.IsShowingPanel`.
- **Audit « bonnes pratiques »** (C# style guide + ScriptableObjects) fait →
  `AUDIT_CODE.md`. Verdict : projet sain, pas de réécriture de masse.
- Tout le travail de la session du 2026-05-20 est **commité et poussé**.

## En cours / non terminé

- **DynamicAssets / génération 3D** *(WIP en pause)* — pipeline CSM/Meshy présent
  et instancié en scène, Phase 2 non faite. **Décision à prendre : reprendre ou geler.**
- **LLM embarqué** *(planifié)* — l'abstraction IA (prérequis) est faite ; reste
  le runtime local + le toggle Options. Voir `SPEC_LLM_local.md`.

## Points d'attention connus

- Génération de quête IA : l'IA ne produit pas toujours un token `[QUEST:...]`
  valide. Piste retenue : génération contrainte (grammaire GBNF / schéma JSON) —
  voir `SPEC_LLM_local.md`.

## Prochaine étape

- Au choix : avancer le LLM local (`SPEC_LLM_local.md`) ou attaquer le `BACKLOG.md`
  — bugs quêtes prioritaires, **Runtime Set** (remplacer les `FindObjectsByType`),
  notifications écran.

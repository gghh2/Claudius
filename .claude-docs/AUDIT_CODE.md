# AUDIT — Bonnes pratiques (Unity)

> Audit du code à l'aune de deux e-books Unity 6 :
> « Use a C# style guide » et « Create modular game architecture with ScriptableObjects ».
>
> Principe directeur : *« Make it good, not perfect. Don't refactor for the sake
> of it. Improve incrementally. »* → **pas** de réécriture de masse. On adopte les
> conventions pour le **code neuf**, on corrige à l'occasion, on cible les rares
> points structurels à forte valeur.

## Conventions C# (rappel)

- `camelCase` : variables locales, paramètres. `PascalCase` : classes, méthodes,
  champs publics, enums.
- Booléens préfixés d'un verbe (`isDead`, `hasKey`).
- Un seul `MonoBehaviour` par fichier ; nom de fichier = nom de la classe.
- Interfaces préfixées `I`.
- `[SerializeField] private` plutôt que `public` pour les champs exposés à l'inspecteur.
- Accolades Allman (nouvelle ligne), conservées même pour les blocs d'une ligne.
- Namespaces recommandés (évite les conflits, surtout avec les assets du Store).
- Pas d'initialiseurs redondants (`= false`, `= 0`, `= null`).

## Écarts C# observés dans Claudius

| Écart | Gravité | Reco |
|---|---|---|
| **Pas de namespaces** — quasi tous les scripts sont dans le namespace global (sauf `DynamicAssets`). | 🟡 | Beaucoup d'assets Store → risque de conflit. Namespaces pour le code neuf, migration progressive. |
| **Champs `public` au lieu de `[SerializeField] private`** — répandu (`CameraFollow`…). | 🟡 | Code neuf : `[SerializeField] private`. Ne pas tout convertir d'un coup. |
| **Gestion du curseur éclatée sur 3 classes** | ✅ | **Réglé** — consolidée dans `SmartCursorManager` (commit `5a1843a`). |
| **`Debug.Log` non systématiquement gardés** par `GlobalDebugManager`. | 🟡 | Déjà au BACKLOG. |
| **Initialiseurs redondants** (`= false`, `= 0`…). | 🟢 | Retirer à l'occasion. |
| **Un fichier / un MonoBehaviour** — `AdventureJournalExtensions.cs` (static + MonoBehaviour `AdventureJournalIntegration`, nom de fichier ≠ classe). | 🟢 | Scinder / renommer si on y retouche. |

Choix du projet **jugés OK** (à garder par cohérence) : pas de préfixe `m_`/`s_`
sur les membres privés (le e-book l'autorise), indentation 4 espaces, accolades Allman.

## Architecture — ScriptableObjects

Les ScriptableObjects (SO) séparent données et logique, réduisent le couplage et
allègent la mémoire. Patterns de l'e-book : data container / flyweight, dual
serialization (SO ↔ JSON), extendable enums, delegate objects / pluggable
behavior, **event channels** (observer, anti-singleton), **Runtime Set**.

**Déjà bien fait** : `AIPromptConfig` est un SO (un asset par type de PNJ) →
pattern « data container » appliqué correctement.

| Opportunité | Valeur | Détail |
|---|---|---|
| **Runtime Set** | 🟡 concret | Le code fait `FindObjectsByType<NPC>()`, `<QuestZone>()`, `<QuestObject>()` à plusieurs endroits. Un SO « Runtime Set » (les objets s'enregistrent en `OnEnable`, se retirent en `OnDisable`) remplace ces scans de scène : plus rapide, découplé. **La meilleure cible SO.** |
| **Singletons → event channels** | 🟢 optionnel | Projet très « singleton » (`QuestManager.Instance`, `QuestJournal.Instance`, `AIDialogueManager.Instance`…). L'e-book voit le singleton comme un anti-pattern *à grande échelle*, mais le dit « adapté aux petits projets ». Pour un solo : **acceptable**. À envisager pour les features neuves ou les couplages les plus douloureux — pas en réécriture de masse. |
| **Config en SO** | 🟢 optionnel | `QuestSystemConfig` / `AudioConstants` sont des classes statiques. Les passer en SO n'a d'intérêt que si un designer doit les régler sans toucher au code. Sinon, statique = très bien. |
| **Pluggable behavior / audio delegates** | 🟢 idée | SO abstrait + sous-classes concrètes pour comportements interchangeables (mouvement PNJ, variations de sons). Sympa pour du contenu neuf, pas une priorité. |

Convention : suffixe `SO` ou `Data` sur les classes ScriptableObject (ex. `NPCConfigSO`).

## Recommandation — ordre de valeur

1. ✅ **Consolidation du curseur** — fait (commit `5a1843a`).
2. **Runtime Set** — remplacer les `FindObjectsByType` répétés (NPC, QuestZone,
   QuestObject) par des SO Runtime Set. Le gain ScriptableObject le plus concret.
3. Namespaces + `[SerializeField] private` — au **code neuf**, migration opportuniste.
4. Le reste (event channels, config-SO, initialiseurs, noms de fichiers) — au fil de l'eau.

Le projet est **sain**. Pas de réécriture de masse : on adopte les bonnes
pratiques pour le neuf et on cible les rares points à forte valeur.

# AUDIT — Bonnes pratiques C# (Unity)

> Audit du code à l'aune du e-book Unity « Use a C# style guide » (Unity 6).
> **Fait** : C# style guide. **Reste** : « Modular architecture with ScriptableObjects ».
>
> Principe directeur du e-book : *« Make it good, not perfect. Don't refactor for
> the sake of it. Improve incrementally. »* → **pas** de réécriture de masse. On
> adopte les conventions pour le **code neuf**, on corrige à l'occasion, et on
> cible les rares points structurels à forte valeur.

## Conventions de référence (rappel)

- `camelCase` : variables locales, paramètres. `PascalCase` : classes, méthodes,
  champs publics, enums.
- Booléens préfixés d'un verbe (`isDead`, `hasKey`).
- Un seul `MonoBehaviour` par fichier ; nom de fichier = nom de la classe.
- Interfaces préfixées `I`.
- `[SerializeField] private` plutôt que `public` pour les champs exposés à l'inspecteur.
- Accolades Allman (nouvelle ligne), conservées même pour les blocs d'une ligne.
- Namespaces recommandés (évite les conflits, surtout avec les assets du Store).
- Pas d'initialiseurs redondants (`= false`, `= 0`, `= null`).

## Écarts observés dans Claudius

| Écart | Gravité | Reco |
|---|---|---|
| **Pas de namespaces** — quasi tous les scripts sont dans le namespace global (sauf `DynamicAssets`). | 🟡 | Le projet a beaucoup d'assets Store → risque de conflit. Namespaces pour le code neuf, migration progressive. |
| **Champs `public` au lieu de `[SerializeField] private`** — répandu (`CameraFollow`…). | 🟡 | Code neuf : `[SerializeField] private`. Ne pas tout convertir d'un coup. |
| **Classes volumineuses / responsabilités multiples** — `UnifiedUIManager`, `PauseMenuUI` ; gestion du curseur éclatée sur 3 classes. | 🟡 | **Cible n°1 : consolidation du curseur.** |
| **`Debug.Log` non systématiquement gardés** par `GlobalDebugManager`. | 🟡 | Déjà au BACKLOG. |
| **Initialiseurs redondants** (`= false`, `= 0`…). | 🟢 | Retirer à l'occasion. |
| **Un fichier / un MonoBehaviour** — `AdventureJournalExtensions.cs` contient un static + le MonoBehaviour `AdventureJournalIntegration` (nom de fichier ≠ classe). | 🟢 | Scinder / renommer si on y retouche. |

## Choix du projet jugés OK (à garder, par cohérence)

- **Pas de préfixe `m_`/`s_`** sur les membres privés : le e-book l'autorise
  explicitement (« beaucoup de devs s'en passent et se fient à l'IDE »). À garder
  tel quel — l'essentiel est la **cohérence**.
- Indentation 4 espaces, accolades Allman : conformes.

## Recommandation — ordre de valeur

1. **Consolidation du curseur** — structurel, bug récurrent. À faire en priorité.
2. Namespaces + `[SerializeField] private` — appliqués au **code neuf**, migration
   opportuniste (pas de rename de masse).
3. Le reste (initialiseurs, noms de fichiers) — au fil de l'eau.

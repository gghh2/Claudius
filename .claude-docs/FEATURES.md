# FEATURES — Claudius

> Inventaire des fonctionnalités **livrées et fonctionnelles**. À mettre à jour à
> chaque feature terminée.
> Le WIP non terminé est dans `STATE.md`, le reste-à-faire dans `BACKLOG.md`.

## Boucle de jeu

- **Joueur** — déplacement (WASD), sprint, saut, stamina. `PlayerControllerCC`.
- **Caméra** — suivi orthographique du joueur, zoom molette (2-15). `CameraFollow`.
- **Footsteps** — sons + particules selon la surface. `FootstepSystem`.
- **Compagnon** — suit le joueur, wander à l'arrêt, presets animaux. `CompanionController`.

## PNJ & Dialogue

- **PNJ** — nom / rôle / description, détection de proximité, nom affiché en billboard.
- **Dialogue IA** — 100 % généré par OpenAI, aucune ligne pré-écrite. Contextuel
  (détecte les quêtes en cours, les livraisons). Touche Entrée pour envoyer.

## Quêtes

- **Génération** — quêtes créées par l'IA via tokens `[QUEST:...]`.
- **Types fonctionnels** — FETCH (collecte), DELIVERY (livraison), EXPLORE
  (présence en zone), TALK (parler à un PNJ). INTERACT / ESCORT partiels.
- **Suivi** — `QuestJournal` (quête suivie automatiquement), annulation possible.
- **Journal des quêtes** — panneau UI listant les quêtes actives / terminées.
- **Marqueurs** — flèches directionnelles vers les objectifs.

## Interface

- **Navigation unifiée** — `UnifiedUIManager` : pile de panels, gestion ESC, layers.
- **Menu principal** — scène dédiée + transitions (`MainMenuUI`, `SceneNavigationManager`).
- **Menu pause** — Reprendre / Respawn / Save-Load / Options / Quitter.
- **Inventaire** — fenêtre d'inventaire avec items de quête.
- **Notifications** — toasts (succès / info).
- **Journal de bord** — panneau narratif : les événements de jeu (quêtes, zones,
  rencontres PNJ) sont transformés en récit immersif par l'IA. Save/load inclus.

## Systèmes

- **Sauvegarde / chargement** — 4 slots, JSON. Restaure joueur, compagnon, quêtes
  (avec positions des objets), PNJ, inventaire, audio, zoom caméra.
- **Audio** — musique par zone (crossfade), SFX 3D poolés, zones d'ambiance,
  volume fonction du zoom caméra.
- **Debug** — `GlobalDebugManager` (flags par système), `HierarchyDebugger`.
- **Abstraction IA** — `IAIProvider` : le moteur d'IA (OpenAI cloud, mock de
  test, futur LLM local) est interchangeable via `AIService.Provider`, sans
  toucher au code de jeu.

## Rendu

- **Pipeline** — URP 17.3, espace colorimétrique linéaire, caméra orthographique.
  Le Render Pipeline Asset du projet (`Assets/Settings/`) est l'actif — pas
  celui d'un pack tiers.
- **Post-traitement** — Volume par défaut : Tonemapping **ACES** en mode de
  grading **HDR**, contraste léger, Split Toning (ombres froides / hautes
  lumières chaudes), vignette. **SSAO** actif (Renderer Feature).
- **Éclairage** — soleil directionnel franc, ombres douces lisibles, ambiant
  skybox, lightmap baké (Mixed / Shadowmask). Rendu contrasté et atmosphérique.

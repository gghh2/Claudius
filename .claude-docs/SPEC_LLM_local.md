# SPEC — LLM embarqué (option Cloud / Local)

> Spécification de conception. Statut : **à valider**, non implémenté.
> Référencé par `BACKLOG.md`. Les numéros de version précis (packages, modèles)
> sont à reconfirmer au moment de l'implémentation.

## Objectif

Pouvoir jouer **sans coût d'API**. Offrir, dans le menu Options, le choix du
moteur d'IA :
- **Cloud** — OpenAI (comportement actuel), qualité maximale, payant.
- **Local** — un LLM embarqué dans le jeu, gratuit à l'usage, qualité moindre.

## État actuel (le point de départ)

- OpenAI est appelé **en dur, à plusieurs endroits** :
  - `AIDialogueManager` — dialogue PNJ + génération des tokens `[QUEST:...]`.
  - `AdventureJournalUI.GetAINarration()` — narration du journal (appel direct
    à `api.openai.com`, indépendant de `AIDialogueManager`).
  - `DynamicAssets` (CSM/Meshy) a ses propres appels — **hors périmètre** ici.
- Modèle : `gpt-3.5-turbo`. Clé dans `APIConfig.cs`.
- **Aucune abstraction** → impossible de changer de moteur sans toucher chaque appelant.

## Étape 1 — Abstraction du backend IA *(prérequis, à faire en premier)*

Introduire une interface commune, par ex. `IAIProvider` :
- une méthode de complétion (messages + paramètres → texte), en gardant le style
  **coroutine / `UnityWebRequest`** déjà utilisé pour rester homogène ;
- `OpenAIProvider` — encapsule l'appel actuel ;
- `LocalLLMProvider` — le futur moteur local.

`AIDialogueManager` et `AdventureJournalUI` passent par `IAIProvider` au lieu
d'appeler OpenAI directement. Le provider actif est résolu via une config/singleton
lue depuis les préférences joueur.

**Bénéfice immédiat, même sans LLM local** : pouvoir basculer le modèle cloud
d'une ligne — voir « Gain rapide » plus bas.

## Étape 2 — Runtime local

Deux routes :

### Option A — LLMUnity *(recommandé)*
Package open-source qui embarque **llama.cpp** dans Unity.
- Modèles **GGUF** quantifiés, inférence CPU et GPU, fonctionne en build (desktop).
- API de chat simple et asynchrone ; gère chargement du modèle, contexte, sampling.
- Mise en œuvre rapide.

### Option B — Unity Inference Engine (Sentis)
- `com.unity.ai.inference` est **déjà dans le manifest** (ajouté à la migration 6.2).
- Exécute de l'**ONNX**. Plus intégré à Unity, mais **bas niveau** pour du chat :
  tokenizer, KV-cache et sampling à intégrer soi-même.

➡️ **Recommandation : LLMUnity** pour la rapidité de mise en œuvre.

## Étape 3 — Choix du modèle

Compromis taille de build / qualité / vitesse. Candidats (quantification ~Q4,
tailles indicatives) :

| Modèle | Taille ~Q4 | Remarque |
|---|---|---|
| Llama-3.2-1B-Instruct | ~0,8 Go | Le plus léger |
| Qwen2.5-1.5B-Instruct | ~1 Go | Bon suivi d'instructions pour sa taille |
| Gemma-2-2B-it | ~1,6 Go | Équilibré |
| Qwen2.5-3B / Phi-3.5-mini | ~2–2,3 Go | Meilleur suivi d'instructions |

- **Narration du journal** : peu exigeant — un modèle 1–1,5B suffit.
- **Dialogue PNJ + tokens `[QUEST:...]`** : exige un bon *instruction-following* —
  privilégier Qwen2.5-3B ou Phi-3.5-mini.

## Étape 4 — Option dans les Options

- Sélecteur dans le panneau Options : « IA : Cloud (OpenAI) / Locale ».
- Persistance via `PlayerPrefs`.
- Au changement, le `IAIProvider` actif est recalculé.
- Cas d'erreur : « Locale » choisi mais modèle absent → message clair, repli sur Cloud.

## Compromis & risques

- **Taille du build** : +0,8 à 2+ Go (le modèle est embarqué, p. ex. via `StreamingAssets`).
- **Performance** : 1er chargement lent ; débit tokens/s dépend du CPU/GPU.
  Prévoir un retour visuel « génération… » (le journal en a déjà un).
- **Qualité** : local < GPT. Risque principal : tokens `[QUEST:...]` mal formés
  → robustifier `QuestTokenDetector` (déjà au `BACKLOG.md` : l'IA produit parfois
  des tokens invalides).
- **Plateformes** : OK desktop. Mobile/console = contraintes fortes (mémoire, taille).
- **Prompts** : ceux réglés pour GPT ne sont pas optimaux pour un petit modèle —
  prévoir une passe de tuning dédiée.

## Fiabilité des tokens de quête (`[QUEST:...]`)

Risque clé d'un modèle local : produire des tokens `[QUEST:...]` mal formés
(GPT lui-même en produit parfois — voir `BACKLOG.md`). La réponse n'est pas
d'espérer, mais de **contraindre** la génération :

- **llama.cpp** supporte les **grammaires GBNF** (*constrained sampling*) : la
  sortie est forcée à une grammaire → un token mal formé devient impossible par
  construction (à confirmer : exposition via LLMUnity).
- **OpenAI** : équivalent via *JSON mode* / *structured outputs* (schéma JSON).
- **Évolution recommandée** : passer le quest token du texte libre
  (`[QUEST:FETCH:...]`) à un **objet JSON conforme à un schéma** — validable
  automatiquement, contraignable des deux côtés, et qui corrige aussi les ratés
  actuels de GPT.
- L'abstraction permettra de **mesurer** le taux de tokens valides d'un modèle
  (via `OllamaProvider` en dev) avant de s'engager.

NB : l'**interprétation** des tokens (`QuestTokenDetector`) est du code C#
déterministe — elle ne dépend pas du modèle. Seule la **génération** est en jeu.

## Découpage proposé

1. ✅ **FAIT** — Abstraction `IAIProvider` : OpenAI + Mock derrière l'interface,
   `AIDialogueManager` et `AdventureJournalUI` migrés, rien cassé.
2. Intégration LLMUnity + un modèle → `LocalLLMProvider`.
3. Sélecteur Options + persistance.
4. Tuning des prompts pour le modèle local.

## Gain rapide (avant même le LLM local)

Une fois l'étape 1 faite, basculer le modèle cloud de `gpt-3.5-turbo` vers
**`gpt-4o-mini`** : sensiblement **moins cher** et de **meilleure qualité**.
Réduit déjà fortement le coût sans aucun compromis.

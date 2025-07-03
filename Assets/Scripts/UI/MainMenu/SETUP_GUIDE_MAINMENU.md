# Guide de Configuration des Scènes Unity - Projet Claudius

## 📋 Prérequis
- Avoir sauvegardé votre projet
- Unity fermé puis réouvert pour charger les nouveaux scripts

## 🎬 Étape 1 : Sauvegarder la scène actuelle comme "Game"

1. Ouvrez votre scène actuelle dans Unity
2. **File → Save As...**
3. Nommez-la **"Game"**
4. Sauvegardez dans le dossier `Assets/Scenes/`

## 🎨 Étape 2 : Créer la scène MainMenu

1. **File → New Scene**
2. Choisissez **"Basic (Built-in)"**
3. **File → Save As...**
4. Nommez-la **"MainMenu"**
5. Sauvegardez dans le dossier `Assets/Scenes/`

## 🏗️ Étape 3 : Construire le Menu Principal

### A. Structure de base

1. **Créez la hiérarchie suivante** (clic droit dans Hierarchy) :

```
MainMenu (Scene)
├── Main Camera
├── Directional Light (optionnel)
├── EventSystem (créé automatiquement avec le Canvas)
├── Canvas
│   ├── Background
│   ├── MainMenuPanel
│   ├── LoadGamePanel
│   ├── OptionsPanel
│   ├── CreditsPanel
│   └── LoadingScreen
├── Managers
│   ├── MainMenuManager
│   ├── SceneNavigationManager
│   ├── SaveGameManager
│   ├── MusicManager (optionnel)
│   └── SoundEffectsManager (optionnel)
```

### B. Configuration du Canvas

1. **Sélectionnez le Canvas**
2. Dans l'Inspector :
   - **Canvas Scaler** → UI Scale Mode : **Scale With Screen Size**
   - Reference Resolution : **1920 x 1080**
   - Screen Match Mode : **0.5**

### C. Créer le Background

1. **Clic droit sur Canvas → UI → Image**
2. Renommez en **"Background"**
3. Dans Rect Transform :
   - Anchor Preset : **Stretch/Stretch** (Alt+Shift+Click sur le carré en bas-droite)
   - Left, Top, Right, Bottom : **0**
4. Image Component :
   - Color : **Noir** ou assignez une image de fond

### D. Créer le MainMenuPanel

1. **Clic droit sur Canvas → UI → Panel**
2. Renommez en **"MainMenuPanel"**
3. Position au centre
4. Ajoutez les boutons :

```
MainMenuPanel
├── Title (TextMeshPro)
├── ButtonContainer (Empty GameObject)
│   ├── NewGameButton
│   ├── ContinueButton
│   ├── LoadGameButton
│   ├── OptionsButton
│   ├── CreditsButton
│   └── QuitButton
```

### E. Configuration des boutons

Pour chaque bouton :
1. **Clic droit sur ButtonContainer → UI → Button - TextMeshPro**
2. Configurez :
   - Width : **300**
   - Height : **50**
   - Espacement vertical : **10** pixels

Textes des boutons :
- New Game → "Nouvelle Partie"
- Continue → "Continuer"
- Load Game → "Charger"
- Options → "Options"
- Credits → "Crédits"
- Quit → "Quitter"

### F. Créer les autres panels

Pour LoadGamePanel, OptionsPanel, CreditsPanel :
1. **Dupliquez MainMenuPanel** (Ctrl+D)
2. Renommez
3. **Désactivez-les** (décochez la case en haut de l'Inspector)
4. Ajoutez un bouton "Retour" dans chaque panel

### G. Créer le LoadingScreen

1. **Clic droit sur Canvas → UI → Panel**
2. Renommez en **"LoadingScreen"**
3. Configurez en plein écran (Stretch/Stretch)
4. Ajoutez :
   - **LoadingText** (TextMeshPro) : "Chargement..."
   - **LoadingBar** (Slider) : pour la progression
5. **Désactivez LoadingScreen**

## 🔧 Étape 4 : Configuration des Managers

### A. MainMenuManager

1. Dans **Managers**, créez un GameObject vide **"MainMenuManager"**
2. Ajoutez le script **MainMenuManager.cs**
3. Dans l'Inspector, assignez :
   - New Game Button
   - Continue Button
   - Load Game Button
   - Options Button
   - Credits Button
   - Quit Button
   - Main Menu Panel
   - Load Game Panel
   - Options Panel
   - Credits Panel
   - Loading Screen
   - Loading Bar
   - Loading Text

### B. SceneNavigationManager

1. Créez un GameObject vide **"SceneNavigationManager"**
2. Ajoutez le script **SceneNavigationManager.cs**
3. Configurez :
   - Main Menu Scene Name : **MainMenu**
   - Game Scene Name : **Game**

### C. SaveGameManager (si pas déjà présent)

1. Créez un GameObject vide **"SaveGameManager"**
2. Ajoutez le script **SaveGameManager.cs**

### D. Configuration du LoadGamePanel

Dans LoadGamePanel :
1. Ajoutez le composant **SimpleSaveGameUI** ou **SaveSystemUI**
2. Configurez les références UI

## 🎮 Étape 5 : Configuration de la scène Game

1. **Ouvrez la scène "Game"**
2. Trouvez ou créez **"GameStartupManager"**
3. Ajoutez/vérifiez le script **GameStartupManager.cs**
4. Créez un **"SceneNavigationManager"** (comme dans MainMenu)

## 🏃 Étape 6 : Configuration du Build

1. **File → Build Settings**
2. **Add Open Scenes** pour ajouter la scène courante
3. Réorganisez pour avoir :
   - Index 0 : **MainMenu**
   - Index 1 : **Game**
4. L'ordre est important ! MainMenu doit être en premier

## ✅ Étape 7 : Test

1. **Ouvrez la scène MainMenu**
2. **Appuyez sur Play**
3. Testez :
   - **Nouvelle Partie** → charge la scène Game
   - Dans Game : **ESC** → **Menu Principal** → retour à MainMenu
   - **Continuer** → charge la dernière sauvegarde
   - **Quitter** → ferme le jeu

## 🎨 Étape 8 : Personnalisation (Optionnel)

### Style visuel
- Ajoutez une image de fond dans Background
- Personnalisez les couleurs des boutons
- Ajoutez des animations d'apparition

### Audio
- Ajoutez une musique de menu
- Sons de survol/clic sur les boutons

### Effets
- Particules en arrière-plan
- Transitions entre panels

## ⚠️ Problèmes courants

1. **"Scene not found"** → Vérifiez Build Settings
2. **Boutons non cliquables** → Vérifiez EventSystem
3. **UI trop petite/grande** → Ajustez Canvas Scaler
4. **Loading infini** → Vérifiez les noms de scènes

## 💡 Tips

- Utilisez des préfabs pour les boutons répétitifs
- Testez sur différentes résolutions
- Gardez une copie de sauvegarde avant modifications majeures
- Le SceneNavigationManager doit être en DontDestroyOnLoad

---

Ce guide vous permettra de créer un système de menu principal professionnel pour votre jeu Claudius !

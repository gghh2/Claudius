# Guide de conversion vers URP (Universal Render Pipeline)

## ⚠️ IMPORTANT : Sauvegardez votre projet avant de commencer !

## Étape 1 : Installation d'URP

### 1.1 Ouvrir le Package Manager
- Window → Package Manager
- En haut à gauche : sélectionnez "Unity Registry"

### 1.2 Installer Universal RP
- Recherchez "Universal RP"
- Cliquez sur "Install" (version recommandée pour Unity 2021+ : 12.x ou plus)
- Attendez la fin de l'installation

## Étape 2 : Créer les assets URP

### 2.1 Créer le Pipeline Asset
1. Clic droit dans le Project → Create → Rendering → URP Asset (with Universal Renderer)
2. Cela crée deux fichiers :
   - `UniversalRenderPipelineAsset` (le pipeline)
   - `UniversalRenderPipelineAsset_Renderer` (le renderer)

### 2.2 Configurer le Pipeline
1. Sélectionnez le `UniversalRenderPipelineAsset`
2. Dans l'Inspector, configurez :
   - **Quality** → Anti Aliasing : FXAA ou SMAA
   - **Lighting** → Additional Lights : Per Pixel
   - **Shadows** → Soft Shadows : ✓ (si vous voulez des ombres douces)

## Étape 3 : Activer URP

### 3.1 Project Settings
1. Edit → Project Settings → Graphics
2. Dans **Scriptable Render Pipeline Settings**, glissez votre `UniversalRenderPipelineAsset`
3. Unity va recompiler tous les shaders (ça peut prendre un moment)

### 3.2 Quality Settings (Optionnel)
1. Edit → Project Settings → Quality
2. Pour chaque niveau de qualité, assignez le même `UniversalRenderPipelineAsset`

## Étape 4 : Convertir les matériaux

### 4.1 Conversion automatique
1. Edit → Rendering → Materials → Convert Selected Built-in Materials to URP
2. Ou pour tout convertir : Edit → Rendering → Materials → Convert Project Materials to URP

### 4.2 Vérification manuelle
Les shaders Built-in deviennent :
- `Standard` → `Universal Render Pipeline/Lit`
- `Mobile/Diffuse` → `Universal Render Pipeline/Simple Lit`
- `Unlit` → `Universal Render Pipeline/Unlit`
- `Sprites/Default` → `Universal Render Pipeline/2D/Sprite-Lit-Default`

## Étape 5 : Adapter les scripts

### 5.1 Post-Processing
- Supprimez l'ancien Post Processing Stack v2
- Utilisez le nouveau système de Volumes :
  1. GameObject → Volume → Global Volume
  2. Créez un nouveau Profile
  3. Add Override → Post-processing → [Effet désiré]

### 5.2 Caméra
1. Sélectionnez votre Main Camera
2. Dans l'Inspector, vérifiez :
   - **Rendering** → Renderer : Use Pipeline Settings
   - **Rendering** → Post Processing : ✓
   - **Environment** → Background Type : Solid Color (pour orthographique)

## Étape 6 : Résoudre les problèmes courants

### Problème : Écran rose/violet
**Solution** : Les shaders ne sont pas compatibles
- Reconvertissez les matériaux
- Vérifiez les shaders custom

### Problème : Éclairage différent
**Solution** : URP gère l'éclairage différemment
- Ajustez l'intensité des lumières (souvent × 0.5)
- Configurez les Additional Lights dans le pipeline asset

### Problème : UI disparue ou bizarre
**Solution** : 
- Canvas → Additional Shader Channels → Everything
- Vérifiez que le shader UI est bien URP

### Problème : Particules noires
**Solution** :
- Particle System → Renderer → Material : Default-Particle (URP)

## Étape 7 : Optimisations spécifiques pour votre projet

### Pour une caméra orthographique
1. Dans `UniversalRenderPipelineAsset` :
   - HDR : Off (sauf si nécessaire)
   - MSAA : 2x ou 4x
   - Render Scale : 1

### Pour les performances mobiles
- Lighting → Per Object Limit : 4
- Shadows → Max Distance : 50-100
- LOD Bias : 1-2

## Étape 8 : Profiter des nouvelles fonctionnalités !

### Shader Graph
- Window → Shader Graph → Create → URP → Lit/Unlit
- Créez des shaders visuellement !

### 2D Renderer (pour jeux 2D)
1. Create → Rendering → URP → 2D Renderer
2. Assignez-le dans votre Pipeline Asset

### Camera Stacking
- Superposez plusieurs caméras pour des effets complexes

## Checklist finale

- [ ] Projet sauvegardé
- [ ] URP installé via Package Manager
- [ ] Pipeline Asset créé et assigné
- [ ] Matériaux convertis
- [ ] Post-processing migré vers Volumes
- [ ] Éclairage ajusté
- [ ] Tests sur toutes les scènes

## Ressources utiles

- [Documentation URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)
- [Guide de migration officiel](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/InstallURPIntoAProject.html)

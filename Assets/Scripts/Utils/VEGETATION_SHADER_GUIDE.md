# Guide Shader Graph pour Végétation avec Fond Noir

## Créer un shader de végétation custom

### 1. Créer le Shader Graph
- Clic droit → Create → Shader Graph → URP → Lit Shader Graph
- Nommez-le "VegetationCutout"

### 2. Configuration du Graph

#### Propriétés à ajouter :
- **Base Texture** (Texture2D) - Votre texture de feuilles
- **Cutoff** (Float, 0.5) - Seuil de transparence
- **Wind Strength** (Float, 0.1) - Force du vent
- **Wind Speed** (Float, 1) - Vitesse du vent

#### Nodes à connecter :

1. **Pour la transparence du fond noir :**
```
[Sample Texture 2D] → [Split]
                         ↓
                    (R channel)
                         ↓
                    [One Minus] (si fond noir)
                         ↓
                    [Step] (avec Cutoff)
                         ↓
                    Alpha Clip Threshold
```

2. **Pour l'animation de vent (optionnel) :**
```
[Time] → [Sine] → [Multiply by Wind Strength]
                            ↓
                    [Add to Vertex Position X]
```

### 3. Settings du Master Stack
- **Surface** : Opaque
- **Alpha Clipping** : ✓
- **Two Sided** : ✓

### 4. Optimisations
- Utilisez LODs pour les plantes distantes
- Batching avec GPU Instancing
- Simplifiez les shaders pour mobile

## Exemples de paramètres

### Herbe
- Cutoff : 0.3
- Wind Strength : 0.2
- Two Sided : Oui

### Feuilles d'arbre
- Cutoff : 0.5
- Wind Strength : 0.1
- Cast Shadows : Two Sided

### Buissons
- Cutoff : 0.4
- Wind Strength : 0.15
- Receive Shadows : Oui

# Système de Marqueurs de Quête

## Fichiers principaux
- **QuestMarkerSystem.cs** : Système principal de marqueurs
- **QuestMarkerDebugger.cs** : Outil de debug (optionnel)
- **QuestMarkerCustomizer.cs** : Personnalisation des sprites

## Configuration rapide

### 1. Installation
Créez un GameObject vide et ajoutez le composant `QuestMarkerSystem`.

### 2. Personnalisation des marqueurs
Dans l'Inspector :
- **Custom Marker Sprite** : Votre image PNG
- **Use Custom Sprite** : Activer/désactiver
- **Custom Sprite Size** : Taille du marqueur

### 3. Sprites recommandés
- Format : PNG avec transparence
- Taille : 64x64 ou 128x128 pixels
- Import : Sprite (2D and UI)

## Utilisation par code
```csharp
// Changer le sprite
QuestMarkerSystem.Instance.SetMarkerSprite(mySprite);

// Changer la taille
QuestMarkerSystem.Instance.SetCustomSpriteSize(new Vector2(64, 64));

// Retour au carré jaune par défaut
QuestMarkerSystem.Instance.SetMarkerSprite(null);
```

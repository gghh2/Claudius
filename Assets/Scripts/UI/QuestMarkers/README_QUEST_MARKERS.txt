# Système de Marqueurs de Quête

## Installation rapide

1. **Créer la configuration**
   - Clic droit dans Project → Create → Quest System → Quest Marker Configuration
   - Placer dans un dossier "Resources" (créer si nécessaire)
   - Nommer "QuestMarkerConfig"

2. **Configurer les paramètres**
   - **Marker Color** : Couleur des flèches
   - **Hide Distance** : Distance de disparition (10m par défaut)
   - **Marker Size** : Taille des flèches (60 par défaut)
   - **Debug Mode** : Activer pour voir les logs

3. **Ajouter le tag aux zones**
   - Sur chaque GameObject avec QuestZone
   - Inspector → Tag → Add Tag → "QuestZone"
   - Assigner le tag

## Utilisation

- Les marqueurs apparaissent automatiquement pour les quêtes actives
- Ils pointent vers les zones contenant des objectifs
- Disparaissent quand vous êtes proche (< Hide Distance)

## Dépannage

**Pas de marqueurs ?**
- Vérifier que les zones ont le tag "QuestZone"
- Vérifier qu'une quête est active
- Vérifier la console pour les warnings

**Configuration non appliquée ?**
- L'asset doit être dans "Resources/QuestMarkerConfig"
- Relancer le jeu après modification

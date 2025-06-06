using System.Collections.Generic;
using UnityEngine;

namespace DynamicAssets.Core
{
    /// <summary>
    /// Version simplifiée du mapping pour démarrer rapidement
    /// </summary>
    public static class SimpleAssetMapping
    {
        /// <summary>
        /// Dictionnaire statique des mappings par défaut
        /// </summary>
        private static Dictionary<string, string> defaultMappings = new Dictionary<string, string>
        {
            // OBJETS SCIENTIFIQUES
            ["cristal_energie"] = "glowing blue energy crystal with magical aura, translucent material, fantasy sci-fi style",
            ["echantillon_alien"] = "alien biological sample in glass containment tube, green glowing liquid, sci-fi laboratory",
            ["donnees_secretes"] = "futuristic data chip with holographic elements, metallic surface with blue LED lights",
            
            // OBJETS MÉDICAUX
            ["medicament_rare"] = "medical vial with glowing healing potion, cork stopper, fantasy RPG style",
            ["vaccin_experimental"] = "high-tech syringe with blue serum, futuristic medical device, clean white plastic",
            
            // OBJETS TECHNOLOGIQUES
            ["composant_electronique"] = "futuristic electronic component with visible circuits, LED indicators, metallic housing",
            ["carte_circuit"] = "high-tech circuit board with glowing traces, microchips, green PCB color",
            
            // ARTEFACTS
            ["artefact_alien"] = "ancient alien artifact with mysterious symbols, dark metal with purple glow",
            ["relique_ancienne"] = "ancient stone relic with carved symbols, weathered surface, archaeological find",
            
            // OBJETS INTERACTABLES
            ["terminal_recherche"] = "futuristic research terminal with holographic display, sleek design, blue interface",
            ["console_securite"] = "high-tech security console with multiple screens, red warning lights, metallic frame",
            
            // RESSOURCES
            ["minerai_rare"] = "rare mineral ore with crystal formations, metallic veins, geological specimen",
            ["fragment_temporel"] = "temporal fragment with time distortion effects, swirling energy, translucent crystal"
        };
        
        /// <summary>
        /// Style par défaut pour tous les assets
        /// </summary>
        private static string defaultStyle = "low-poly game asset, clean textures, optimized for real-time rendering";
        
        /// <summary>
        /// Récupère le prompt visuel pour un nom d'objet
        /// </summary>
        public static string GetVisualPrompt(string itemName)
        {
            if (defaultMappings.ContainsKey(itemName))
            {
                return $"{defaultMappings[itemName]}, {defaultStyle}";
            }
            
            // Fallback automatique
            return GenerateAutoPrompt(itemName);
        }
        
        /// <summary>
        /// Génère automatiquement un prompt si pas de mapping trouvé
        /// </summary>
        private static string GenerateAutoPrompt(string itemName)
        {
            // Convertit nom technique → description lisible
            string humanReadable = itemName.Replace('_', ' ').ToLower();
            
            // Ajoute le style par défaut
            return $"{humanReadable}, {defaultStyle}";
        }
        
        /// <summary>
        /// Ajoute un nouveau mapping dynamiquement
        /// </summary>
        public static void AddMapping(string itemName, string visualPrompt)
        {
            defaultMappings[itemName] = visualPrompt;
            Debug.Log($"📝 Mapping ajouté: {itemName} → {visualPrompt}");
        }
        
        /// <summary>
        /// Vérifie si un mapping existe
        /// </summary>
        public static bool HasMapping(string itemName)
        {
            return defaultMappings.ContainsKey(itemName);
        }
        
        /// <summary>
        /// Récupère tous les noms d'objets mappés
        /// </summary>
        public static string[] GetAllItemNames()
        {
            List<string> items = new List<string>(defaultMappings.Keys);
            items.Sort();
            return items.ToArray();
        }
        
        /// <summary>
        /// Debug : affiche tous les mappings
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public static void DebugMappings()
        {
            Debug.Log($"📋 SimpleAssetMapping initialisé avec {defaultMappings.Count} mappings");
            foreach (var mapping in defaultMappings)
            {
                Debug.Log($"  • {mapping.Key} → {mapping.Value}");
            }
        }
    }
    

}
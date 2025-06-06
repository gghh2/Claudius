using System.Collections.Generic;
using UnityEngine;

namespace DynamicAssets.Core
{
    /// <summary>
    /// Gère la correspondance entre noms techniques d'objets et prompts visuels pour l'IA
    /// </summary>
    [System.Serializable]
    public class AssetPromptMapping
    {
        public string itemName;           // Nom technique (ex: "cristal_energie")
        public string visualPrompt;       // Prompt pour l'IA (ex: "glowing blue energy crystal")
        public string style;             // Style artistique spécifique
        public string[] tags;            // Tags pour catégorisation
        public AssetQuality targetQuality; // Qualité cible
        public int targetTriangles;      // Nombre de triangles souhaité
        
        public AssetPromptMapping(string itemName, string visualPrompt, string style = "game asset")
        {
            this.itemName = itemName;
            this.visualPrompt = visualPrompt;
            this.style = style;
            this.targetQuality = AssetQuality.Medium;
            this.targetTriangles = 1000;
            this.tags = new string[0];
        }
    }
    
    /// <summary>
    /// ScriptableObject pour configurer les mappings dans l'éditeur Unity
    /// </summary>
    [CreateAssetMenu(fileName = "AssetMappingConfig", menuName = "Dynamic Assets/Asset Mapping Config")]
    public class AssetMappingConfig : ScriptableObject
    {
        [Header("Configuration")]
        public string version = "1.0";
        public string defaultStyle = "low-poly game asset, clean textures";
        public AssetQuality defaultQuality = AssetQuality.Medium;
        public int defaultTriangles = 1000;
        
        [Header("Prompt Mappings")]
        public List<AssetPromptMapping> mappings = new List<AssetPromptMapping>();
        
        [Header("Style Presets")]
        public List<StylePreset> stylePresets = new List<StylePreset>();
        
        /// <summary>
        /// Initialise avec des mappings par défaut
        /// </summary>
        [ContextMenu("Initialize Default Mappings")]
        public void InitializeDefaults()
        {
            mappings.Clear();
            
            // OBJETS SCIENTIFIQUES
            AddMapping("cristal_energie", 
                "glowing blue energy crystal with magical aura, translucent material, fantasy sci-fi style",
                "fantasy", new[] { "energy", "crystal", "magic" });
                
            AddMapping("echantillon_alien", 
                "alien biological sample in glass containment tube, green glowing liquid, sci-fi laboratory",
                "sci-fi", new[] { "alien", "biology", "sample" });
                
            AddMapping("donnees_secretes", 
                "futuristic data chip with holographic elements, metallic surface with blue LED lights",
                "cyberpunk", new[] { "data", "technology", "secret" });
            
            // OBJETS MÉDICAUX
            AddMapping("medicament_rare", 
                "medical vial with glowing healing potion, cork stopper, fantasy RPG style",
                "fantasy", new[] { "medical", "potion", "healing" });
                
            AddMapping("vaccin_experimental", 
                "high-tech syringe with blue serum, futuristic medical device, clean white plastic",
                "sci-fi", new[] { "medical", "vaccine", "technology" });
            
            // OBJETS TECHNOLOGIQUES
            AddMapping("composant_electronique", 
                "futuristic electronic component with visible circuits, LED indicators, metallic housing",
                "cyberpunk", new[] { "electronics", "component", "technology" });
                
            AddMapping("carte_circuit", 
                "high-tech circuit board with glowing traces, microchips, green PCB color",
                "sci-fi", new[] { "electronics", "circuit", "computer" });
            
            // ARTEFACTS
            AddMapping("artefact_alien", 
                "ancient alien artifact with mysterious symbols, dark metal with purple glow",
                "alien", new[] { "artifact", "ancient", "alien" });
                
            AddMapping("relique_ancienne", 
                "ancient stone relic with carved symbols, weathered surface, archaeological find",
                "archaeological", new[] { "relic", "ancient", "stone" });
            
            // OBJETS INTERACTABLES
            AddMapping("terminal_recherche", 
                "futuristic research terminal with holographic display, sleek design, blue interface",
                "sci-fi", new[] { "terminal", "computer", "research" });
                
            AddMapping("console_securite", 
                "high-tech security console with multiple screens, red warning lights, metallic frame",
                "cyberpunk", new[] { "security", "console", "technology" });
            
            // RESSOURCES
            AddMapping("minerai_rare", 
                "rare mineral ore with crystal formations, metallic veins, geological specimen",
                "realistic", new[] { "mineral", "ore", "resource" });
                
            AddMapping("fragment_temporel", 
                "temporal fragment with time distortion effects, swirling energy, translucent crystal",
                "sci-fi", new[] { "temporal", "time", "fragment" });
            
            Debug.Log($"✅ {mappings.Count} mappings par défaut initialisés");
        }
        
        /// <summary>
        /// Ajoute un mapping facilement
        /// </summary>
        void AddMapping(string itemName, string prompt, string style, string[] tags)
        {
            AssetPromptMapping mapping = new AssetPromptMapping(itemName, prompt, style)
            {
                tags = tags,
                targetQuality = defaultQuality,
                targetTriangles = defaultTriangles
            };
            mappings.Add(mapping);
        }
        
        /// <summary>
        /// Trouve le mapping pour un nom d'objet
        /// </summary>
        public AssetPromptMapping GetMapping(string itemName)
        {
            return mappings.Find(m => m.itemName == itemName);
        }
        
        /// <summary>
        /// Génère un prompt visuel pour un nom d'objet
        /// </summary>
        public string GetVisualPrompt(string itemName)
        {
            AssetPromptMapping mapping = GetMapping(itemName);
            
            if (mapping != null)
            {
                return $"{mapping.visualPrompt}, {mapping.style}";
            }
            
            // Fallback automatique
            return GenerateAutoPrompt(itemName);
        }
        
        /// <summary>
        /// Génère automatiquement un prompt si pas de mapping trouvé
        /// </summary>
        string GenerateAutoPrompt(string itemName)
        {
            // Convertit nom technique → description lisible
            string humanReadable = itemName.Replace('_', ' ').ToLower();
            
            // Ajoute le style par défaut
            return $"{humanReadable}, {defaultStyle}";
        }
        
        /// <summary>
        /// Ajoute un nouveau mapping dynamiquement
        /// </summary>
        public void AddMapping(string itemName, string visualPrompt, string style = null)
        {
            // Retire l'ancien mapping s'il existe
            mappings.RemoveAll(m => m.itemName == itemName);
            
            // Ajoute le nouveau
            AssetPromptMapping newMapping = new AssetPromptMapping(
                itemName, 
                visualPrompt, 
                style ?? defaultStyle
            );
            
            mappings.Add(newMapping);
            
            Debug.Log($"📝 Mapping ajouté: {itemName} → {visualPrompt}");
        }
        
        /// <summary>
        /// Recherche des mappings par tags
        /// </summary>
        public List<AssetPromptMapping> FindByTag(string tag)
        {
            List<AssetPromptMapping> results = new List<AssetPromptMapping>();
            
            foreach (var mapping in mappings)
            {
                if (mapping.tags != null)
                {
                    foreach (string mappingTag in mapping.tags)
                    {
                        if (mappingTag.ToLower().Contains(tag.ToLower()))
                        {
                            results.Add(mapping);
                            break;
                        }
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Valide tous les mappings
        /// </summary>
        [ContextMenu("Validate All Mappings")]
        public void ValidateMappings()
        {
            int errors = 0;
            
            foreach (var mapping in mappings)
            {
                if (string.IsNullOrEmpty(mapping.itemName))
                {
                    Debug.LogError("❌ Mapping avec itemName vide trouvé");
                    errors++;
                }
                
                if (string.IsNullOrEmpty(mapping.visualPrompt))
                {
                    Debug.LogError($"❌ Mapping '{mapping.itemName}' sans prompt visuel");
                    errors++;
                }
                
                if (mapping.targetTriangles <= 0)
                {
                    Debug.LogWarning($"⚠️ Mapping '{mapping.itemName}' avec targetTriangles invalide: {mapping.targetTriangles}");
                }
            }
            
            if (errors == 0)
            {
                Debug.Log($"✅ Tous les {mappings.Count} mappings sont valides");
            }
            else
            {
                Debug.LogError($"❌ {errors} erreurs trouvées dans les mappings");
            }
        }
    }
    
    /// <summary>
    /// Preset de style pour cohérence artistique
    /// </summary>
    [System.Serializable]
    public class StylePreset
    {
        public string name;              // "Sci-Fi", "Fantasy", "Cyberpunk"
        public string baseStyle;         // Style de base pour ce preset
        public string colorPalette;      // Palette de couleurs suggérée
        public string lighting;          // Instructions d'éclairage
        public AssetQuality quality;     // Qualité par défaut
        public int triangleCount;        // Nombre de triangles suggéré
        
        public StylePreset(string name, string baseStyle)
        {
            this.name = name;
            this.baseStyle = baseStyle;
            this.quality = AssetQuality.Medium;
            this.triangleCount = 1000;
        }
        
        /// <summary>
        /// Génère le prompt complet avec ce style
        /// </summary>
        public string ApplyToPrompt(string basePrompt)
        {
            string styledPrompt = $"{basePrompt}, {baseStyle}";
            
            if (!string.IsNullOrEmpty(colorPalette))
                styledPrompt += $", {colorPalette}";
                
            if (!string.IsNullOrEmpty(lighting))
                styledPrompt += $", {lighting}";
                
            return styledPrompt;
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using DynamicAssets.Generation.API;
using DynamicAssets.Generation.Config;
using DynamicAssets.Core;

namespace DynamicAssets.Generation.API
{
    /// <summary>
    /// Convertit nos prompts simples en requêtes CSM optimisées
    /// Intègre avec votre système SimpleAssetMapping existant
    /// </summary>
    public static class CSMPromptConverter
    {
        #region Prompt Enhancement
        
        /// <summary>
        /// Convertit un nom d'objet technique en requête CSM via votre mapping existant
        /// </summary>
        public static CSMRequest ConvertItemNameToCSMRequest(string itemName, CSMConfig config)
        {
            Debug.Log($"🔍 Conversion item: '{itemName}' via SimpleAssetMapping");
            
            // Utilise votre système SimpleAssetMapping existant
            string visualPrompt = SimpleAssetMapping.GetVisualPrompt(itemName);
            
            // Enrichit le prompt avec les paramètres CSM
            string enhancedPrompt = EnhancePrompt(visualPrompt, config);
            
            // Crée la requête CSM
            var request = new CSMRequest(enhancedPrompt, itemName, config);
            
            Debug.Log($"✅ Requête depuis mapping: {itemName} → {enhancedPrompt}");
            return request;
        }
        
        /// <summary>
        /// Convertit un prompt simple en requête CSM complète
        /// </summary>
        public static CSMRequest ConvertToCSMRequest(string basicPrompt, string objectName, CSMConfig config)
        {
            Debug.Log($"🎨 Conversion prompt direct: '{basicPrompt}'");
            
            // Enrichissement du prompt de base
            string enhancedPrompt = EnhancePrompt(basicPrompt, config);
            
            // Crée la requête CSM
            var request = new CSMRequest(enhancedPrompt, objectName, config);
            
            Debug.Log($"✅ Requête CSM générée: {request.max_triangles} triangles, qualité '{request.quality}'");
            return request;
        }
        
        /// <summary>
        /// Enrichit un prompt basique avec des détails techniques optimisés
        /// </summary>
        private static string EnhancePrompt(string basicPrompt, CSMConfig config)
        {
            var promptParts = new List<string>();
            
            // Prompt de base (nettoyé)
            string cleanPrompt = CleanPrompt(basicPrompt);
            promptParts.Add(cleanPrompt);
            
            // Style cohérent depuis la config
            if (!string.IsNullOrEmpty(config.defaultStyle))
            {
                promptParts.Add(config.defaultStyle);
            }
            
            // Optimisations techniques automatiques
            promptParts.Add("optimized topology");
            promptParts.Add("game-ready mesh");
            promptParts.Add("centered pivot");
            promptParts.Add("normalized scale");
            
            // Contraintes de qualité basées sur la config
            if (config.maxTriangles > 0)
            {
                if (config.maxTriangles <= 500)
                    promptParts.Add("very low poly, simplified geometry");
                else if (config.maxTriangles <= 1000)
                    promptParts.Add("low poly, clean geometry");
                else if (config.maxTriangles <= 2000)
                    promptParts.Add("medium poly, detailed geometry");
                else
                    promptParts.Add("high poly, detailed geometry");
            }
            
            // Instructions de texture basées sur la config
            if (config.generateTextures)
            {
                if (config.textureResolution <= 512)
                    promptParts.Add("simple textures, clean UV mapping");
                else if (config.textureResolution <= 1024)
                    promptParts.Add("detailed textures, optimized UV layout");
                else
                    promptParts.Add("high resolution textures, professional UV mapping");
            }
            else
            {
                promptParts.Add("solid colors, no complex textures");
            }
            
            // Instructions de qualité selon la config
            switch (config.defaultQuality)
            {
                case ModelQuality.Low:
                    promptParts.Add("fast rendering, simplified details");
                    break;
                case ModelQuality.Medium:
                    promptParts.Add("balanced quality, good details");
                    break;
                case ModelQuality.High:
                    promptParts.Add("high quality, rich details");
                    break;
                case ModelQuality.Ultra:
                    promptParts.Add("ultra quality, maximum details");
                    break;
            }
            
            // Cohérence artistique pour jeu spatial
            promptParts.Add("consistent sci-fi aesthetic");
            promptParts.Add("neutral lighting");
            promptParts.Add("suitable for space game");
            
            // Contraintes techniques Unity
            promptParts.Add("Unity compatible");
            promptParts.Add("real-time rendering optimized");
            
            string enhanced = string.Join(", ", promptParts);
            Debug.Log($"📝 Prompt enrichi: '{enhanced}'");
            
            return enhanced;
        }
        
        /// <summary>
        /// Nettoie un prompt de base
        /// </summary>
        private static string CleanPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                return "generic game object";
            
            // Supprime les caractères problématiques
            string cleaned = prompt.Trim();
            cleaned = cleaned.Replace("\n", " ");
            cleaned = cleaned.Replace("\r", " ");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");
            
            return cleaned;
        }
        
        #endregion
        
        #region Seed Generation
        
        /// <summary>
        /// Génère un seed stable pour des prompts identiques (reproductibilité)
        /// </summary>
        public static int GenerateStableSeed(string itemName, string prompt)
        {
            // Combine le nom de l'item et le prompt pour un hash unique
            string combined = $"{itemName}_{prompt}";
            
            // Hash stable du prompt pour reproductibilité
            int hash = combined.GetHashCode();
            
            // Seed positif pour CSM
            int seed = Mathf.Abs(hash) % 999999;
            
            Debug.Log($"🎲 Seed généré: {seed} pour '{itemName}'");
            return seed;
        }
        
        #endregion
        
        #region Options Generation
        
        /// <summary>
        /// Configure les options spécifiques CSM selon la config
        /// </summary>
        public static Dictionary<string, object> GetGenerationOptions(CSMConfig config)
        {
            var options = new Dictionary<string, object>();
            
            // Options de base
            options["auto_optimize"] = true;
            options["generate_lod"] = config.generateLODs;
            options["include_textures"] = config.generateTextures;
            options["center_pivot"] = true;
            options["normalize_scale"] = true;
            
            // Options de qualité selon la config
            switch (config.defaultQuality)
            {
                case ModelQuality.Low:
                    options["render_time"] = "fast";
                    options["detail_level"] = "low";
                    options["optimization_level"] = "aggressive";
                    break;
                    
                case ModelQuality.Medium:
                    options["render_time"] = "balanced";
                    options["detail_level"] = "medium";
                    options["optimization_level"] = "balanced";
                    break;
                    
                case ModelQuality.High:
                    options["render_time"] = "quality";
                    options["detail_level"] = "high";
                    options["optimization_level"] = "minimal";
                    break;
                    
                case ModelQuality.Ultra:
                    options["render_time"] = "premium";
                    options["detail_level"] = "ultra";
                    options["optimization_level"] = "none";
                    break;
            }
            
            // Optimisations Unity spécifiques
            options["unity_compatible"] = true;
            options["realtime_rendering"] = true;
            options["mobile_compatible"] = true; // Au cas où
            
            // Format de sortie selon la config
            options["output_format"] = config.outputFormat.ToString().ToLower();
            options["texture_format"] = "unity_compatible";
            
            Debug.Log($"⚙️ Options CSM configurées: {options.Count} paramètres");
            return options;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Valide qu'une requête CSM est bien formée
        /// </summary>
        public static bool ValidateRequest(CSMRequest request)
        {
            if (request == null)
            {
                Debug.LogError("❌ Requête CSM null");
                return false;
            }
            
            if (string.IsNullOrEmpty(request.prompt))
            {
                Debug.LogError("❌ Requête CSM invalide: prompt vide");
                return false;
            }
            
            if (string.IsNullOrEmpty(request.object_name))
            {
                Debug.LogError("❌ Requête CSM invalide: nom d'objet manquant");
                return false;
            }
            
            if (request.max_triangles <= 0)
            {
                Debug.LogError("❌ Requête CSM invalide: nombre de triangles invalide");
                return false;
            }
            
            if (request.texture_resolution < 256 || request.texture_resolution > 2048)
            {
                Debug.LogError($"❌ Requête CSM invalide: résolution de texture {request.texture_resolution} hors limites (256-2048)");
                return false;
            }
            
            Debug.Log("✅ Requête CSM validée avec succès");
            return true;
        }
        
        #endregion
        
        #region Integration with Quest System
        
        /// <summary>
        /// Convertit un objet de quête en requête CSM
        /// Intégration spéciale avec votre système de quêtes
        /// </summary>
        public static CSMRequest ConvertQuestObjectToCSMRequest(string questObjectName, QuestObjectType objectType, CSMConfig config)
        {
            Debug.Log($"🎯 Conversion objet de quête: {questObjectName} ({objectType})");
            
            // Utilise votre mapping existant
            string basePrompt = SimpleAssetMapping.GetVisualPrompt(questObjectName);
            
            // Ajoute des instructions spécifiques selon le type d'objet de quête
            string questSpecificPrompt = AddQuestTypeInstructions(basePrompt, objectType);
            
            // Enrichit et crée la requête
            string enhancedPrompt = EnhancePrompt(questSpecificPrompt, config);
            var request = new CSMRequest(enhancedPrompt, questObjectName, config);
            
            Debug.Log($"✅ Requête pour objet de quête créée: {questObjectName}");
            return request;
        }
        
        /// <summary>
        /// Ajoute des instructions spécifiques selon le type d'objet de quête
        /// </summary>
        private static string AddQuestTypeInstructions(string basePrompt, QuestObjectType objectType)
        {
            switch (objectType)
            {
                case QuestObjectType.Item:
                    return $"{basePrompt}, collectible item, clear visual identity, pickupable object";
                    
                case QuestObjectType.InteractableObject:
                    return $"{basePrompt}, interactive terminal, clear interaction points, technological design";
                    
                case QuestObjectType.NPC:
                    return $"{basePrompt}, character model, humanoid design, approachable appearance";
                    
                case QuestObjectType.Marker:
                    return $"{basePrompt}, exploration marker, clear landmark, discoverable location";
                    
                default:
                    return basePrompt;
            }
        }
        
        #endregion
        
        #region Testing Methods
        
        /// <summary>
        /// Méthode de test pour valider le système
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void TestPromptConversion()
        {
            Debug.Log("🧪 Test CSMPromptConverter démarré");
            
            // Test avec un objet de votre mapping
            string testPrompt = SimpleAssetMapping.GetVisualPrompt("cristal_energie");
            Debug.Log($"📝 Prompt de test récupéré: {testPrompt}");
            
            // Note: On ne peut pas tester la conversion complète sans CSMConfig
            // Ce sera fait dans les tests suivants
        }
        
        #endregion
    }
}
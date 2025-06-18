using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Outil pour configurer la transparence des plantes/végétation
/// </summary>
public class PlantTransparencySetup : MonoBehaviour
{
    [Header("Configuration")]
    public Material[] plantMaterials;
    public Texture2D testTexture;
    
    [Header("Paramètres de transparence")]
    [Range(0f, 1f)]
    public float alphaCutoff = 0.5f;
    public bool useAlphaClipping = true;
    public bool doubleSided = true;
    
    [ContextMenu("Configurer les matériaux")]
    public void SetupMaterials()
    {
        foreach (var mat in plantMaterials)
        {
            if (mat != null)
            {
                ConfigurePlantMaterial(mat);
            }
        }
    }
    
    public static void ConfigurePlantMaterial(Material mat)
    {
        if (mat == null) return;
        
        // Pour URP Lit Shader
        if (mat.shader.name.Contains("Universal Render Pipeline/Lit"))
        {
            // Activer l'alpha clipping
            mat.SetFloat("_AlphaClip", 1);
            mat.SetFloat("_Cutoff", 0.5f);
            
            // Définir le mode de surface
            mat.SetFloat("_Surface", 0); // 0 = Opaque avec Alpha Clip
            
            // Double sided pour les feuilles
            mat.SetFloat("_Cull", 0); // 0 = Off (double sided)
            
            // Mots-clés shader
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            
            Debug.Log($"✅ Matériau {mat.name} configuré pour Alpha Cutout");
        }
        // Pour les shaders custom ou autres
        else
        {
            Debug.LogWarning($"⚠️ Shader non supporté : {mat.shader.name}");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlantTransparencySetup))]
public class PlantTransparencySetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        PlantTransparencySetup setup = (PlantTransparencySetup)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration Rapide", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Configurer tous les matériaux", GUILayout.Height(30)))
        {
            setup.SetupMaterials();
        }
        
        EditorGUILayout.Space();
        
        // Instructions
        EditorGUILayout.HelpBox(
            "Pour les textures de plantes avec fond noir :\n\n" +
            "1. Le fond noir doit être dans le canal Alpha\n" +
            "2. Utilisez Alpha Clipping (pas Transparent)\n" +
            "3. Ajustez le Cutoff pour contrôler la transparence\n" +
            "4. Activez Double Sided pour voir les deux côtés", 
            MessageType.Info);
        
        if (GUILayout.Button("Créer un matériau de plante"))
        {
            CreatePlantMaterial();
        }
    }
    
    void CreatePlantMaterial()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Sauvegarder le matériau", 
            "PlantMaterial", 
            "mat", 
            "Créer un nouveau matériau pour plante");
        
        if (!string.IsNullOrEmpty(path))
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            PlantTransparencySetup.ConfigurePlantMaterial(mat);
            
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = mat;
            EditorGUIUtility.PingObject(mat);
        }
    }
}

/// <summary>
/// Window pour convertir en masse les matériaux de végétation
/// </summary>
public class PlantMaterialConverterWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<Material> materials = new List<Material>();
    private bool convertFromBlackToAlpha = true;
    private float alphaCutoff = 0.5f;
    
    [MenuItem("Tools/Vegetation/Plant Material Converter")]
    public static void ShowWindow()
    {
        GetWindow<PlantMaterialConverterWindow>("Plant Material Converter");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Convertisseur de Matériaux de Végétation", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Options
        convertFromBlackToAlpha = EditorGUILayout.Toggle("Convertir noir → alpha", convertFromBlackToAlpha);
        alphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", alphaCutoff, 0f, 1f);
        
        EditorGUILayout.Space();
        
        // Drag & Drop zone
        EditorGUILayout.LabelField("Glissez vos matériaux ici :", EditorStyles.boldLabel);
        
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Glisser les matériaux ici");
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is Material)
                        {
                            materials.Add(draggedObject as Material);
                        }
                    }
                }
                break;
        }
        
        // Liste des matériaux
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Matériaux à convertir ({materials.Count}) :");
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        for (int i = materials.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            materials[i] = (Material)EditorGUILayout.ObjectField(materials[i], typeof(Material), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                materials.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        // Boutons d'action
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Vider la liste"))
        {
            materials.Clear();
        }
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Convertir tous", GUILayout.Height(30)))
        {
            ConvertAllMaterials();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Instructions détaillées
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Instructions pour les textures de plantes :\n\n" +
            "MÉTHODE 1 - Si votre texture a déjà un canal Alpha :\n" +
            "• Importez la texture\n" +
            "• Alpha Source = 'From Gray Scale' ou 'Input Texture Alpha'\n" +
            "• Alpha Is Transparency = ✓\n\n" +
            "MÉTHODE 2 - Si le fond est noir (pas d'alpha) :\n" +
            "• Utilisez un logiciel externe (Photoshop/GIMP)\n" +
            "• Sélectionnez le noir avec la baguette magique\n" +
            "• Supprimez ou créez un masque alpha\n" +
            "• Exportez en PNG avec transparence\n\n" +
            "MÉTHODE 3 - Utiliser Shader Graph (recommandé) :\n" +
            "• Créez un shader custom\n" +
            "• Utilisez le noir comme masque", 
            MessageType.Info);
    }
    
    void ConvertAllMaterials()
    {
        int converted = 0;
        
        foreach (var mat in materials)
        {
            if (mat != null)
            {
                // Configuration de base
                PlantTransparencySetup.ConfigurePlantMaterial(mat);
                
                // Configuration spécifique
                mat.SetFloat("_Cutoff", alphaCutoff);
                
                // Vérifier la texture
                if (mat.HasProperty("_BaseMap"))
                {
                    Texture tex = mat.GetTexture("_BaseMap");
                    if (tex != null)
                    {
                        ConfigureTextureImportSettings(tex);
                    }
                }
                
                EditorUtility.SetDirty(mat);
                converted++;
            }
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Conversion terminée", 
            $"{converted} matériaux convertis avec succès !", "OK");
    }
    
    void ConfigureTextureImportSettings(Texture texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer != null)
        {
            bool needsReimport = false;
            
            // Configurer pour la transparence
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }
            
            if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                needsReimport = true;
            }
            
            if (needsReimport)
            {
                importer.SaveAndReimport();
                Debug.Log($"✅ Texture {texture.name} reconfigurée pour la transparence");
            }
        }
    }
}
#endif

/// <summary>
/// Shader helper pour créer des shaders de végétation custom
/// </summary>
public static class VegetationShaderHelper
{
    public static string GenerateAlphaCutoutShader(string shaderName)
    {
        return $@"
Shader ""Vegetation/{shaderName}""
{{
    Properties
    {{
        _BaseMap (""Texture"", 2D) = ""white"" {{}}
        _Cutoff (""Alpha Cutoff"", Range(0,1)) = 0.5
        _WindStrength (""Wind Strength"", Range(0,1)) = 0.1
        _WindSpeed (""Wind Speed"", Range(0,10)) = 1
    }}
    
    SubShader
    {{
        Tags 
        {{ 
            ""RenderType""=""TransparentCutout"" 
            ""Queue""=""AlphaTest""
            ""RenderPipeline""=""UniversalPipeline""
        }}
        
        Pass
        {{
            Name ""ForwardLit""
            Tags {{ ""LightMode""=""UniversalForward"" }}
            
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl""
            
            struct Attributes
            {{
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            }};
            
            struct Varyings
            {{
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            }};
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Cutoff;
                float _WindStrength;
                float _WindSpeed;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {{
                Varyings output;
                
                // Animation vent simple
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float windPhase = _Time.y * _WindSpeed + worldPos.x * 0.5;
                float windOffset = sin(windPhase) * _WindStrength * input.uv.y;
                input.positionOS.x += windOffset;
                
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = worldPos;
                
                return output;
            }}
            
            half4 frag(Varyings input) : SV_Target
            {{
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // Alpha cutout
                clip(texColor.a - _Cutoff);
                
                // Éclairage simple
                Light mainLight = GetMainLight();
                half3 lighting = LightingLambert(mainLight.color, mainLight.direction, input.normalWS);
                
                return half4(texColor.rgb * lighting, 1);
            }}
            ENDHLSL
        }}
    }}
}}";
    }
}

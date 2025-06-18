using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// Simule une profondeur de champ pour les caméras orthographiques
/// </summary>
[RequireComponent(typeof(Camera))]
public class OrthographicDOF : MonoBehaviour
{
    [Header("Depth of Field Settings")]
    [Tooltip("Distance du plan focal depuis la caméra")]
    public float focalDistance = 10f;
    
    [Tooltip("Largeur de la zone nette")]
    public float focalRange = 5f;
    
    [Tooltip("Intensité du flou maximum")]
    [Range(0f, 10f)]
    public float maxBlurSize = 3f;
    
    [Tooltip("Transition douce du flou")]
    [Range(0.1f, 5f)]
    public float blurFalloff = 1f;
    
    [Header("Performance")]
    [Tooltip("Qualité du flou (plus bas = meilleures performances)")]
    [Range(1, 4)]
    public int blurIterations = 2;
    
    [Header("Debug")]
    public bool showFocalPlane = false;
    public Color focalPlaneColor = new Color(0, 1, 0, 0.1f);
    
    private Camera cam;
    private Material blurMaterial;
    
    // Shader pour le flou
    private const string BlurShader = @"
Shader ""Hidden/OrthographicDOF""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _BlurSize (""Blur Size"", Float) = 1.0
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            float _FocalDistance;
            float _FocalRange;
            float _BlurFalloff;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Échantillonnage de la profondeur
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = LinearEyeDepth(depth);
                
                // Calcul de la distance au plan focal
                float distanceFromFocal = abs(linearDepth - _FocalDistance);
                
                // Calcul du facteur de flou
                float blurFactor = saturate((distanceFromFocal - _FocalRange) / _BlurFalloff);
                blurFactor = blurFactor * blurFactor; // Courbe quadratique
                
                // Flou gaussien simple
                float2 texelSize = _MainTex_TexelSize.xy * _BlurSize * blurFactor;
                fixed4 color = tex2D(_MainTex, i.uv);
                
                // Échantillonnage en croix
                color += tex2D(_MainTex, i.uv + float2(texelSize.x, 0));
                color += tex2D(_MainTex, i.uv + float2(-texelSize.x, 0));
                color += tex2D(_MainTex, i.uv + float2(0, texelSize.y));
                color += tex2D(_MainTex, i.uv + float2(0, -texelSize.y));
                
                // Échantillonnage diagonal
                color += tex2D(_MainTex, i.uv + texelSize * 0.7);
                color += tex2D(_MainTex, i.uv - texelSize * 0.7);
                color += tex2D(_MainTex, i.uv + float2(texelSize.x, -texelSize.y) * 0.7);
                color += tex2D(_MainTex, i.uv + float2(-texelSize.x, texelSize.y) * 0.7);
                
                return color / 9.0;
            }
            ENDCG
        }
    }
}";
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Active la depth texture
        cam.depthTextureMode |= DepthTextureMode.Depth;
        
        // Crée le material pour le flou
        CreateBlurMaterial();
    }
    
    void CreateBlurMaterial()
    {
        if (blurMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/OrthographicDOF");
            if (shader == null)
            {
                // Créer le shader dynamiquement si non trouvé
                Debug.LogWarning("Shader OrthographicDOF non trouvé. Utilisation d'un fallback.");
                shader = Shader.Find("Hidden/BlitCopy");
            }
            
            if (shader != null)
            {
                blurMaterial = new Material(shader);
                blurMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (blurMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // Configure les paramètres du shader
        blurMaterial.SetFloat("_FocalDistance", focalDistance);
        blurMaterial.SetFloat("_FocalRange", focalRange);
        blurMaterial.SetFloat("_BlurFalloff", blurFalloff);
        
        // Applique le flou en plusieurs passes pour un meilleur résultat
        RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height);
        RenderTexture temp2 = RenderTexture.GetTemporary(source.width, source.height);
        
        Graphics.Blit(source, temp);
        
        for (int i = 0; i < blurIterations; i++)
        {
            float iterationBlur = (maxBlurSize / blurIterations) * (i + 1);
            blurMaterial.SetFloat("_BlurSize", iterationBlur);
            
            Graphics.Blit(temp, temp2, blurMaterial);
            
            // Swap buffers
            var swap = temp;
            temp = temp2;
            temp2 = swap;
        }
        
        Graphics.Blit(temp, destination);
        
        RenderTexture.ReleaseTemporary(temp);
        RenderTexture.ReleaseTemporary(temp2);
    }
    
    void OnDrawGizmos()
    {
        if (!showFocalPlane) return;
        
        Gizmos.color = focalPlaneColor;
        
        // Dessine le plan focal
        Vector3 center = transform.position + transform.forward * focalDistance;
        Vector3 size = new Vector3(100, 100, 0.1f);
        
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);
        
        // Dessine la zone de netteté
        Gizmos.color = new Color(focalPlaneColor.r, focalPlaneColor.g, focalPlaneColor.b, focalPlaneColor.a * 0.5f);
        Gizmos.DrawCube(Vector3.forward * focalRange, size);
        Gizmos.DrawCube(-Vector3.forward * focalRange, size);
    }
    
    void OnDestroy()
    {
        if (blurMaterial != null)
        {
            DestroyImmediate(blurMaterial);
        }
    }
}

/// <summary>
/// Version simplifiée utilisant les layers
/// </summary>
public class OrthographicDOFByLayers : MonoBehaviour
{
    [System.Serializable]
    public class LayerBlurSettings
    {
        public string layerName = "Background";
        public LayerMask layerMask;
        [Range(0f, 10f)]
        public float blurAmount = 3f;
        public Color tint = Color.white;
    }
    
    [Header("Layer-based DOF")]
    public LayerBlurSettings[] layerSettings = new LayerBlurSettings[]
    {
        new LayerBlurSettings { layerName = "Background", blurAmount = 5f },
        new LayerBlurSettings { layerName = "Midground", blurAmount = 2f },
        new LayerBlurSettings { layerName = "Foreground", blurAmount = 0f }
    };
    
    [Header("Camera Setup")]
    [Tooltip("Caméras supplémentaires pour chaque layer")]
    public bool autoCreateCameras = true;
    
    private Camera mainCamera;
    private List<Camera> layerCameras = new List<Camera>();
    
    void Start()
    {
        mainCamera = GetComponent<Camera>();
        
        if (autoCreateCameras)
        {
            SetupLayerCameras();
        }
    }
    
    void SetupLayerCameras()
    {
        // Crée une caméra par layer
        for (int i = 0; i < layerSettings.Length; i++)
        {
            GameObject camObj = new GameObject($"DOF_Camera_{layerSettings[i].layerName}");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            
            Camera layerCam = camObj.AddComponent<Camera>();
            layerCam.CopyFrom(mainCamera);
            layerCam.cullingMask = layerSettings[i].layerMask;
            layerCam.depth = mainCamera.depth - 1 - i;
            layerCam.clearFlags = i == 0 ? CameraClearFlags.SolidColor : CameraClearFlags.Depth;
            
            // Ajoute un effet de post-processing si nécessaire
            if (layerSettings[i].blurAmount > 0)
            {
                // Ici, vous pouvez ajouter votre effet de flou préféré
                // Par exemple, un Volume avec URP
            }
            
            layerCameras.Add(layerCam);
        }
        
        // Configure la caméra principale pour ne rien capturer
        mainCamera.cullingMask = 0;
        mainCamera.clearFlags = CameraClearFlags.Nothing;
    }
}

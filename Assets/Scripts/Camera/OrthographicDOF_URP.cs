using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

/// <summary>
/// Custom Depth of Field pour caméras orthographiques avec URP
/// </summary>
[Serializable, VolumeComponentMenu("Post-processing/Custom/Orthographic DOF")]
public class OrthographicDepthOfField : VolumeComponent, IPostProcessComponent
{
    [Header("Focus Settings")]
    public ClampedFloatParameter focalDistance = new ClampedFloatParameter(10f, 0.1f, 100f);
    public ClampedFloatParameter focalRange = new ClampedFloatParameter(5f, 0.1f, 50f);
    
    [Header("Blur Settings")]
    public ClampedFloatParameter maxBlur = new ClampedFloatParameter(1f, 0f, 5f);
    public ClampedIntParameter quality = new ClampedIntParameter(2, 1, 4);
    
    [Header("Advanced")]
    public BoolParameter useForegroundBlur = new BoolParameter(true);
    public ClampedFloatParameter foregroundBlurOffset = new ClampedFloatParameter(2f, 0f, 10f);
    
    public bool IsActive() => maxBlur.value > 0f;
    public bool IsTileCompatible() => false;
}

/// <summary>
/// Render Feature pour l'Orthographic DOF
/// </summary>
public class OrthographicDOFRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader;
    }
    
    public Settings settings = new Settings();
    private OrthographicDOFPass dofPass;
    
    public override void Create()
    {
        if (settings.shader == null)
        {
            Debug.LogError("Orthographic DOF Shader is missing!");
            return;
        }
        
        dofPass = new OrthographicDOFPass(settings);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.orthographic)
        {
            dofPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(dofPass);
        }
    }
    
    class OrthographicDOFPass : ScriptableRenderPass
    {
        private Settings settings;
        private Material material;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTexture;
        
        private static readonly int FocalDistanceID = Shader.PropertyToID("_FocalDistance");
        private static readonly int FocalRangeID = Shader.PropertyToID("_FocalRange");
        private static readonly int MaxBlurID = Shader.PropertyToID("_MaxBlur");
        
        public OrthographicDOFPass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
            
            if (settings.shader != null)
            {
                material = CoreUtils.CreateEngineMaterial(settings.shader);
            }
            
            tempTexture.Init("_TempBlurTexture");
        }
        
        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;
            
            var stack = VolumeManager.instance.stack;
            var dof = stack.GetComponent<OrthographicDepthOfField>();
            
            if (dof == null || !dof.IsActive()) return;
            
            CommandBuffer cmd = CommandBufferPool.Get("Orthographic DOF");
            
            // Set shader parameters
            material.SetFloat(FocalDistanceID, dof.focalDistance.value);
            material.SetFloat(FocalRangeID, dof.focalRange.value);
            material.SetFloat(MaxBlurID, dof.maxBlur.value);
            
            // Get temp RT
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            
            cmd.GetTemporaryRT(tempTexture.id, descriptor);
            
            // Apply blur
            for (int i = 0; i < dof.quality.value; i++)
            {
                cmd.Blit(source, tempTexture.Identifier(), material, 0);
                cmd.Blit(tempTexture.Identifier(), source, material, 1);
            }
            
            cmd.ReleaseTemporaryRT(tempTexture.id);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}

/// <summary>
/// Helper pour configurer rapidement l'Orthographic DOF
/// </summary>
public class OrthographicDOFController : MonoBehaviour
{
    [Header("Volume Reference")]
    public Volume postProcessVolume;
    
    [Header("Auto Focus")]
    public bool autoFocus = false;
    public Transform focusTarget;
    public float focusSpeed = 2f;
    
    [Header("Dynamic Settings")]
    public bool dynamicBlur = false;
    public AnimationCurve blurCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float blurDistanceMax = 50f;
    
    private OrthographicDepthOfField dofComponent;
    private Camera cam;
    
    void Start()
    {
        cam = Camera.main;
        
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out dofComponent);
        }
        
        if (dofComponent == null)
        {
            Debug.LogWarning("Orthographic DOF component not found in Volume!");
        }
    }
    
    void Update()
    {
        if (dofComponent == null) return;
        
        // Auto focus
        if (autoFocus && focusTarget != null)
        {
            float targetDistance = Vector3.Distance(cam.transform.position, focusTarget.position);
            float currentDistance = dofComponent.focalDistance.value;
            
            dofComponent.focalDistance.value = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * focusSpeed);
        }
        
        // Dynamic blur based on movement or distance
        if (dynamicBlur && focusTarget != null)
        {
            float distance = Vector3.Distance(cam.transform.position, focusTarget.position);
            float normalizedDistance = Mathf.Clamp01(distance / blurDistanceMax);
            float blurAmount = blurCurve.Evaluate(normalizedDistance);
            
            dofComponent.maxBlur.value = blurAmount * dofComponent.maxBlur.max;
        }
    }
    
    /// <summary>
    /// Focus sur une position spécifique
    /// </summary>
    public void FocusOn(Vector3 worldPosition, float duration = 1f)
    {
        if (dofComponent == null) return;
        
        float distance = Vector3.Distance(cam.transform.position, worldPosition);
        StartCoroutine(AnimateFocus(distance, duration));
    }
    
    System.Collections.IEnumerator AnimateFocus(float targetDistance, float duration)
    {
        float startDistance = dofComponent.focalDistance.value;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            dofComponent.focalDistance.value = Mathf.Lerp(startDistance, targetDistance, t);
            
            yield return null;
        }
    }
}

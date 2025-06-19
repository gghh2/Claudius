using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Debug helper to check URP Fog configuration
/// </summary>
public class URPFogChecker : MonoBehaviour
{
    [ContextMenu("Check URP Fog Setup")]
    void CheckFogSetup()
    {
        Debug.Log("=== URP FOG CHECKER ===");
        
        // Check if we have URP
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null)
        {
            Debug.LogError("❌ No Render Pipeline Asset assigned!");
            return;
        }
        
        Debug.Log($"✅ Current Pipeline: {pipeline.name}");
        
        // Check if it's URP
        if (pipeline is UniversalRenderPipelineAsset urpAsset)
        {
            Debug.Log($"✅ URP Version: {Application.unityVersion}");
            Debug.Log($"✅ URP Asset: {urpAsset.name}");
            
            // For URP 12+, the renderer data is accessed differently
            // We'll use reflection to be version-agnostic
            var rendererDataList = urpAsset.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (rendererDataList != null)
            {
                var dataList = rendererDataList.GetValue(urpAsset) as ScriptableRendererData[];
                if (dataList != null && dataList.Length > 0)
                {
                    Debug.Log($"✅ Found {dataList.Length} renderer(s)");
                    foreach (var data in dataList)
                    {
                        if (data != null)
                        {
                            Debug.Log($"  - Renderer: {data.name}");
                        }
                    }
                }
            }
        }
        
        // Check current volume
        var volume = GetComponent<Volume>();
        if (volume != null && volume.profile != null)
        {
            Debug.Log($"✅ Volume Profile: {volume.profile.name}");
            
            // List all overrides
            Debug.Log("Available Overrides in this profile:");
            foreach (var component in volume.profile.components)
            {
                Debug.Log($"  - {component.GetType().Name}");
            }
            
            // Check specifically for Fog-related components
            var fogComponent = volume.profile.components.FirstOrDefault(c => c.GetType().Name.Contains("Fog"));
            if (fogComponent != null)
            {
                Debug.Log($"✅ FOG COMPONENT FOUND: {fogComponent.GetType().Name}");
                
                // Use reflection to check if it's enabled
                var enabledProperty = fogComponent.GetType().GetProperty("enabled");
                if (enabledProperty != null)
                {
                    var paramValue = enabledProperty.GetValue(fogComponent);
                    var valueProperty = paramValue?.GetType().GetProperty("value");
                    if (valueProperty != null)
                    {
                        bool isEnabled = (bool)valueProperty.GetValue(paramValue);
                        Debug.Log($"  - Fog Enabled: {isEnabled}");
                    }
                }
            }
            else
            {
                Debug.Log("⚠️ No Fog component found in this volume profile");
                Debug.Log("💡 Try adding it via: Add Override → Fog (or search for 'fog')");
            }
        }
        
        Debug.Log("\n💡 If you found 'Fog' in overrides, you're all set!");
        Debug.Log("If not, make sure you're looking in the right categories.");
    }
}
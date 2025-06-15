using UnityEngine;

/// <summary>
/// Script de test pour comprendre les projections de caméra
/// </summary>
public class CameraProjectionTest : MonoBehaviour
{
    public Transform testTarget;
    
    void Update()
    {
        if (testTarget == null || Camera.main == null) return;
        
        if (Input.GetKeyDown(KeyCode.P)) // P pour Projection test
        {
            TestProjection();
        }
    }
    
    void TestProjection()
    {
        Camera cam = Camera.main;
        Vector3 targetPos = testTarget.position;
        
        Debug.Log($"=== TEST PROJECTION ===");
        Debug.Log($"Camera: {(cam.orthographic ? "ORTHOGRAPHIC" : "PERSPECTIVE")}");
        Debug.Log($"Target World Pos: {targetPos}");
        
        // Test ViewportPoint
        Vector3 viewportPos = cam.WorldToViewportPoint(targetPos);
        Debug.Log($"ViewportPoint: X={viewportPos.x:F2}, Y={viewportPos.y:F2}, Z={viewportPos.z:F2}");
        
        // Test ScreenPoint
        Vector3 screenPos = cam.WorldToScreenPoint(targetPos);
        Debug.Log($"ScreenPoint: X={screenPos.x:F0}, Y={screenPos.y:F0}, Z={screenPos.z:F2}");
        
        // Interprétation
        if (cam.orthographic)
        {
            Debug.Log($"→ En Ortho: Z={screenPos.z} (positif = dans le frustum)");
        }
        else
        {
            Debug.Log($"→ En Perspective: Z={viewportPos.z} (négatif = derrière)");
        }
    }
    
    void OnDrawGizmos()
    {
        if (testTarget == null) return;
        
        // Dessine une ligne vers la cible
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, testTarget.position);
        Gizmos.DrawWireSphere(testTarget.position, 0.5f);
    }
}

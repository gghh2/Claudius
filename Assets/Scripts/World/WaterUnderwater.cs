using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detecte quand la camera passe sous la surface du bloc d'eau et applique
/// un effet underwater : overlay couleur (teinte verdatre/bleue) + fog
/// dense pour reduire la visibilite. Sortie : tout est restaure.
///
/// A poser dans la scene sur n'importe quel GameObject (typiquement un
/// manager). Le composant trouve sa surface d'eau et sa camera tout seul.
/// </summary>
public class WaterUnderwater : MonoBehaviour
{
    [Header("Surface de l'eau")]
    [Tooltip("Le bloc d'eau dont on surveille la surface (auto-trouve si null par le nom 'WaterBlock').")]
    public Renderer waterRenderer;

    [Tooltip("Offset Y additionnel par rapport a bounds.max.y. " +
        "Positif si tu veux que l'effet se declenche un peu avant l'immersion (vagues).")]
    public float surfaceOffset = -0.05f;

    [Header("Camera")]
    [Tooltip("Camera a surveiller (auto-trouve via Camera.main si null).")]
    public Camera trackedCamera;

    [Header("Overlay")]
    [Tooltip("Couleur de l'overlay underwater (alpha = intensite).")]
    public Color underwaterTint = new Color(0.1f, 0.35f, 0.45f, 0.55f);

    [Tooltip("Layer d'affichage de l'overlay — au-dessus du monde, sous les UIs critiques.")]
    public int overlayCanvasSortingOrder = 4000;

    [Header("Fog underwater")]
    public bool overrideFog = true;
    public Color underwaterFogColor = new Color(0.08f, 0.25f, 0.32f);
    public float underwaterFogDensity = 0.12f;

    Image overlayImage;
    Canvas overlayCanvas;
    bool isUnderwater;
    float waterSurfaceY;

    // Sauvegarde de l'etat fog initial pour pouvoir le restaurer.
    bool savedFogEnabled;
    Color savedFogColor;
    float savedFogDensity;
    FogMode savedFogMode;
    bool fogStateCaptured;

    void Awake()
    {
        if (waterRenderer == null) AutoFindWater();
        if (trackedCamera == null) trackedCamera = Camera.main;
        BuildOverlay();
        CaptureFogState();
        ComputeSurfaceY();
    }

    void AutoFindWater()
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("waterblock") || n.Contains("water_block") || n == "water")
            {
                waterRenderer = r;
                return;
            }
        }
    }

    void ComputeSurfaceY()
    {
        if (waterRenderer == null) { waterSurfaceY = 0f; return; }
        waterSurfaceY = waterRenderer.bounds.max.y + surfaceOffset;
    }

    void CaptureFogState()
    {
        savedFogEnabled = RenderSettings.fog;
        savedFogColor = RenderSettings.fogColor;
        savedFogDensity = RenderSettings.fogDensity;
        savedFogMode = RenderSettings.fogMode;
        fogStateCaptured = true;
    }

    void BuildOverlay()
    {
        var canvasGo = new GameObject("WaterUnderwaterOverlay");
        canvasGo.transform.SetParent(transform, false);
        overlayCanvas = canvasGo.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = overlayCanvasSortingOrder;
        canvasGo.AddComponent<CanvasScaler>();
        // Pas de GraphicRaycaster : l'overlay ne doit pas bloquer les clics.

        var imgGo = new GameObject("Tint");
        imgGo.transform.SetParent(canvasGo.transform, false);
        overlayImage = imgGo.AddComponent<Image>();
        overlayImage.color = underwaterTint;
        overlayImage.raycastTarget = false;

        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlayCanvas.enabled = false;
    }

    void Update()
    {
        if (trackedCamera == null) trackedCamera = Camera.main;
        if (trackedCamera == null || waterRenderer == null) return;

        // Recalcule la surface chaque frame si la mer bouge (vagues, level
        // edit). Cout negligeable.
        ComputeSurfaceY();

        bool nowUnderwater = trackedCamera.transform.position.y < waterSurfaceY;
        if (nowUnderwater != isUnderwater) ApplyState(nowUnderwater);
    }

    void ApplyState(bool underwater)
    {
        isUnderwater = underwater;
        if (overlayCanvas != null) overlayCanvas.enabled = underwater;
        if (overlayImage != null) overlayImage.color = underwaterTint;

        if (overrideFog && fogStateCaptured)
        {
            if (underwater)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = underwaterFogColor;
                RenderSettings.fogDensity = underwaterFogDensity;
            }
            else
            {
                RenderSettings.fog = savedFogEnabled;
                RenderSettings.fogMode = savedFogMode;
                RenderSettings.fogColor = savedFogColor;
                RenderSettings.fogDensity = savedFogDensity;
            }
        }
    }

    void OnDisable()
    {
        // Restaure tout pour eviter de laisser le fog underwater bloque en
        // edit mode si le composant est desactive.
        if (overrideFog && fogStateCaptured)
        {
            RenderSettings.fog = savedFogEnabled;
            RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogDensity = savedFogDensity;
        }
        if (overlayCanvas != null) overlayCanvas.enabled = false;
        isUnderwater = false;
    }
}

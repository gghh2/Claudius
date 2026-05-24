using UnityEngine;

/// <summary>
/// Cycle jour/nuit visuel : oriente le soleil, ajuste sa couleur et son
/// intensité, gère une lune nocturne, teinte la skybox. Adapté depuis le
/// TimeManager du projet Kingdoms (HDRP) pour URP — la principale différence
/// est l'échelle d'intensité des Light (Lux HDRP ≫ valeurs URP).
///
/// Source de vérité du temps : <see cref="GameClock"/>. Ce composant n'avance
/// pas son propre temps — il lit l'heure courante et applique le visuel.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Sun")]
    [Tooltip("La Directional Light qui sert de soleil. Auto-trouvée si null.")]
    public Light sunLight;
    [Tooltip("Couleur du soleil au fil de la journée (0 = minuit, 0.5 = midi, 1 = minuit).")]
    public Gradient lightColorGradient;
    [Tooltip("Courbe d'intensité (URP : valeurs typiques 0-2.5).")]
    public AnimationCurve lightIntensityCurve;
    [Tooltip("Intensité maximale (midi). URP : 1.5-2.5 selon le rendu voulu.")]
    public float maxLightIntensity = 1.8f;
    [Tooltip("Intensité minimale (cœur de nuit). URP : 0.05-0.15.")]
    public float minNightIntensity = 0.08f;

    [Header("Moon")]
    [Tooltip("Light optionnelle pour la lune. Auto-créée si null.")]
    public Light moonLight;
    [Tooltip("Couleur de la lumière lunaire (bleu froid).")]
    public Color moonColor = new Color(0.5f, 0.6f, 0.85f, 1f);
    [Tooltip("Intensité max de la lune au cœur de nuit (URP : 0.15-0.4).")]
    public float moonMaxIntensity = 0.25f;

    [Header("Skybox")]
    [Tooltip("Matériau de skybox dont on tinte _Tint (optionnel — peut casser " +
        "un skybox HDRI sans propriété _Tint, dans ce cas laisser null).")]
    public Material skyboxMaterial;
    public Gradient skyColorGradient;
    [Tooltip("Nom de la propriété de teinte sur le matériau (varie selon shader).")]
    public string skyboxTintProperty = "_Tint";

    [Header("Sun Rotation")]
    [Tooltip("Angle Y du soleil (orientation horizontale). 170 = direction par défaut Kingdoms.")]
    public float sunYAngle = 170f;

    public bool IsDaytime
    {
        get
        {
            if (GameClock.Instance == null) return true;
            int h = GameClock.Instance.Hour;
            return h >= 6 && h < 18;
        }
    }

    void Start()
    {
        if (sunLight == null) sunLight = FindMainDirectionalLight();
        if (moonLight == null) moonLight = CreateMoonLight();
        InitializeDefaultGradients();
    }

    void Update()
    {
        if (GameClock.Instance == null) return;
        ApplyLighting();
    }

    void ApplyLighting()
    {
        float t = (GameClock.Instance.Hour + GameClock.Instance.Minute / 60f) / 24f;

        if (sunLight != null)
        {
            // Soleil : 0 = minuit (orienté vers le bas), 0.25 = lever, 0.5 = zénith.
            float sunRot = t * 360f - 90f;
            sunLight.transform.rotation = Quaternion.Euler(sunRot, sunYAngle, 0f);

            if (lightColorGradient != null)
                sunLight.color = lightColorGradient.Evaluate(t);

            if (lightIntensityCurve != null)
            {
                float v = lightIntensityCurve.Evaluate(t) * maxLightIntensity;
                sunLight.intensity = Mathf.Max(v, minNightIntensity);
            }
        }

        UpdateMoon(t);
        UpdateSkybox(t);
    }

    void UpdateMoon(float t)
    {
        if (moonLight == null) return;

        bool night = !IsDaytime;
        moonLight.enabled = night;
        if (!night) return;

        // Lune à l'opposé du soleil (décalage de 12h).
        float moonRot = ((GameClock.Instance.Hour + GameClock.Instance.Minute / 60f + 12f) / 24f) * 360f - 90f;
        moonLight.transform.rotation = Quaternion.Euler(moonRot, sunYAngle, 0f);

        // Intensité : maximale à minuit, atténuée aux bords de la nuit.
        float h = GameClock.Instance.Hour + GameClock.Instance.Minute / 60f;
        float nightProgress = h < 6f ? 1f - (h / 6f) : (h - 18f) / 6f;
        moonLight.intensity = Mathf.Lerp(moonMaxIntensity * 0.25f, moonMaxIntensity, nightProgress);
        moonLight.color = moonColor;
    }

    void UpdateSkybox(float t)
    {
        if (skyboxMaterial == null || skyColorGradient == null) return;
        if (!skyboxMaterial.HasProperty(skyboxTintProperty)) return;
        skyboxMaterial.SetColor(skyboxTintProperty, skyColorGradient.Evaluate(t));
    }

    Light FindMainDirectionalLight()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional && l.name != "MoonLight")
                return l;
        }
        return null;
    }

    Light CreateMoonLight()
    {
        // Réutilise une MoonLight existante si présente en scène.
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.name == "MoonLight") return l;
        }

        GameObject go = new GameObject("MoonLight");
        go.transform.SetParent(transform);
        Light moon = go.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = moonColor;
        moon.intensity = 0f;
        moon.shadows = LightShadows.None;
        moon.transform.rotation = Quaternion.Euler(90f, sunYAngle, 0f);
        return moon;
    }

    void InitializeDefaultGradients()
    {
        if (lightColorGradient == null || lightColorGradient.colorKeys.Length == 0)
        {
            lightColorGradient = new Gradient();
            var c = new GradientColorKey[5];
            c[0] = new GradientColorKey(new Color(0.30f, 0.30f, 0.50f), 0f);   // Minuit
            c[1] = new GradientColorKey(new Color(1.00f, 0.60f, 0.40f), 0.25f); // Aube
            c[2] = new GradientColorKey(new Color(1.00f, 0.96f, 0.90f), 0.5f);  // Midi
            c[3] = new GradientColorKey(new Color(1.00f, 0.50f, 0.30f), 0.75f); // Crépuscule
            c[4] = new GradientColorKey(new Color(0.30f, 0.30f, 0.50f), 1f);    // Retour minuit
            var a = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
            lightColorGradient.SetKeys(c, a);
        }

        if (lightIntensityCurve == null || lightIntensityCurve.length == 0)
        {
            // Valeurs adaptées URP : pic à 1 (multiplié par maxLightIntensity).
            lightIntensityCurve = new AnimationCurve();
            lightIntensityCurve.AddKey(0f, 0.05f);   // Minuit
            lightIntensityCurve.AddKey(0.25f, 0.6f); // Aube
            lightIntensityCurve.AddKey(0.5f, 1.0f);  // Midi
            lightIntensityCurve.AddKey(0.75f, 0.6f); // Crépuscule
            lightIntensityCurve.AddKey(1f, 0.05f);   // Minuit
        }

        if (skyColorGradient == null || skyColorGradient.colorKeys.Length == 0)
        {
            skyColorGradient = new Gradient();
            var c = new GradientColorKey[4];
            c[0] = new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 0f);
            c[1] = new GradientColorKey(new Color(0.80f, 0.50f, 0.30f), 0.25f);
            c[2] = new GradientColorKey(new Color(0.50f, 0.70f, 1.00f), 0.5f);
            c[3] = new GradientColorKey(new Color(0.90f, 0.40f, 0.20f), 0.75f);
            var a = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
            skyColorGradient.SetKeys(c, a);
        }
    }
}

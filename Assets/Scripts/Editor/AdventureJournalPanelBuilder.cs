#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Outil éditeur : génère le panneau "Adventure Journal" dans la scène Game,
/// attache et câble <see cref="AdventureJournalUI"/>, et l'enregistre auprès de
/// UnifiedUIManager.
///
/// Menu : Tools > Claudius > Create Adventure Journal Panel
///
/// Le panneau produit est fonctionnel mais volontairement sobre — à restyler
/// ensuite dans l'éditeur (couleurs, polices, sprites).
/// </summary>
public static class AdventureJournalPanelBuilder
{
    const string PanelName = "AdventureJournalPanel";

    [MenuItem("Tools/Claudius/Create Adventure Journal Panel")]
    public static void CreatePanel()
    {
        // --- 1. Localiser le GameObject "UI" et son Canvas ---
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot == null)
        {
            EditorUtility.DisplayDialog("Adventure Journal",
                "GameObject 'UI' introuvable. Ouvre la scène Game avant de lancer l'outil.", "OK");
            return;
        }

        Canvas canvas = uiRoot.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Adventure Journal",
                "Aucun Canvas trouvé sous 'UI'.", "OK");
            return;
        }

        // --- 2. Éviter les doublons ---
        Transform existing = canvas.transform.Find(PanelName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Adventure Journal",
                $"'{PanelName}' existe déjà. Le supprimer et le recréer ?", "Recréer", "Annuler"))
                return;
            Object.DestroyImmediate(existing.gameObject);
        }

        // --- 3. Construire le panneau ---
        GameObject panel = NewUI(PanelName, canvas.transform);
        Stretch(panel);
        var panelCanvas = panel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 20; // pauseMenuLayer
        panel.AddComponent<GraphicRaycaster>();
        panel.AddComponent<AdventureJournalPanelHelper>();
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        // Titre
        GameObject titleGO = NewUI("Title", panel.transform);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -20f);
        titleRT.sizeDelta = new Vector2(-40f, 60f);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "Journal de Bord";
        title.fontSize = 32;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        // ScrollView
        GameObject scrollGO = NewUI("ScrollView", panel.transform);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(30f, 90f);
        scrollRT.offsetMax = new Vector2(-30f, -90f);
        var scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.35f);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();

        GameObject viewport = NewUI("Viewport", scrollGO.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();

        GameObject content = NewUI("Content", viewport.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var contentText = content.AddComponent<TextMeshProUGUI>();
        contentText.text = "";
        contentText.fontSize = 18;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.margin = new Vector4(12f, 12f, 12f, 12f);

        scrollRect.content = contentRT;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        // Indicateur de chargement
        GameObject loadingGO = NewUI("LoadingIndicator", panel.transform);
        var loadingRT = loadingGO.GetComponent<RectTransform>();
        loadingRT.anchorMin = new Vector2(0.5f, 0.5f);
        loadingRT.anchorMax = new Vector2(0.5f, 0.5f);
        loadingRT.sizeDelta = new Vector2(400f, 50f);
        loadingRT.anchoredPosition = Vector2.zero;
        var loadingText = loadingGO.AddComponent<TextMeshProUGUI>();
        loadingText.text = "L'IA rédige votre journal...";
        loadingText.fontSize = 20;
        loadingText.fontStyle = FontStyles.Italic;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingGO.SetActive(false);

        // Bouton Fermer, centré (la narration IA est automatique : pas de bouton Rafraîchir)
        Button closeBtn = MakeButton("CloseButton", panel.transform, "Fermer",
            new Vector2(0.5f, 0f), new Vector2(0f, 32f));

        // --- 4. AdventureJournalUI sur le GameObject "UI" ---
        var journal = uiRoot.GetComponent<AdventureJournalUI>();
        if (journal == null) journal = uiRoot.AddComponent<AdventureJournalUI>();
        journal.journalPanel = panel;
        journal.journalTitle = title;
        journal.journalContent = contentText;
        journal.loadingIndicator = loadingGO;
        journal.closeButton = closeBtn;
        journal.scrollRect = scrollRect;

        // --- 5. Enregistrer le panneau auprès de UnifiedUIManager ---
        var managers = Object.FindObjectsByType<UnifiedUIManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length > 0)
        {
            var so = new SerializedObject(managers[0]);
            var prop = so.FindProperty("adventureJournalPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = panel;
                so.ApplyModifiedProperties();
            }
        }
        else
        {
            Debug.LogWarning("[AdventureJournalPanelBuilder] UnifiedUIManager introuvable — " +
                "assigne le champ 'adventureJournalPanel' à la main dans l'inspecteur.");
        }

        // --- 6. Finaliser ---
        panel.SetActive(false);
        EditorUtility.SetDirty(uiRoot);
        EditorSceneManager.MarkSceneDirty(uiRoot.scene);
        Selection.activeGameObject = panel;
        Debug.Log("[AdventureJournalPanelBuilder] Panneau '" + PanelName +
            "' créé et câblé. Sauvegarde la scène (Ctrl+S).");
    }

    // --- Helpers ---

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Button MakeButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pos)
    {
        GameObject go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(200f, 52f);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.20f, 0.28f, 1f);
        var btn = go.AddComponent<Button>();

        GameObject txtGO = NewUI("Text", go.transform);
        Stretch(txtGO);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 20;
        txt.alignment = TextAlignmentOptions.Center;
        return btn;
    }
}
#endif

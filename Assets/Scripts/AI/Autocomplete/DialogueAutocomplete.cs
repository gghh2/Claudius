using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Autocompletion du champ de dialogue : quand le joueur tape un mot (>= 3
/// lettres) qui prefixe un nom familier au jeu (item possede, zone
/// decouverte, PNJ rencontre, note lue), un bandeau apparait au-dessus de
/// l'input field proposant la completion. TAB la valide en remplacant le
/// mot partiel par le nom complet et formate.
///
/// Sources : <see cref="KnownReferenceProvider"/>.
///
/// Le composant s'auto-attache au DialogueUI au demarrage et cree son
/// propre bandeau UI ; aucun cablage manuel en scene n'est requis.
/// </summary>
public class DialogueAutocomplete : MonoBehaviour
{
    [Tooltip("Nombre minimum de lettres tapees avant de proposer une completion.")]
    public int minPrefixLength = 3;

    [Tooltip("Couleur du texte du bandeau.")]
    public Color bannerColor = new Color(0.85f, 0.85f, 0.55f, 0.9f);

    TMP_InputField input;
    TextMeshProUGUI banner;
    string currentSuggestion;       // Nom complet formate, ou null.
    string currentPartialWord;      // Mot partiel tape par le joueur.
    int currentPartialStart;        // Position du debut du mot partiel dans input.text.
    int lastCaretPos = -1;          // Pour detecter les deplacements de caret sans onValueChanged.

    void Start()
    {
        if (DialogueUI.Instance == null) { enabled = false; return; }
        input = DialogueUI.Instance.playerInputField;
        if (input == null) { enabled = false; return; }

        BuildBanner();
        input.onValueChanged.AddListener(OnInputChanged);
        HideBanner();
    }

    void BuildBanner()
    {
        // Le bandeau est cree comme frere de l'input field, ancre juste
        // au-dessus, sur toute la largeur. Pas de raycast (juste decoratif).
        var parent = input.transform.parent;
        var go = new GameObject("AutocompleteBanner");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        var inputRt = input.GetComponent<RectTransform>();
        // Ancrage : meme x que l'input, juste au-dessus.
        rt.anchorMin = inputRt.anchorMin;
        rt.anchorMax = inputRt.anchorMax;
        rt.pivot = new Vector2(inputRt.pivot.x, 0f);
        rt.anchoredPosition = inputRt.anchoredPosition + new Vector2(0f, inputRt.rect.height * 0.5f + 4f);
        rt.sizeDelta = new Vector2(inputRt.sizeDelta.x, 22f);

        banner = go.AddComponent<TextMeshProUGUI>();
        banner.fontSize = 14f;
        banner.color = bannerColor;
        banner.alignment = TextAlignmentOptions.MidlineLeft;
        banner.raycastTarget = false;
        banner.text = "";
    }

    void OnInputChanged(string text)
    {
        ComputeSuggestion(text, input.caretPosition);
    }

    void Update()
    {
        if (input == null || !input.gameObject.activeInHierarchy) { HideBanner(); return; }

        // TAB : si on a une suggestion, on l'applique.
        if (Input.GetKeyDown(KeyCode.Tab) && currentSuggestion != null)
        {
            ApplySuggestion();
            return;
        }

        // Le caret peut bouger sans onValueChanged (fleches gauche/droite).
        // On ne recalcule que si la position a change pour eviter de
        // re-scanner toutes les references chaque frame.
        if (input.isFocused && input.caretPosition != lastCaretPos)
        {
            lastCaretPos = input.caretPosition;
            ComputeSuggestion(input.text, input.caretPosition);
        }
    }

    void ComputeSuggestion(string text, int caretPos)
    {
        if (string.IsNullOrEmpty(text)) { ClearSuggestion(); return; }

        // Le "mot courant" = depuis le dernier espace/debut jusqu'au caret.
        int end = Mathf.Clamp(caretPos, 0, text.Length);
        int start = end;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;

        if (end - start < minPrefixLength) { ClearSuggestion(); return; }
        // Si le caret est suivi de texte non-whitespace, on est au milieu d'un
        // mot deja complet — pas d'autocomplete.
        if (end < text.Length && !char.IsWhiteSpace(text[end])) { ClearSuggestion(); return; }

        string partial = text.Substring(start, end - start);
        string match = KnownReferenceProvider.FindMatch(partial);

        // La suggestion doit etre strictement plus longue que le prefix tape
        // (sinon completer "lettre" par "lettre" est inutile).
        if (match == null || match.Length <= partial.Length || !match.ToLowerInvariant().StartsWith(partial.ToLowerInvariant()))
        {
            ClearSuggestion();
            return;
        }

        currentSuggestion = match;
        currentPartialWord = partial;
        currentPartialStart = start;
        ShowBanner($"<size=12>[TAB]</size>  {match}");
    }

    void ApplySuggestion()
    {
        if (currentSuggestion == null || input == null) return;

        string before = input.text.Substring(0, currentPartialStart);
        int afterStart = currentPartialStart + currentPartialWord.Length;
        string after = (afterStart <= input.text.Length) ? input.text.Substring(afterStart) : "";

        string replaced = before + currentSuggestion + after;
        int newCaret = (before + currentSuggestion).Length;

        input.text = replaced;
        input.caretPosition = newCaret;
        input.selectionAnchorPosition = newCaret;
        input.selectionFocusPosition = newCaret;

        ClearSuggestion();
        input.ActivateInputField();
    }

    void ShowBanner(string text)
    {
        if (banner == null) return;
        banner.text = text;
        banner.gameObject.SetActive(true);
    }

    void HideBanner()
    {
        if (banner == null) return;
        banner.text = "";
        banner.gameObject.SetActive(false);
    }

    void ClearSuggestion()
    {
        currentSuggestion = null;
        currentPartialWord = null;
        HideBanner();
    }

    void OnDestroy()
    {
        if (input != null) input.onValueChanged.RemoveListener(OnInputChanged);
    }
}

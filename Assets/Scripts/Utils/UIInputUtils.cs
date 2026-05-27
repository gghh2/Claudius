using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class UIInputUtils
{
    /// <summary>
    /// True si l'utilisateur est en train de taper dans un champ de saisie
    /// (TMP_InputField ou InputField legacy). Les raccourcis clavier globaux
    /// (B boutique, ² console, I/J/L panneaux...) doivent bail tot quand
    /// c'est le cas, sinon une frappe normale declenche l'action.
    /// </summary>
    public static bool IsTypingInInputField()
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var go = es.currentSelectedGameObject;
        if (go == null) return false;
        return go.GetComponent<TMP_InputField>() != null
            || go.GetComponent<InputField>() != null;
    }
}

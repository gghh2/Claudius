using System;
using UnityEngine;

/// <summary>
/// Porte-monnaie du joueur. Singleton persistant. Notifie les changements
/// via l'événement OnCreditsChanged pour que l'UI ou d'autres systèmes
/// s'y abonnent sans coupler.
/// </summary>
public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [SerializeField] private int credits = 0;
    public int Credits => credits;

    /// <summary>Émis quand le solde change. Paramètre : nouveau solde.</summary>
    public event Action<int> OnCreditsChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddCredits(int amount)
    {
        if (amount <= 0) return;
        credits += amount;
        OnCreditsChanged?.Invoke(credits);

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
            Debug.Log($"[Wallet] +{amount} crédits (total: {credits})");

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowSuccess($"+{amount} crédits");
    }

    public bool SpendCredits(int amount)
    {
        if (amount <= 0) return true;
        if (credits < amount) return false;

        credits -= amount;
        OnCreditsChanged?.Invoke(credits);

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
            Debug.Log($"[Wallet] -{amount} crédits (total: {credits})");

        return true;
    }

    /// <summary>Charge un solde depuis une sauvegarde. N'émet pas de toast.</summary>
    public void LoadCredits(int amount)
    {
        credits = Mathf.Max(0, amount);
        OnCreditsChanged?.Invoke(credits);
    }
}

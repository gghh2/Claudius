using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Journalise sur disque CHAQUE mission proposée par un PNJ — y compris les cas
/// ratés (token [QUEST:...] absent ou mal formé). Outil de debug destiné à
/// mesurer la fiabilité du LLM à produire des tokens valides
/// (cf. SPEC_LLM_local.md, Phase 5).
///
/// Fichier : {Application.persistentDataPath}/missions_proposees.log
/// Actif uniquement en éditeur / development build (<c>Debug.isDebugBuild</c>) :
/// silencieux dans un build de distribution, rien à désactiver à la main.
/// </summary>
public static class MissionProposalLogger
{
    /// <summary>Chemin complet du fichier de log.</summary>
    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, "missions_proposees.log");

    static bool sessionHeaderWritten;

    /// <summary>
    /// Consigne une réponse d'IA et le résultat de la détection de quête.
    /// </summary>
    /// <param name="npcName">Nom du PNJ.</param>
    /// <param name="npcRole">Rôle du PNJ.</param>
    /// <param name="rawResponse">Réponse brute de l'IA, avant nettoyage des tokens.</param>
    /// <param name="detectedQuests">Quêtes détectées (peut être null ou vide).</param>
    public static void Log(string npcName, string npcRole, string rawResponse,
                           List<QuestToken> detectedQuests)
    {
        if (!Debug.isDebugBuild)
            return;

        try
        {
            var sb = new StringBuilder();

            if (!sessionHeaderWritten)
            {
                sb.AppendLine();
                sb.AppendLine("################################################################");
                sb.AppendLine($"### Session démarrée le {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("################################################################");
                sessionHeaderWritten = true;
            }

            int count = detectedQuests != null ? detectedQuests.Count : 0;

            sb.AppendLine("----------------------------------------------------------------");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] PNJ : {npcName}  (rôle : {npcRole})");
            sb.AppendLine("--- Réponse brute de l'IA ---");
            sb.AppendLine(string.IsNullOrEmpty(rawResponse) ? "(vide)" : rawResponse);
            sb.AppendLine(count > 0
                ? $"--- Tokens détectés : {count} ---"
                : "--- Tokens détectés : 0  (AUCUNE QUÊTE PARSÉE) ---");

            for (int i = 0; i < count; i++)
            {
                QuestToken q = detectedQuests[i];
                sb.AppendLine($"  #{i + 1}  type={q.questType}  zone={q.zoneName}  desc={q.description}");
            }

            File.AppendAllText(FilePath, sb.ToString());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MissionProposalLogger] Écriture du log échouée : {e.Message}");
        }
    }

    /// <summary>Vide le fichier de log et réarme l'en-tête de session.</summary>
    public static void Clear()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            sessionHeaderWritten = false;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MissionProposalLogger] Effacement du log échoué : {e.Message}");
        }
    }
}

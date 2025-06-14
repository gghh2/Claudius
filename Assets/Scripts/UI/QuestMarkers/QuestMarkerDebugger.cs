using UnityEngine;
using System.Linq;

/// <summary>
/// Script de debug pour le système de marqueurs de quête
/// </summary>
public class QuestMarkerDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public KeyCode debugKey = KeyCode.F9;
    public bool autoDebugEverySecond = false;
    
    private float debugTimer = 0f;
    
    void Update()
    {
        // Debug manuel avec touche
        if (Input.GetKeyDown(debugKey))
        {
            PerformFullDebug();
        }
        
        // Debug automatique
        if (autoDebugEverySecond)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= 1f)
            {
                debugTimer = 0f;
                PerformQuickDebug();
            }
        }
    }
    
    void PerformFullDebug()
    {
        Debug.Log("========== QUEST MARKER DEBUG ==========");
        
        // 1. Vérifie les composants essentiels
        Debug.Log("=== 1. COMPOSANTS ===");
        Debug.Log($"QuestManager existe: {QuestManager.Instance != null}");
        Debug.Log($"QuestJournal existe: {QuestJournal.Instance != null}");
        Debug.Log($"QuestMarkerSystem existe: {QuestMarkerSystem.Instance != null}");
        Debug.Log($"Camera principale: {Camera.main != null}");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"Joueur (tag Player): {player != null}");
        if (player != null)
            Debug.Log($"Position joueur: {player.transform.position}");
        
        // 2. Liste toutes les quêtes actives
        Debug.Log("\n=== 2. QUÊTES ACTIVES ===");
        if (QuestManager.Instance != null)
        {
            Debug.Log($"Nombre de quêtes actives: {QuestManager.Instance.activeQuests.Count}");
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                Debug.Log($"\n- Quête: {quest.questData.description}");
                Debug.Log($"  ID: {quest.questId}");
                Debug.Log($"  Type: {quest.questData.questType}");
                Debug.Log($"  NPC donneur: {quest.giverNPCName}");
                Debug.Log($"  Progression: {quest.currentProgress}/{quest.questData.quantity}");
                Debug.Log($"  Complétée: {quest.isCompleted}");
                Debug.Log($"  Objets spawnés: {quest.spawnedObjects.Count}");
                
                // Liste les objets spawnés
                foreach (var obj in quest.spawnedObjects)
                {
                    if (obj != null)
                    {
                        QuestObject qo = obj.GetComponent<QuestObject>();
                        if (qo != null)
                        {
                            float distance = player != null ? 
                                Vector3.Distance(player.transform.position, obj.transform.position) : 0f;
                            Debug.Log($"    - {qo.objectName} à {obj.transform.position} (Distance: {distance:F1}m, Collecté: {qo.isCollected})");
                        }
                    }
                }
            }
        }
        
        // 3. Vérifie les objets de quête actifs
        Debug.Log("\n=== 3. TOUS LES OBJETS DE QUÊTE ===");
        QuestObject[] allQuestObjects = FindObjectsOfType<QuestObject>();
        Debug.Log($"Nombre total d'objets de quête: {allQuestObjects.Length}");
        
        int uncollected = allQuestObjects.Count(qo => !qo.isCollected);
        Debug.Log($"Non collectés: {uncollected}");
        
        Debug.Log("========== FIN DU DEBUG ==========");
    }
    
    void PerformQuickDebug()
    {
        if (QuestManager.Instance == null) return;
        
        int activeQuests = QuestManager.Instance.activeQuests.Count;
        int totalObjects = 0;
        int uncollectedObjects = 0;
        
        foreach (var quest in QuestManager.Instance.activeQuests)
        {
            foreach (var obj in quest.spawnedObjects)
            {
                if (obj != null)
                {
                    totalObjects++;
                    QuestObject qo = obj.GetComponent<QuestObject>();
                    if (qo != null && !qo.isCollected)
                        uncollectedObjects++;
                }
            }
        }
        
        Debug.Log($"[QuickDebug] Quêtes: {activeQuests}, Objets: {totalObjects} (Non collectés: {uncollectedObjects})");
    }
    
    [ContextMenu("Debug Complet")]
    public void ForceFullDebug()
    {
        PerformFullDebug();
    }
}

using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public enum QuestType
{
    FETCH,      // Ramasser des objets
    DELIVERY,   // Livrer quelque chose à quelqu'un
    EXPLORE,    // Explorer une zone
    TALK,       // Parler à un NPC
    INTERACT,   // Interagir avec un objet
    ESCORT      // Escorter quelqu'un
}

[System.Serializable]
public class QuestToken
{
    public QuestType questType;
    public string questId;
    public string objectName;
    public string targetName;
    public string zoneName;
    public QuestZoneType? zoneType;
    public QuestObjectType? objectType;
    public int quantity = 1;
    public string description;
    
    // Constructeur
    public QuestToken(QuestType type, string id)
    {
        questType = type;
        questId = id;
    }
}

public class QuestTokenDetector : MonoBehaviour
{
    public static QuestTokenDetector Instance { get; private set; }
    
    [Header("Token Detection")]
    public bool debugMode = true;
    
    // Patterns regex pour détecter les tokens
    private readonly Dictionary<QuestType, string> tokenPatterns = new Dictionary<QuestType, string>
    {
        { QuestType.FETCH, @"\[QUEST:FETCH:([^:]+):([^:]+):?([^\]]*)\]" },
        { QuestType.DELIVERY, @"\[QUEST:DELIVERY:([^:]+):([^:]+):([^:]+):?([^\]]*)\]" },
        { QuestType.EXPLORE, @"\[QUEST:EXPLORE:([^:]+):?([^\]]*)\]" },
        { QuestType.TALK, @"\[QUEST:TALK:([^:]+):([^:]+):?([^\]]*)\]" },
        { QuestType.INTERACT, @"\[QUEST:INTERACT:([^:]+):([^:]+):?([^\]]*)\]" },
        { QuestType.ESCORT, @"\[QUEST:ESCORT:([^:]+):([^:]+):([^:]+):?([^\]]*)\]" }
    };
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Méthode principale pour analyser un message IA
    public List<QuestToken> DetectQuestTokens(string aiMessage)
    {
        List<QuestToken> detectedQuests = new List<QuestToken>();
        
        if (string.IsNullOrEmpty(aiMessage))
            return detectedQuests;
        
        if (debugMode)
            Debug.Log($"=== ANALYSE MESSAGE IA ===\n{aiMessage}");
        
        // Vérifie chaque type de quête
        foreach (var kvp in tokenPatterns)
        {
            QuestType questType = kvp.Key;
            string pattern = kvp.Value;
            
            MatchCollection matches = Regex.Matches(aiMessage, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                QuestToken token = ParseQuestToken(questType, match);
                if (token != null)
                {
                    // Validate that the quest can actually be created
                    if (IsQuestTokenValid(token))
                    {
                        detectedQuests.Add(token);
                        
                        if (debugMode)
                            Debug.Log($"Token détecté et validé: {token.questType} - {token.description}");
                    }
                    else
                    {
                        if (debugMode)
                            Debug.LogWarning($"Token détecté mais INVALIDE: {token.questType} - {token.description} (zone ou type non supporté)");
                    }
                }
            }
        }
        
        return detectedQuests;
    }
    
    QuestToken ParseQuestToken(QuestType questType, Match match)
    {
        try
        {
            string questId = System.Guid.NewGuid().ToString().Substring(0, 8);
            QuestToken token = new QuestToken(questType, questId);
            
            switch (questType)
            {
                case QuestType.FETCH:
                    // [QUEST:FETCH:cristal_energie:laboratory:3]
                    token.objectName = match.Groups[1].Value;
                    token.zoneName = match.Groups[2].Value;
                    if (match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out int qty))
                        token.quantity = qty;
                    token.zoneType = ParseZoneType(token.zoneName);
                    token.objectType = QuestObjectType.Item;
                    token.description = $"Trouvez {token.quantity} {token.objectName} dans {token.zoneName}";
                    break;
                
                case QuestType.DELIVERY:
                    // [QUEST:DELIVERY:message_secret:garde_imperial:hangar]
                    token.objectName = match.Groups[1].Value;
                    token.targetName = match.Groups[2].Value;
                    token.zoneName = match.Groups[3].Value;
                    token.zoneType = ParseZoneType(token.zoneName);
                    token.objectType = QuestObjectType.NPC;
                    token.description = $"Livrez {token.objectName} à {token.targetName} dans {token.zoneName}";
                    break;
                
                case QuestType.EXPLORE:
                    // [QUEST:EXPLORE:ruines_anciennes]
                    token.zoneName = match.Groups[1].Value;
                    token.zoneType = ParseZoneType(token.zoneName);
                    token.objectType = QuestObjectType.Marker;
                    token.description = $"Explorez {token.zoneName}";
                    break;
                
                case QuestType.TALK:
                    // [QUEST:TALK:scientifique_perdu:laboratory]
                    token.targetName = match.Groups[1].Value;
                    token.zoneName = match.Groups[2].Value;
                    token.zoneType = ParseZoneType(token.zoneName);
                    token.objectType = QuestObjectType.NPC;
                    token.description = $"Parlez à {token.targetName} dans {token.zoneName}";
                    break;
                
                case QuestType.INTERACT:
                    // [QUEST:INTERACT:terminal_securite:security]
                    token.objectName = match.Groups[1].Value;
                    token.zoneName = match.Groups[2].Value;
                    token.zoneType = ParseZoneType(token.zoneName);
                    token.objectType = QuestObjectType.InteractableObject;
                    token.description = $"Interagissez avec {token.objectName} dans {token.zoneName}";
                    break;
                
                case QuestType.ESCORT:
                    // [QUEST:ESCORT:refugie:zone_securisee:residential]
                    token.targetName = match.Groups[1].Value;
                    token.zoneName = match.Groups[2].Value;
                    if (match.Groups[3].Success)
                        token.zoneName = match.Groups[3].Value;
                    token.zoneType = ParseZoneType(token.zoneName);
                    token.objectType = QuestObjectType.NPC;
                    token.description = $"Escortez {token.targetName} vers {token.zoneName}";
                    break;
            }
            
            return token;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur parsing token {questType}: {e.Message}");
            return null;
        }
    }
    
    QuestZoneType? ParseZoneType(string zoneName)
	{
	    string zoneNameLower = zoneName.ToLower();
	    
	    // LABORATORY - Laboratoire
	    if (zoneNameLower.Contains("lab") || zoneNameLower.Contains("scientif") || 
	        zoneNameLower.Contains("recherche") || zoneNameLower.Contains("experience") ||
	        zoneNameLower.Contains("test") || zoneNameLower.Contains("analyse") ||
	        zoneNameLower.Contains("chimie") || zoneNameLower.Contains("biologie") ||
	        zoneNameLower.Contains("physique") || zoneNameLower.Contains("etude") ||
	        zoneNameLower.Contains("sample") || zoneNameLower.Contains("echantillon"))
	        return QuestZoneType.Laboratory;
	    
	    // HANGAR - Zones techniques et vaisseaux
	    if (zoneNameLower.Contains("hangar") || zoneNameLower.Contains("vaisseau") ||
	        zoneNameLower.Contains("ship") || zoneNameLower.Contains("navire") ||
	        zoneNameLower.Contains("dock") || zoneNameLower.Contains("garage") ||
	        zoneNameLower.Contains("reparation") || zoneNameLower.Contains("maintenance") ||
	        zoneNameLower.Contains("moteur") || zoneNameLower.Contains("cargo") ||
	        zoneNameLower.Contains("transport") || zoneNameLower.Contains("vehicule"))
	        return QuestZoneType.Hangar;
	    
	    // MARKET - Commerce et échanges
	    if (zoneNameLower.Contains("marche") || zoneNameLower.Contains("commerce") ||
	        zoneNameLower.Contains("boutique") || zoneNameLower.Contains("magasin") ||
	        zoneNameLower.Contains("vente") || zoneNameLower.Contains("achat") ||
	        zoneNameLower.Contains("echange") || zoneNameLower.Contains("trade") ||
	        zoneNameLower.Contains("vendeur") || zoneNameLower.Contains("marchand") ||
	        zoneNameLower.Contains("bazaar") || zoneNameLower.Contains("market"))
	        return QuestZoneType.Market;
	    
	    // RUINS - Ruines et sites anciens
	    if (zoneNameLower.Contains("ruine") || zoneNameLower.Contains("ancien") || 
	        zoneNameLower.Contains("ruins") || zoneNameLower.Contains("vestige") ||
	        zoneNameLower.Contains("archeologie") || zoneNameLower.Contains("temple") ||
	        zoneNameLower.Contains("monument") || zoneNameLower.Contains("relique") ||
	        zoneNameLower.Contains("artifact") || zoneNameLower.Contains("site") ||
	        zoneNameLower.Contains("prehistoire") || zoneNameLower.Contains("fouille"))
	        return QuestZoneType.Ruins;
	    
	    // SECURITY - Sécurité et contrôle
	    if (zoneNameLower.Contains("secur") || zoneNameLower.Contains("garde") ||
	        zoneNameLower.Contains("police") || zoneNameLower.Contains("patrol") ||
	        zoneNameLower.Contains("surveillance") || zoneNameLower.Contains("controle") ||
	        zoneNameLower.Contains("checkpoint") || zoneNameLower.Contains("prison") ||
	        zoneNameLower.Contains("detention") || zoneNameLower.Contains("defense") ||
	        zoneNameLower.Contains("armurerie") || zoneNameLower.Contains("militaire"))
	        return QuestZoneType.SecurityArea;
	    
	    // STORAGE - Stockage et entrepôts
	    if (zoneNameLower.Contains("stock") || zoneNameLower.Contains("entrepot") || 
	        zoneNameLower.Contains("storage") || zoneNameLower.Contains("depot") ||
	        zoneNameLower.Contains("reserve") || zoneNameLower.Contains("magasin") ||
	        zoneNameLower.Contains("warehouse") || zoneNameLower.Contains("cave") ||
	        zoneNameLower.Contains("container") || zoneNameLower.Contains("cargaison") ||
	        zoneNameLower.Contains("provisions") || zoneNameLower.Contains("ressource"))
	        return QuestZoneType.Storage;
	    
	    // RESIDENTIAL - Zones d'habitation
	    if (zoneNameLower.Contains("resident") || zoneNameLower.Contains("habitation") ||
	        zoneNameLower.Contains("maison") || zoneNameLower.Contains("appartement") ||
	        zoneNameLower.Contains("logement") || zoneNameLower.Contains("quartier") ||
	        zoneNameLower.Contains("domicile") || zoneNameLower.Contains("foyer") ||
	        zoneNameLower.Contains("chambre") || zoneNameLower.Contains("dortoir") ||
	        zoneNameLower.Contains("famille") || zoneNameLower.Contains("civil"))
	        return QuestZoneType.Residential;
	    
	    // ENGINEERING - Ingénierie et machines
	    if (zoneNameLower.Contains("engine") || zoneNameLower.Contains("machine") ||
	        zoneNameLower.Contains("reacteur") || zoneNameLower.Contains("generateur") ||
	        zoneNameLower.Contains("energie") || zoneNameLower.Contains("power") ||
	        zoneNameLower.Contains("turbine") || zoneNameLower.Contains("propulsion") ||
	        zoneNameLower.Contains("technique") || zoneNameLower.Contains("mecanique") ||
	        zoneNameLower.Contains("systeme") || zoneNameLower.Contains("informatique"))
	        return QuestZoneType.Engineering;
	    
	    // BRIDGE - Commandement et contrôle
	    if (zoneNameLower.Contains("pont") || zoneNameLower.Contains("command") ||
	        zoneNameLower.Contains("controle") || zoneNameLower.Contains("bridge") ||
	        zoneNameLower.Contains("capitaine") || zoneNameLower.Contains("navigation") ||
	        zoneNameLower.Contains("pilotage") || zoneNameLower.Contains("operation") ||
	        zoneNameLower.Contains("central") || zoneNameLower.Contains("direction") ||
	        zoneNameLower.Contains("quartier_general") || zoneNameLower.Contains("bureau"))
	        return QuestZoneType.Bridge;
	    
	    // MEDICAL - Soins et médical
	    if (zoneNameLower.Contains("medical") || zoneNameLower.Contains("soin") ||
	        zoneNameLower.Contains("hopital") || zoneNameLower.Contains("infirmerie") ||
	        zoneNameLower.Contains("docteur") || zoneNameLower.Contains("medecin") ||
	        zoneNameLower.Contains("chirurgie") || zoneNameLower.Contains("traitement") ||
	        zoneNameLower.Contains("pharmacie") || zoneNameLower.Contains("guerison") ||
	        zoneNameLower.Contains("urgence") || zoneNameLower.Contains("sante"))
	        return QuestZoneType.MedicalBay;
	    
	    return null;
	}
    
    // Validate that a quest token can actually be created
    bool IsQuestTokenValid(QuestToken token)
    {
        // Pour l'instant, on accepte tous les tokens détectés
        // La validation se fera lors de la création de la quête dans QuestManager
        return true;
        
        /* Code de validation désactivé temporairement
        if (QuestZoneManager.Instance == null)
        {
            Debug.LogWarning("QuestZoneManager.Instance is null - cannot validate quest");
            return false;
        }
        
        // Check if zone type exists and supports the required object type
        if (token.zoneType.HasValue && token.objectType.HasValue)
        {
            var supportingZones = QuestZoneManager.Instance.GetZonesSupportingObjectType(token.objectType.Value);
            
            // Check if any of the supporting zones match the requested zone type
            bool zoneSupportsQuest = supportingZones.Any(z => z.zoneType == token.zoneType.Value);
            
            if (!zoneSupportsQuest)
            {
                Debug.LogWarning($"Zone type {token.zoneType.Value} does not support object type {token.objectType.Value}");
                return false;
            }
            
            return true;
        }
        
        // If we can't determine zone or object type, consider it invalid
        Debug.LogWarning($"Quest token missing zone type or object type information");
        return false;
        */
    }
    
    // Nettoie le message en retirant les tokens (pour l'affichage au joueur)
    public string CleanMessageFromTokens(string aiMessage)
    {
        string cleanedMessage = aiMessage;
        
        foreach (var pattern in tokenPatterns.Values)
        {
            cleanedMessage = Regex.Replace(cleanedMessage, pattern, "", RegexOptions.IgnoreCase);
        }
        
        // Nettoie les espaces multiples
        cleanedMessage = Regex.Replace(cleanedMessage, @"\s+", " ").Trim();
        
        return cleanedMessage;
    }
    
    // Méthode pour tester le système
    [ContextMenu("Test Token Detection")]
    public void TestTokenDetection()
    {
        string testMessage = @"Ah, parfait timing ! J'ai justement besoin d'aide. 
        [QUEST:FETCH:cristal_energie:laboratory:2] 
        Pourriez-vous me récupérer ces cristaux ? Ils sont essentiels pour mes recherches.
        [QUEST:TALK:assistant_laboratoire:laboratory]
        Aussi, mon assistant a des informations importantes à vous transmettre.";
        
        List<QuestToken> tokens = DetectQuestTokens(testMessage);
        string cleanMessage = CleanMessageFromTokens(testMessage);
        
        Debug.Log($"Message nettoyé: {cleanMessage}");
        Debug.Log($"Quêtes détectées: {tokens.Count}");
    }
}

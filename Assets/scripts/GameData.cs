using UnityEngine;

// Singleton to store game data that persists between scenes
public class GameData : MonoBehaviour
{
    private static GameData instance;
    
    public static GameData Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameData");
                instance = go.AddComponent<GameData>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    // Character selections for each player (0 = character 1, 1 = character 2, etc.)
    public int player1CharacterID = 0;
    public int player2CharacterID = 0;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // Set character for a player
    public void SetPlayerCharacter(int playerIndex, int characterID)
    {
        if (playerIndex == 0)
        {
            player1CharacterID = characterID;
            Debug.Log("Player 1 selected character: " + characterID);
        }
        else if (playerIndex == 1)
        {
            player2CharacterID = characterID;
            Debug.Log("Player 2 selected character: " + characterID);
        }
    }
    
    // Get character for a player
    public int GetPlayerCharacter(int playerIndex)
    {
        if (playerIndex == 0)
            return player1CharacterID;
        else if (playerIndex == 1)
            return player2CharacterID;
        
        return 0; // Default to first character
    }
    
    // Reset all data
    public void Reset()
    {
        player1CharacterID = 0;
        player2CharacterID = 0;
    }
}

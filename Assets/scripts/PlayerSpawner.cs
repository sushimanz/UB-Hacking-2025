using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Setup")]
    [Tooltip("Reference to PlayerInputManager component")]
    public PlayerInputManager playerInputManager;
    
    [Header("Spawn Settings")]
    [Tooltip("Spawn both players immediately on scene load")]
    public bool spawnOnStart = true;
    [Tooltip("Player 1 spawn position")]
    public Vector3 player1SpawnPosition = new Vector3(-3f, 0f, 0f);
    [Tooltip("Player 2 spawn position")]
    public Vector3 player2SpawnPosition = new Vector3(3f, 0f, 0f);
    
    [Header("Camera Setup")]
    [Tooltip("Camera script to update with player references")]
    public cam cameraScript;
    
    void Start()
    {
        if (spawnOnStart && playerInputManager != null)
        {
            SpawnPlayers();
        }
    }
    
    void SpawnPlayers()
    {
        // Get available input devices
        var keyboard = Keyboard.current;
        var gamepads = Gamepad.all;
        
        Debug.Log("Available devices - Keyboard: " + (keyboard != null) + ", Gamepads: " + gamepads.Count);
        
        // Determine device for each player
        InputDevice player1Device = null;
        InputDevice player2Device = null;
        
        // Store spawned player transforms
        Transform player1Transform = null;
        Transform player2Transform = null;
        
        // PRIORITIZE TWO GAMEPADS if available
        if (gamepads.Count >= 2)
        {
            // Two gamepads available - use them for both players
            player1Device = gamepads[0];
            player2Device = gamepads[1];
            Debug.Log("Using 2 gamepads");
        }
        else if (gamepads.Count == 1)
        {
            // Only one gamepad - player 1 gets gamepad, player 2 gets keyboard
            player1Device = gamepads[0];
            if (keyboard != null)
            {
                player2Device = keyboard;
                Debug.Log("Using 1 gamepad + keyboard");
            }
        }
        else if (keyboard != null)
        {
            // No gamepads - player 1 gets keyboard (player 2 won't spawn)
            player1Device = keyboard;
            Debug.Log("Using keyboard only");
        }
        
        // Spawn Player 1
        if (player1Device != null)
        {
            PlayerInput player1 = PlayerInput.Instantiate(
                playerInputManager.playerPrefab,
                playerIndex: 0,
                controlScheme: null,
                pairWithDevice: player1Device
            );
            
            if (player1 != null)
            {
                player1.gameObject.name = "Player 1";
                player1.transform.position = player1SpawnPosition;
                player1Transform = player1.transform;
                Debug.Log("Player 1 spawned on device: " + player1Device.displayName + " at " + player1SpawnPosition);
            }
        }
        else
        {
            Debug.LogError("No input device available for Player 1!");
        }
        
        // Spawn Player 2
        if (player2Device != null)
        {
            PlayerInput player2 = PlayerInput.Instantiate(
                playerInputManager.playerPrefab,
                playerIndex: 1,
                controlScheme: null,
                pairWithDevice: player2Device
            );
            
            if (player2 != null)
            {
                player2.gameObject.name = "Player 2";
                player2.transform.position = player2SpawnPosition;
                player2Transform = player2.transform;
                Debug.Log("Player 2 spawned on device: " + player2Device.displayName + " at " + player2SpawnPosition);
            }
        }
        else
        {
            Debug.LogWarning("No input device available for Player 2! Connect a second gamepad or use keyboard.");
        }
        
        // Initialize camera with player references
        if (cameraScript != null && player1Transform != null && player2Transform != null)
        {
            cameraScript.player1 = player1Transform;
            cameraScript.player2 = player2Transform;
            Debug.Log("Camera initialized with player references");
        }
        else if (cameraScript == null)
        {
            Debug.LogWarning("Camera script not assigned to PlayerSpawner!");
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("Assign your character buttons in a single array, row by row")]
    public Button[] characterButtons;

    [Header("Grid settings")]
    public int rows = 2;
    public int columns = 4;

    [Header("Player highlights")]
    public RawImage player1Highlight;
    public RawImage player2Highlight;

    [Header("Character image containers")]
    public GameObject player1Images;
    public GameObject player2Images;

    [Header("Character name text")]
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player2NameText;

    [Header("Ready text")]
    public TextMeshProUGUI player1ReadyText;
    public TextMeshProUGUI player2ReadyText;

    [Header("Scene Loading")]
    public string nextSceneName = "GameScene";

    [Header("Selection")]
    public GameObject player1ConfirmIndicator;
    public GameObject player2ConfirmIndicator;

    [Header("Movement & Sound")]
    public float moveCooldown = 0.2f;
    public AudioSource audioSource;
    public AudioClip moveSound;
    public AudioClip invalidSound;

    [Header("Allowed Characters")]
    public string[] allowedCharacters = { "Ethan", "Alan" };

    private Button[,] buttonGrid;

    private int player1Row = 0, player1Col = 0;
    private int player2Row = 0, player2Col = 0;

    private float player1LastMoveTime = 0f;
    private float player2LastMoveTime = 0f;

    private Dictionary<string, GameObject> player1ImagesByName = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> player2ImagesByName = new Dictionary<string, GameObject>();

    private bool player1Confirmed = false;
    private bool player2Confirmed = false;

    public static string player1SelectedCharacter = "";
    public static string player2SelectedCharacter = "";

    void Start()
    {
        // Check buttons array
        if (characterButtons == null || characterButtons.Length == 0)
        {
            Debug.LogError("No character buttons assigned!");
            return;
        }
        if (characterButtons.Length != rows * columns)
        {
            Debug.LogError("Character buttons count does not match rows * columns!");
            return;
        }

        // Build grid
        buttonGrid = new Button[rows, columns];
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int r = i / columns;
            int c = i % columns;
            buttonGrid[r, c] = characterButtons[i];
        }

        // Map images
        MapImagesByName(player1Images, player1ImagesByName);
        MapImagesByName(player2Images, player2ImagesByName);
        HideAllImages(player1ImagesByName);
        HideAllImages(player2ImagesByName);

        // Hide indicators
        if (player1ConfirmIndicator != null) player1ConfirmIndicator.SetActive(false);
        if (player2ConfirmIndicator != null) player2ConfirmIndicator.SetActive(false);
        if (player1ReadyText != null) player1ReadyText.gameObject.SetActive(false);
        if (player2ReadyText != null) player2ReadyText.gameObject.SetActive(false);

        UpdateHighlights();
        UpdateDisplayedImages();
    }

    void Update()
    {
        HandlePlayer1Input();
        HandlePlayer2Input();
    }

    void HandlePlayer1Input()
    {
        // Movement cooldown
        if (Time.time - player1LastMoveTime >= moveCooldown)
        {
            int dRow = 0, dCol = 0;

            // WASD input
            if (Keyboard.current.wKey.isPressed) dRow = -1;
            else if (Keyboard.current.sKey.isPressed) dRow = 1;
            if (Keyboard.current.aKey.isPressed) dCol = -1;
            else if (Keyboard.current.dKey.isPressed) dCol = 1;

            // Xbox controller (first gamepad)
            if (Gamepad.all.Count > 0)
            {
                var pad = Gamepad.all[0];
                if (pad.leftStick.up.wasPressedThisFrame) dRow = -1;
                else if (pad.leftStick.down.wasPressedThisFrame) dRow = 1;
                if (pad.leftStick.left.wasPressedThisFrame) dCol = -1;
                else if (pad.leftStick.right.wasPressedThisFrame) dCol = 1;
            }

            if ((dRow != 0 || dCol != 0) && !player1Confirmed)
            {
                player1Row = Mathf.Clamp(player1Row + dRow, 0, rows - 1);
                player1Col = Mathf.Clamp(player1Col + dCol, 0, columns - 1);
                UpdateHighlights();
                UpdateDisplayedImages();
                PlaySound();
                player1LastMoveTime = Time.time;
            }
        }

        // Confirm
        if ((Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.all.Count > 0 && Gamepad.all[0].buttonSouth.wasPressedThisFrame))
        {
            if (!player1Confirmed) Player1Select();
        }

        // Deselect
        if ((Keyboard.current.backspaceKey.wasPressedThisFrame) ||
            (Gamepad.all.Count > 0 && Gamepad.all[0].buttonEast.wasPressedThisFrame))
        {
            if (player1Confirmed)
            {
                player1Confirmed = false;
                if (player1ConfirmIndicator != null) player1ConfirmIndicator.SetActive(false);
                if (player1ReadyText != null) player1ReadyText.gameObject.SetActive(false);
                Debug.Log("Player 1 deselected");
                PlaySound();
            }
        }
    }

    void HandlePlayer2Input()
    {
        // Movement cooldown
        if (Time.time - player2LastMoveTime >= moveCooldown)
        {
            int dRow = 0, dCol = 0;

            // Arrow keys
            if (Keyboard.current.upArrowKey.isPressed) dRow = -1;
            else if (Keyboard.current.downArrowKey.isPressed) dRow = 1;
            if (Keyboard.current.leftArrowKey.isPressed) dCol = -1;
            else if (Keyboard.current.rightArrowKey.isPressed) dCol = 1;

            // PS4 controller (second gamepad)
            if (Gamepad.all.Count > 1)
            {
                var pad = Gamepad.all[1];
                if (pad.leftStick.up.wasPressedThisFrame) dRow = -1;
                else if (pad.leftStick.down.wasPressedThisFrame) dRow = 1;
                if (pad.leftStick.left.wasPressedThisFrame) dCol = -1;
                else if (pad.leftStick.right.wasPressedThisFrame) dCol = 1;
            }

            if ((dRow != 0 || dCol != 0) && !player2Confirmed)
            {
                player2Row = Mathf.Clamp(player2Row + dRow, 0, rows - 1);
                player2Col = Mathf.Clamp(player2Col + dCol, 0, columns - 1);
                UpdateHighlights();
                UpdateDisplayedImages();
                PlaySound();
                player2LastMoveTime = Time.time;
            }
        }

        // Confirm
        if ((Keyboard.current.enterKey.wasPressedThisFrame) ||
            (Gamepad.all.Count > 1 && Gamepad.all[1].buttonSouth.wasPressedThisFrame))
        {
            if (!player2Confirmed) Player2Select();
        }

        // Deselect
        if ((Keyboard.current.deleteKey.wasPressedThisFrame) ||
            (Gamepad.all.Count > 1 && Gamepad.all[1].buttonEast.wasPressedThisFrame))
        {
            if (player2Confirmed)
            {
                player2Confirmed = false;
                if (player2ConfirmIndicator != null) player2ConfirmIndicator.SetActive(false);
                if (player2ReadyText != null) player2ReadyText.gameObject.SetActive(false);
                Debug.Log("Player 2 deselected");
                PlaySound();
            }
        }
    }

    void UpdateHighlights()
    {
        if (player1Highlight != null)
            player1Highlight.transform.position = buttonGrid[player1Row, player1Col].transform.position;
        if (player2Highlight != null)
            player2Highlight.transform.position = buttonGrid[player2Row, player2Col].transform.position;
    }

    void UpdateDisplayedImages()
    {
        string player1ButtonName = buttonGrid[player1Row, player1Col].name;
        string player2ButtonName = buttonGrid[player2Row, player2Col].name;

        if (player1NameText != null) player1NameText.text = player1ButtonName;
        if (player2NameText != null) player2NameText.text = player2ButtonName;

        HideAllImages(player1ImagesByName);
        HideAllImages(player2ImagesByName);

        if (player1ImagesByName.ContainsKey(player1ButtonName))
            player1ImagesByName[player1ButtonName].SetActive(true);
        if (player2ImagesByName.ContainsKey(player2ButtonName))
            player2ImagesByName[player2ButtonName].SetActive(true);
    }

    bool IsCharacterAllowed(string characterName)
    {
        foreach (string allowedChar in allowedCharacters)
        {
            if (characterName.Equals(allowedChar, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    void Player1Select()
    {
        string selected = buttonGrid[player1Row, player1Col].name;

        if (!IsCharacterAllowed(selected))
        {
            Debug.Log("Player 1 cannot select " + selected + " - Only Ethan and Alan are available!");
            PlayInvalidSound();
            return;
        }

        player1SelectedCharacter = selected;
        player1Confirmed = true;
        PlayerPrefs.SetString("Player1Character", selected);
        if (player1ConfirmIndicator != null) player1ConfirmIndicator.SetActive(true);
        if (player1ReadyText != null) player1ReadyText.gameObject.SetActive(true);
        Debug.Log("Player 1 CONFIRMED: " + selected);
        PlaySound();
        CheckIfBothPlayersReady();
    }

    void Player2Select()
    {
        string selected = buttonGrid[player2Row, player2Col].name;

        if (!IsCharacterAllowed(selected))
        {
            Debug.Log("Player 2 cannot select " + selected + " - Only Ethan and Alan are available!");
            PlayInvalidSound();
            return;
        }

        player2SelectedCharacter = selected;
        player2Confirmed = true;
        PlayerPrefs.SetString("Player2Character", selected);
        if (player2ConfirmIndicator != null) player2ConfirmIndicator.SetActive(true);
        if (player2ReadyText != null) player2ReadyText.gameObject.SetActive(true);
        Debug.Log("Player 2 CONFIRMED: " + selected);
        PlaySound();
        CheckIfBothPlayersReady();
    }

    void CheckIfBothPlayersReady()
    {
        if (player1Confirmed && player2Confirmed)
        {
            Debug.Log("✅ BOTH PLAYERS READY! LOADING SCENE...");
            StartCoroutine(LoadNextSceneWithDelay(1f));
        }
    }

    IEnumerator LoadNextSceneWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogError("Next scene name is not set!");
    }

    void PlaySound()
    {
        if (audioSource != null && moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }

    void PlayInvalidSound()
    {
        if (audioSource != null && invalidSound != null)
            audioSource.PlayOneShot(invalidSound);
        else if (audioSource != null && moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }

    private void MapImagesByName(GameObject parent, Dictionary<string, GameObject> dictionary)
    {
        if (parent == null) return;
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            dictionary[child.name] = child;
        }
    }

    private void HideAllImages(Dictionary<string, GameObject> images)
    {
        if (images == null) return;
        foreach (var go in images.Values)
            if (go != null) go.SetActive(false);
    }
}
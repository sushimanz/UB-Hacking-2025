using UnityEngine;
using UnityEngine.UI;
using TMPro; // ✅ Added for TextMeshPro
using System.Collections;
using System.Collections.Generic;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("Assign your character buttons in a single array, row by row")]
    public Button[] characterButtons; // Example: Erik → Ethan → Alan → Paul → Yorah → Carl → Ken → Jesse

    [Header("Grid settings")]
    public int rows = 2;
    public int columns = 4;

    [Header("Player highlights")]
    public RawImage player1Highlight;
    public RawImage player2Highlight;

    [Header("Character image containers")]
    public GameObject player1Images; // Parent GameObject containing Player 1's images
    public GameObject player2Images; // Parent GameObject containing Player 2's images

    [Header("Character name text")]
    public TextMeshProUGUI player1NameText; // Drag your TextMeshPro component here for Player 1
    public TextMeshProUGUI player2NameText; // Drag your TextMeshPro component here for Player 2

    [Header("Movement & Sound")]
    public float moveCooldown = 0.2f;
    public AudioSource audioSource;
    public AudioClip moveSound;

    private Button[,] buttonGrid;

    private int player1Row = 0, player1Col = 0;
    private int player2Row = 0, player2Col = 0;

    private bool player1CanMove = true;
    private bool player2CanMove = true;

    private Dictionary<string, GameObject> player1ImagesByName = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> player2ImagesByName = new Dictionary<string, GameObject>();

    void Start()
    {
        // Safety check
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

        // Build the 2D grid
        buttonGrid = new Button[rows, columns];
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int r = i / columns;
            int c = i % columns;
            buttonGrid[r, c] = characterButtons[i];
        }

        // Map images by character name
        MapImagesByName(player1Images, player1ImagesByName);
        MapImagesByName(player2Images, player2ImagesByName);

        // Hide all initially
        HideAllImages(player1ImagesByName);
        HideAllImages(player2ImagesByName);

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
        if (!player1CanMove) return;

        int dRow = 0, dCol = 0;

        if (Input.GetKeyDown(KeyCode.W)) dRow = -1;
        else if (Input.GetKeyDown(KeyCode.S)) dRow = 1;
        else if (Input.GetKeyDown(KeyCode.A)) dCol = -1;
        else if (Input.GetKeyDown(KeyCode.D)) dCol = 1;

        if (dRow != 0 || dCol != 0)
        {
            player1Row = Mathf.Clamp(player1Row + dRow, 0, rows - 1);
            player1Col = Mathf.Clamp(player1Col + dCol, 0, columns - 1);

            UpdateHighlights();
            UpdateDisplayedImages();
            PlaySound();
            StartCoroutine(MoveCooldown(1));
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Player1Select();
            PlaySound();
        }
    }

    void HandlePlayer2Input()
    {
        if (!player2CanMove) return;

        int dRow = 0, dCol = 0;

        if (Input.GetKeyDown(KeyCode.UpArrow)) dRow = -1;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) dRow = 1;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) dCol = -1;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) dCol = 1;

        if (dRow != 0 || dCol != 0)
        {
            player2Row = Mathf.Clamp(player2Row + dRow, 0, rows - 1);
            player2Col = Mathf.Clamp(player2Col + dCol, 0, columns - 1);

            UpdateHighlights();
            UpdateDisplayedImages();
            PlaySound();
            StartCoroutine(MoveCooldown(2));
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Player2Select();
            PlaySound();
        }
    }

    private IEnumerator MoveCooldown(int player)
    {
        if (player == 1) player1CanMove = false;
        else player2CanMove = false;

        yield return new WaitForSeconds(moveCooldown);

        if (player == 1) player1CanMove = true;
        else player2CanMove = true;
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
        // Get the currently hovered button names
        string player1ButtonName = buttonGrid[player1Row, player1Col].name;
        string player2ButtonName = buttonGrid[player2Row, player2Col].name;

        // Update the name text for each player
        if (player1NameText != null)
        {
            player1NameText.text = player1ButtonName;
        }

        if (player2NameText != null)
        {
            player2NameText.text = player2ButtonName;
        }

        // Hide all images first
        HideAllImages(player1ImagesByName);
        HideAllImages(player2ImagesByName);

        // Show only the matching image for each player
        if (player1ImagesByName.ContainsKey(player1ButtonName))
        {
            player1ImagesByName[player1ButtonName].SetActive(true);
        }
        else
        {
            Debug.LogWarning($"No image found for Player 1 character: {player1ButtonName}");
        }

        if (player2ImagesByName.ContainsKey(player2ButtonName))
        {
            player2ImagesByName[player2ButtonName].SetActive(true);
        }
        else
        {
            Debug.LogWarning($"No image found for Player 2 character: {player2ButtonName}");
        }
    }

    void Player1Select()
    {
        string selected = buttonGrid[player1Row, player1Col].name;
        Debug.Log("Player 1 selected: " + selected);
    }

    void Player2Select()
    {
        string selected = buttonGrid[player2Row, player2Col].name;
        Debug.Log("Player 2 selected: " + selected);
    }

    void PlaySound()
    {
        if (audioSource != null && moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }

    private void MapImagesByName(GameObject parent, Dictionary<string, GameObject> dictionary)
    {
        if (parent == null) return;

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            // Use the child's name as the key
            dictionary[child.name] = child;
        }
    }

    private void HideAllImages(Dictionary<string, GameObject> images)
    {
        if (images == null) return;
        foreach (var kvp in images.Values)
        {
            if (kvp != null)
                kvp.SetActive(false);
        }
    }
}
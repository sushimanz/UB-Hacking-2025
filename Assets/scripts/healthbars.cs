using UnityEngine;
using UnityEngine.UI;

public class healthbars : MonoBehaviour
{
    [Header("Player 1 Health Bars")]
    [Tooltip("Front health bar for Player 1 (shrinks immediately on damage)")]
    public RawImage player1FrontBar;
    [Tooltip("Back health bar for Player 1 (shrinks smoothly after front bar)")]
    public RawImage player1BackBar;
    [Tooltip("Reference to Player 1")]
    public player player1;

    [Header("Player 2 Health Bars")]
    [Tooltip("Front health bar for Player 2 (shrinks immediately on damage)")]
    public RawImage player2FrontBar;
    [Tooltip("Back health bar for Player 2 (shrinks smoothly after front bar)")]
    public RawImage player2BackBar;
    [Tooltip("Reference to Player 2")]
    public player player2;

    [Header("Health Bar Settings")]
    [Tooltip("How fast the back bar shrinks to match the front bar")]
    public float backBarShrinkSpeed = 0.5f;
    [Tooltip("Delay before back bar starts shrinking")]
    public float backBarDelay = 0.3f;

    // Track previous health values
    private float player1PreviousHealth;
    private float player2PreviousHealth;
    
    // Track when damage occurred for delay
    private float player1DamageTime = -999f;
    private float player2DamageTime = -999f;
    
    // Store original widths of health bars
    private float player1FrontBarOriginalWidth;
    private float player1BackBarOriginalWidth;
    private float player2FrontBarOriginalWidth;
    private float player2BackBarOriginalWidth;

    void Start()
    {
        // Find players if not assigned
        FindPlayers();
        
        // Store original widths
        if (player1FrontBar != null)
            player1FrontBarOriginalWidth = player1FrontBar.rectTransform.sizeDelta.x;
        if (player1BackBar != null)
            player1BackBarOriginalWidth = player1BackBar.rectTransform.sizeDelta.x;
        if (player2FrontBar != null)
            player2FrontBarOriginalWidth = player2FrontBar.rectTransform.sizeDelta.x;
        if (player2BackBar != null)
            player2BackBarOriginalWidth = player2BackBar.rectTransform.sizeDelta.x;
        
        // Initialize previous health values
        if (player1 != null)
        {
            player1PreviousHealth = player1.health;
            UpdateHealthBar(player1FrontBar, player1.health, player1.maxHealth, player1FrontBarOriginalWidth);
            UpdateHealthBar(player1BackBar, player1.health, player1.maxHealth, player1BackBarOriginalWidth);
        }
        
        if (player2 != null)
        {
            player2PreviousHealth = player2.health;
            UpdateHealthBar(player2FrontBar, player2.health, player2.maxHealth, player2FrontBarOriginalWidth);
            UpdateHealthBar(player2BackBar, player2.health, player2.maxHealth, player2BackBarOriginalWidth);
        }
    }

    void FindPlayers()
    {
        // Find all player objects in the scene
        player[] allPlayers = FindObjectsByType<player>(FindObjectsSortMode.None);
        
        foreach (player p in allPlayers)
        {
            if (p.playerIndex == 0)
            {
                player1 = p;
                Debug.Log("Healthbars found Player 1: " + p.gameObject.name);
            }
            else if (p.playerIndex == 1)
            {
                player2 = p;
                Debug.Log("Healthbars found Player 2: " + p.gameObject.name);
            }
        }
        
        // If players not found yet, try again next frame
        if (player1 == null || player2 == null)
        {
            Invoke(nameof(FindPlayers), 0.1f);
        }
    }

    void Update()
    {
        // Update Player 1 health bars
        if (player1 != null && player1FrontBar != null && player1BackBar != null)
        {
            // Check if player took damage
            if (player1.health < player1PreviousHealth)
            {
                player1DamageTime = Time.time;
                player1PreviousHealth = player1.health;
            }

            // Update front bar immediately
            UpdateHealthBar(player1FrontBar, player1.health, player1.maxHealth, player1FrontBarOriginalWidth);

            // Update back bar with delay and smooth shrinking
            if (Time.time - player1DamageTime > backBarDelay)
            {
                RectTransform backBarRect = player1BackBar.rectTransform;
                float currentWidth = backBarRect.sizeDelta.x;
                float targetWidth = player1BackBarOriginalWidth * (player1.health / player1.maxHealth);
                
                if (currentWidth > targetWidth)
                {
                    float newWidth = Mathf.Lerp(currentWidth, targetWidth, backBarShrinkSpeed * Time.deltaTime);
                    Vector2 sizeDelta = backBarRect.sizeDelta;
                    sizeDelta.x = newWidth;
                    backBarRect.sizeDelta = sizeDelta;
                }
            }
        }

        // Update Player 2 health bars
        if (player2 != null && player2FrontBar != null && player2BackBar != null)
        {
            // Check if player took damage
            if (player2.health < player2PreviousHealth)
            {
                player2DamageTime = Time.time;
                player2PreviousHealth = player2.health;
            }

            // Update front bar immediately
            UpdateHealthBar(player2FrontBar, player2.health, player2.maxHealth, player2FrontBarOriginalWidth);

            // Update back bar with delay and smooth shrinking
            if (Time.time - player2DamageTime > backBarDelay)
            {
                RectTransform backBarRect = player2BackBar.rectTransform;
                float currentWidth = backBarRect.sizeDelta.x;
                float targetWidth = player2BackBarOriginalWidth * (player2.health / player2.maxHealth);
                
                if (currentWidth > targetWidth)
                {
                    float newWidth = Mathf.Lerp(currentWidth, targetWidth, backBarShrinkSpeed * Time.deltaTime);
                    Vector2 sizeDelta = backBarRect.sizeDelta;
                    sizeDelta.x = newWidth;
                    backBarRect.sizeDelta = sizeDelta;
                }
            }
        }
    }

    // Helper method to update a health bar's fill amount
    private void UpdateHealthBar(RawImage healthBar, float currentHealth, float maxHealth, float originalWidth)
    {
        if (healthBar == null) return;
        
        float fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        
        // For RawImage, we shrink the RectTransform width
        RectTransform rectTransform = healthBar.rectTransform;
        Vector2 sizeDelta = rectTransform.sizeDelta;
        sizeDelta.x = originalWidth * fillAmount;
        rectTransform.sizeDelta = sizeDelta;
    }
}

using System.Collections;
using UnityEngine;
using TMPro; 

public class FlashingText : MonoBehaviour
{
    public TextMeshProUGUI flashingText; 
    public float blinkInterval = 0.5f; 

    void Start()
    {
        
        if (flashingText == null)
        {
            flashingText = GetComponent<TextMeshProUGUI>();
        }

        
        StartCoroutine(BlinkText());
    }

    IEnumerator BlinkText()
    {
        while (true) 
        {
            flashingText.enabled = !flashingText.enabled; 
            yield return new WaitForSeconds(blinkInterval); 
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class wait : MonoBehaviour
{
    public float waitTime = 5;

    void Start()
    {
        StartCoroutine(intro());
    }

    IEnumerator intro()
    {
        yield return new WaitForSeconds(waitTime);

        SceneManager.LoadScene(1);
    }

}

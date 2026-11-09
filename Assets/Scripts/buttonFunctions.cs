using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class buttonFunctions : MonoBehaviour
{
    void moveToGame() {
        SceneManager.LoadScene("SampleScene");
    }
    
    void exitGame() {
        Application.Quit();
    }
}

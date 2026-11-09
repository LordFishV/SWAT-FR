using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionStart : MonoBehaviour, IInteractable
{
    public bool canTransition;

    public void Interact(GameObject interactingObject)
    {
        // Check if can transition (GameMode.cs Controls This)
        if (!canTransition)
            return;

        LoadLevel();
    }

    public void OnHoverEnter(GameObject hoveringObject)
    {
    }

    public void OnHoverExit(GameObject hoveringObject)
    {
    }

    private void LoadLevel()
    {
        // Get current and next level index
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        // Loops back if nextIndex is greater then the amount of level in build
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            nextIndex = 0; // Loop back

        // Load next level
        SceneManager.LoadScene(nextIndex);
    }
}

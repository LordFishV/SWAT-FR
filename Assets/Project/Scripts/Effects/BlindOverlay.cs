using UnityEngine;
using UnityEngine.UIElements;

// The white screen when the player is flashed.
// Put this on a UI object. Assign the player's Blindable and a CanvasGroup on a
// full-screen white Image (turn the Image's Raycast Target off).
public class BlindOverlay : MonoBehaviour
{
    [SerializeField] private Blindable _player;
    [SerializeField] private CanvasGroup _whiteScreen;

    private void Update()
    {
        _whiteScreen.alpha = _player.Intensity;
    }
}
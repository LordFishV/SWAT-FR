using UnityEngine;

// Put this on the player and on any enemy a flashbang should affect.
// Enemy AI can check IsBlinded. The player's white screen reads Intensity.
public class Blindable : MonoBehaviour
{
    [Tooltip("Seconds spent fading out at the end of the blind.")]
    [SerializeField] private float _fadeTime = 2f;

    private float _blindedUntil;

    public bool IsBlinded => Time.time < _blindedUntil;

    // 1 = fully blind, fading down to 0 over the last few seconds
    public float Intensity => Mathf.Clamp01((_blindedUntil - Time.time) / _fadeTime);

    public void Blind(float seconds)
    {
        _blindedUntil = Mathf.Max(_blindedUntil, Time.time + seconds);
    }
}
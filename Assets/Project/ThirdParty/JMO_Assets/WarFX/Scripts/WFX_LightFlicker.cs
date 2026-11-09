using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class WFX_LightFlicker : MonoBehaviour
{
    [Tooltip("Seconds the light stays on. 0.05 is about three frames at 60 fps.")]
    [SerializeField] private float duration = 0.05f;
    [Tooltip("Fade the intensity down over the duration instead of cutting off.")]
    [SerializeField] private bool fade = false;

    private Light _light;
    private float _startIntensity;
    private float _elapsed;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _startIntensity = _light.intensity;
    }

    private void OnEnable()
    {
        _elapsed = 0f;
        _light.intensity = _startIntensity;
        _light.enabled = true;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_elapsed >= duration)
        {
            _light.enabled = false;
            enabled = false;   // nothing left to do, stop running Update
            return;
        }

        if (fade)
            _light.intensity = Mathf.Lerp(_startIntensity, 0f, _elapsed / duration);
    }
}

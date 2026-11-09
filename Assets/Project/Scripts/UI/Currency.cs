using UnityEngine;
using UnityEngine.UIElements;

public class Currency : MonoBehaviour
{
    public int currency = 0;
    public int incrementPerSecond = 1;
    private Label _currencyLabel;
    private float _timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _currencyLabel = root.Q<Label>("CurrencyLabel");

        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= 1f)
        {
            currency += incrementPerSecond;
            _timer = 0f;
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        _currencyLabel.text = "$: " + currency;
    }
}

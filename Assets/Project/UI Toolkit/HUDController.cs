using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Display Settings")]
    [SerializeField] private int magazineSegments = 10;
    [SerializeField] private float reloadDisplayDuration = 2f;
    [SerializeField] private float gadgetDisplayDuration = 1.25f;

    private VisualElement reloadContainer;
    private VisualElement magazineGraphic;
    private VisualElement gadgetContainer;
    private VisualElement gadgetList;

    private Label magazineAmmo;
    private Label ammoType;

    private readonly List<VisualElement> ammoSegments = new();
    private readonly List<VisualElement> gadgetSlots = new();
    private readonly List<Label> gadgetCounts = new();

    private bool ready;

    /// <summary>Called by the Players Start and not Awake due to UIDocuments building the visual tree in its own OnEnable</summary>
    public void Initialise() {
        if (uiDocument == null) {
            Debug.LogError("HUD: the UIDocument slot on HUDController is empty.", this);
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        if (root == null) {
            Debug.LogError("HUD: the UIDocument has no root. Assign a Panel Settings asset, make sure the UIDocument component is enabled and its GameObject is active.", this);
            return;
        }

        reloadContainer = root.Q<VisualElement>("ReloadContainer");
        magazineGraphic = root.Q<VisualElement>("MagazineGraphic");
        magazineAmmo = root.Q<Label>("MagazineAmmo");
        ammoType = root.Q<Label>("AmmoType");
        gadgetContainer = root.Q<VisualElement>("GadgetContainer");
        gadgetList = root.Q<VisualElement>("GadgetList");

        if (reloadContainer == null || magazineGraphic == null || magazineAmmo == null
            || gadgetContainer == null || gadgetList == null) {
            Debug.LogError("HUD: could not find all named elements. Check the UIDocument has HUD.uxml as its Source Asset and a Panel Settings, and that the names match.", this);
            return;
        }

        ready = true;

        CreateMagazineGraphic();
        HideReloadUI();
        HideGadgetUI();
    }

    /// <summary>Re create the ammo graphic everytime its called as its called when reloading is called by the player.</summary>
    private void CreateMagazineGraphic(){
        magazineGraphic.Clear();
        ammoSegments.Clear();

        for (int i = 0; i < magazineSegments; i++) {
            VisualElement segment = new VisualElement();
            segment.AddToClassList("ammo-segment");

            magazineGraphic.Add(segment);
            ammoSegments.Add(segment);
        }
    }

    public void ShowReloadUI(int currentAmmo, int magazineCapacity, string ammoTypeName = "") {
        if (!ready)
            return;

        // Cancel a pending hide so it doesn't vanish mid-reload
        CancelInvoke(nameof(HideReloadUI));

        if (ammoType != null && !string.IsNullOrEmpty(ammoTypeName))
            ammoType.text = ammoTypeName;

        reloadContainer.AddToClassList("visible");
        UpdateReloadAmmo(currentAmmo, magazineCapacity);
    }

    public void UpdateReloadAmmo(int currentAmmo, int magazineCapacity) {
        if (!ready)
            return;

        magazineAmmo.text = $"{currentAmmo} / {magazineCapacity}";

        // Ceil, so any ammo at all shows at least one segment
        int filledSegments = magazineCapacity > 0
            ? Mathf.CeilToInt((float)currentAmmo / magazineCapacity * magazineSegments)
            : 0;

        filledSegments = Mathf.Clamp(filledSegments, 0, magazineSegments);

        for (int i = 0; i < ammoSegments.Count; i++) {
            if (i < filledSegments)
                ammoSegments[i].RemoveFromClassList("empty");
            else
                ammoSegments[i].AddToClassList("empty");
        }
    }

    // Leaves the panel up for a moment after the reload, then hides it
    public void HideReloadUIAfterDelay() {
        if (!ready)
            return;

        CancelInvoke(nameof(HideReloadUI));
        Invoke(nameof(HideReloadUI), reloadDisplayDuration);
    }

    public void HideReloadUI() {
        if (!ready)
            return;

        reloadContainer.RemoveFromClassList("visible");
    }

    // Gadget Display
    public void BuildGadgetList(IReadOnlyList<Equippable> gadgets) {
        if (!ready)
            return;

        gadgetList.Clear();
        gadgetSlots.Clear();
        gadgetCounts.Clear();

        foreach (Equippable gadget in gadgets) {
            VisualElement slot = new VisualElement();
            slot.AddToClassList("gadget-slot");

            VisualElement icon = new VisualElement();
            icon.AddToClassList("gadget-icon");

            if (gadget.Icon != null)
                icon.style.backgroundImage = gadget.Icon;

            Label nameLabel = new Label(gadget.DisplayName);
            nameLabel.AddToClassList("gadget-name");

            Label countLabel = new Label(FormatCharges(gadget.Charges));
            countLabel.AddToClassList("gadget-count");

            slot.Add(icon);
            slot.Add(nameLabel);
            slot.Add(countLabel);

            gadgetList.Add(slot);
            gadgetSlots.Add(slot);
            gadgetCounts.Add(countLabel);
        }
    }

    // Call after a gadget is used so the "x 2" text updates
    public void UpdateGadgetCounts(IReadOnlyList<Equippable> gadgets) {
        if (!ready)
            return;

        for (int i = 0; i < gadgetCounts.Count && i < gadgets.Count; i++)
            gadgetCounts[i].text = FormatCharges(gadgets[i].Charges);
    }

    public void ShowGadgetSelection(int selectedIndex) {
        if (!ready)
            return;

        gadgetContainer.AddToClassList("visible");
        UpdateSelectedGadget(selectedIndex);

        CancelInvoke(nameof(HideGadgetUI));
        Invoke(nameof(HideGadgetUI), gadgetDisplayDuration);
    }

    public void UpdateSelectedGadget(int selectedIndex) {
        for (int i = 0; i < gadgetSlots.Count; i++) {
            if (i == selectedIndex)
                gadgetSlots[i].AddToClassList("selected");
            else
                gadgetSlots[i].RemoveFromClassList("selected");
        }
    }

    public void HideGadgetUI()
    {
        if (!ready)
            return;

        gadgetContainer.RemoveFromClassList("visible");
    }

    // Tools (hammer, ram) return -1 and show no count
    private static string FormatCharges(int charges)
    {
        return charges >= 0 ? $"× {charges}" : string.Empty;
    }
}
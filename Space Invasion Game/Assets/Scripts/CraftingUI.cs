using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
struct ImageText
{
    public Image imageComponent;
    public Text textComponent;

    public void SetColorAll(Color color)
    {
        if (imageComponent) imageComponent.color = color;
        if (textComponent) textComponent.color = color;
    }

    public void SetColorTheme(StationTheme theme)
    {
        if (imageComponent) imageComponent.color = theme.mainColor;
        if (textComponent) textComponent.color = theme.secondaryColor;
    }
}

[Serializable]
struct Browser
{
    public GameObject self;
    public ImageButton subRootButton;
    public ImageButton[] materialButtons;
}

[Serializable]
struct RootPanel
{
    public GameObject invalidRootLink;
    public GameObject validRootLink;
    public GameObject rootButtonPanel;
    public ImageButton rootButton;

    public void ShowRootPanel(bool show, bool valid)
    {
        validRootLink.SetActive(show && valid);
        invalidRootLink.SetActive(show && !valid);
        rootButtonPanel.SetActive(show);
    }
}

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI instance;

    [Header("Settings")]
    [SerializeField] StationTheme currentTheme;
    [SerializeField] List<ImageButton> recipeButtons;

    [Header("Required Components")]
    [SerializeField] GameObject craftingWindow;
    [SerializeField] GameObject recipeBrowser;
    [SerializeField] GameObject materialPanel;
    [SerializeField] GameObject craftbuttonPanel;
    [SerializeField] GameObject instruction;
    [SerializeField] GameObject imageButtonPrefab;

    [Header("Colorable Panels")]
    [SerializeField] Image craftingWindowImage;
    [SerializeField] Image recipeBrowserImage;
    [SerializeField] Image materialBrowserImage;
    [SerializeField] ImageText titlePanel;
    [SerializeField] ImageText craftButton;

    [Header("Root")]
    [SerializeField] RootPanel rootPanel;

    [Header("Browsers")]
    [SerializeField] Browser[] matBrowsers;

    public bool isVisible { get { return craftingWindow.activeInHierarchy; } }

    private Dictionary<Item, int> inventory = new Dictionary<Item, int>();

    private PlayerStatus playerStatus;
    private PlayerInventory playerInventory;
    private Item rootItem;

    private bool recipleSelected;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        recipleSelected = false;

        titlePanel.textComponent.text = "Fabricator"; // TODO: put this into StationTheme struct
        titlePanel.SetColorTheme(currentTheme);

        craftingWindowImage.color = currentTheme.mainColorWA;
        recipeBrowserImage.color = currentTheme.mainColor;
        materialBrowserImage.color = currentTheme.mainColor;

        materialPanel.SetActive(recipleSelected);
        craftbuttonPanel.SetActive(recipleSelected);
        instruction.SetActive(!recipleSelected);
    }

    public void DisplayRecipe(Item itemRecipe)
    {
        var matCount = itemRecipe.requiredMaterials.Count;
        if (matCount <= 0) return;

        if(rootItem != null)
        {
            if(rootItem == itemRecipe)
            {
                rootPanel.ShowRootPanel(false, true);
            }
            else if (rootItem.CheckItemRequirement(itemRecipe))
            {
                rootPanel.ShowRootPanel(true, true);
                rootPanel.rootButton.SetItemRoot(rootItem, GetQuantity(rootItem));
            }
            else
            {
                rootPanel.ShowRootPanel(false, true);
            }
        }
        else
        {
            rootPanel.ShowRootPanel(false, true);
        }

        if (instruction.activeInHierarchy) instruction.SetActive(false);
        if (!materialPanel.activeInHierarchy) materialPanel.SetActive(true);
        if (!craftbuttonPanel.activeInHierarchy) craftbuttonPanel.SetActive(true);

        ActivateMaterialPanel(matCount);
        var browser = matBrowsers[matCount - 1];
        browser.subRootButton.SetItemSubroot(itemRecipe, GetQuantity(itemRecipe));
        rootItem = itemRecipe;

        for (int i = 0; i < matCount; i++)
        {
            var material = itemRecipe.requiredMaterials[i];

            browser.materialButtons[i].SetItemRequirement(material.item, GetQuantity(material.item), material.count);
        }
    } 

    public void Craft()
    {
        if (rootItem == null) return;

        playerInventory.Craft(rootItem);
    }

    public void Close()
    {
        playerStatus.Stun(false);
        craftingWindow.SetActive(false);
    }

    [Client]
    public void NotifyInventoryUpdate(Dictionary<Item, int> newInventory)
    {
        inventory = newInventory;

        int index = 0;

        foreach (KeyValuePair<Item, int> kvp in inventory)
        {
            if (kvp.Key.requiredMaterials.Count <= 0) continue;

            if (index < recipeButtons.Count)
            {
                if (!recipeButtons[index].gameObject.activeInHierarchy)
                    recipeButtons[index].gameObject.SetActive(true);

                recipeButtons[index].SetItem(kvp.Key, kvp.Value);
            }
            else
            {
                var button = Instantiate(imageButtonPrefab, recipeBrowser.transform);
                var imageButton = button.GetComponent<ImageButton>();
                imageButton.SetItem(kvp.Key, kvp.Value);
                recipeButtons.Add(imageButton);
            }
            index++;
        }

        index++;

        if( index< recipeButtons.Count)
        {
            for(int i = index; i < recipeButtons.Count; i++)
            {
                recipeButtons[i].gameObject.SetActive(false);
            }
        }

        if (rootItem != null)
            DisplayRecipe(rootItem);
    }

    private int GetQuantity(Item item)
    {
        return inventory.ContainsKey(item) ? inventory[item] : 0;
    }

    private void ActivateMaterialPanel(int count)
    {
        for (int i = 0; i < matBrowsers.Length; i++)
            matBrowsers[i].self.SetActive(count == i + 1);
    }

    public void SetVisibility(bool isVisible)
    {
        craftingWindow.SetActive(isVisible);
        if (isVisible)
            playerInventory.CraftingUIRequestInventoryUpdate();
    }

    [Client]
    public void SetPlayerInventory(PlayerInventory playerInventory)
    {
        this.playerInventory = playerInventory;
    }

    [Client]
    public void SetPlayerStatus(PlayerStatus playerStatus)
    {
        this.playerStatus = playerStatus;
    }


}

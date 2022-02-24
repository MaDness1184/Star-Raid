using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

enum ImageButtonOnClickBehaviour
{
    DisplayRecipe,

}

public class ImageButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    const string DESCRIPTION_COLOR_CODE = "#fbf236";

    [Header("Required Components")]
    [SerializeField] private bool subroot = false;
    [SerializeField] private Image _buttonImageComponent;
    [SerializeField] private Image _contentImageComponent;
    [SerializeField] private Text _textComponent;
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;

    [Header("Debugs")]
    [SerializeField] private Item currentItem;
    [TextArea]
    [SerializeField] private string tooltipBuilder;

    public Image buttonImageComponent { get { return _buttonImageComponent; } }
    public Image contentImageComponent { get { return _contentImageComponent; } }
    public Text textComponent { get { return _textComponent; } }

    /*public ImageButton(ItemCountObsolete itemCount)
    {
        SetItemCount(itemCount);
    }*/

    public void SetItem(Item item, int quantity)
    {
        currentItem = item;
        _contentImageComponent.sprite = item.sprite;
        _textComponent.text = "x" + (item.resultComponents.Count == 0 ?
            0 : item.resultComponents[0].count);

        BuildTooltip(item, quantity);
    }

    public void SetItemRoot(Item item, int quantity)
    {
        currentItem = item;
        _contentImageComponent.sprite = item.sprite;

        BuildTooltip(item, quantity);
    }

    private void BuildTooltip(Item item, int quantity)
    {
        tooltipBuilder = $"<size=120%><color=#{ColorUtility.ToHtmlStringRGBA(item.tooltipColor)}><b>{item.name}</b></color></size>" +
            $"\nQuantity: {quantity}" +
            $"\n" +
            $"\n<color={DESCRIPTION_COLOR_CODE}><i>\"{item.description}\"</i></color>" +
            $"\n" +
            $"\n" +
            (item.requiredMaterials.Count > 0 ? $"<size=90%>Click to view recipe</size>" : $"Uncraftable item") +
            $"\n";
    }

    public void SetItemRequirement(Item item, int quantity, int requirement)
    {
        currentItem = item;
        _contentImageComponent.sprite = item.sprite;
        _textComponent.text = quantity + "/" + requirement;
        _textComponent.color = quantity < requirement ? invalidColor : validColor;
        _buttonImageComponent.color = _textComponent.color;

        BuildRequirement(item, quantity, requirement);
    }

    private void BuildRequirement(Item item, int quantity, int requirement)
    {
        tooltipBuilder = $"<size=120%><color=#{ColorUtility.ToHtmlStringRGBA(item.tooltipColor)}><b>{item.name}</b></color></size>" +
            $"\nAvailable: {quantity}" +
            $"\nRequire: {requirement}" +
            $"\n" +
            $"\n<color={DESCRIPTION_COLOR_CODE}><i>\"{item.description}\"</i></color>" +
            $"\n" +
            $"\n" +
            (item.requiredMaterials.Count > 0 ? $"<size=90%>Click to view recipe</size>" : $"Uncraftable item") +
            $"\n";
    }

    public void SetItemSubroot(Item item, int quantity)
    {
        currentItem = item;
        _contentImageComponent.sprite = item.sprite;
        _textComponent.text = "x" + (item.resultComponents.Count == 0 ?
            0 : item.resultComponents[0].count);

        BuildTooltipSubroot(item, quantity);
    }

    private void BuildTooltipSubroot(Item item, int quantity)
    {
        tooltipBuilder = $"<size=120%><color=#{ColorUtility.ToHtmlStringRGBA(item.tooltipColor)}><b>{item.name}</b></color></size>" +
            $"\nQuantity: {quantity}" +
            $"\n" +
            $"\n<color={DESCRIPTION_COLOR_CODE}><i>\"{item.description}\"</i></color>" +
            $"\n" +
            $"\nClick Craft to craft {(item.resultComponents.Count == 0 ? 0 : item.resultComponents[0].count)} units" +
            $"\n";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ToolTip.ShowTooltip(tooltipBuilder);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ToolTip.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!subroot)
            CraftingUI.instance.DisplayRecipe(currentItem);
    }
}

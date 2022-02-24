using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

enum RelativePosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Uninitialized,
}

public class ToolTip : MonoBehaviour
{
    public static ToolTip instance;

    [SerializeField] private RectTransform mainCanvas;
    [SerializeField] private RectTransform tooltipBackground;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [SerializeField] private Sprite topRightSprite;
    [SerializeField] private Sprite topLeftSprite;
    [SerializeField] private Sprite bottomLeftSprite;
    [SerializeField] private Sprite bottomRightSprite;
    private RectTransform tooltipTransform;

    bool show;
    Color backgroundColorCache;

    Vector2 mousePosCache = new Vector2(0, 0);
    Vector2 mouseOffset = new Vector2(0, 0);
    Vector2 posOffset = new Vector2(0, 0);
    //Vector2 offsetCache = new Vector2(0, 0);

    Vector2 mouseTopRightOffset = new Vector2(-5, -5);
    Vector2 mouseTopLeftOffset = new Vector2(5, -5);
    Vector2 mouseBottomLeftOffset = new Vector2(5, 5);
    Vector2 mouseBottomRightOffset = new Vector2(-5, 5);

    RelativePosition relativePosition = RelativePosition.Uninitialized;
    RelativePosition cache = RelativePosition.Uninitialized;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        tooltipTransform = GetComponent<RectTransform>();
        backgroundColorCache = backgroundImage.color;
        /*SetText("<color=#76428a><b>Scrum Jelly</b></color>" +
            "\n" +
            "\nIn inventory: 12" +
            "\nCost 2 to craft Green Cube" +
            "\n" +
            "\n<color=#fbf236><i>'Strange but somewhat useful material'</i></color>" +
            "\n" +
            "\nClick to see recipe " +
            "\n(placeholder)" +
            "\n");*/
        //SetText("Simple smol tooltip");
        HideTooltip();
    }

    private void SetText(string newText)
    {
        tooltipText.text = newText;
        tooltipText.ForceMeshUpdate();

        var paddingSize = new Vector2(tooltipText.margin.x * 2,
            tooltipText.margin.y * 2);
        var textSize = tooltipText.GetRenderedValues(false);
        tooltipBackground.sizeDelta = textSize + paddingSize;
    }

    private void Update()
    {
        if (!show) return;

        mousePosCache = Mouse.current.position.ReadValue();

        // Mouse position anchored to bottom left for some freaking reason?!?!?
        if (mousePosCache.x < Screen.width - tooltipBackground.rect.width * mainCanvas.localScale.x)
        {
            if(mousePosCache.y < 5 + tooltipBackground.rect.height * mainCanvas.localScale.x)
            {
                //Bottom left
                relativePosition = RelativePosition.BottomLeft;
                if(relativePosition != cache)
                {
                    cache = relativePosition;
                    backgroundImage.sprite = bottomLeftSprite;
                    mouseOffset = mouseBottomLeftOffset;
                }

                posOffset = new Vector2(0, 0);
            }
            else
            {
                //Top left
                relativePosition = RelativePosition.TopLeft;
                if (relativePosition != cache)
                {
                    cache = relativePosition;
                    backgroundImage.sprite = topLeftSprite;
                    mouseOffset = mouseTopLeftOffset;
                }

                posOffset = new Vector2(0, -tooltipBackground.rect.height);
            }
        }
        else
        {
            if (mousePosCache.y < 5 + tooltipBackground.rect.height * mainCanvas.localScale.x)
            {
                //Bottom right
                relativePosition = RelativePosition.BottomRight;
                if (relativePosition != cache)
                {
                    cache = relativePosition;
                    backgroundImage.sprite = bottomRightSprite;
                    mouseOffset = mouseBottomRightOffset;
                }

                posOffset = new Vector2(-tooltipBackground.rect.width, 0);
            }
            else
            {
                //Top right
                relativePosition = RelativePosition.TopRight;
                if (relativePosition != cache)
                {
                    cache = relativePosition;
                    backgroundImage.sprite = topRightSprite;
                    mouseOffset = mouseTopRightOffset;
                }

                posOffset = new Vector2(-tooltipBackground.rect.width, -tooltipBackground.rect.height);
            }
        }

        var anchoredPosition = Mouse.current.position.ReadValue() / mainCanvas.localScale.x + posOffset + mouseOffset;

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, 0, mainCanvas.rect.width - tooltipBackground.rect.width);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 0, mainCanvas.rect.height - tooltipBackground.rect.height);

        tooltipTransform.anchoredPosition = anchoredPosition;
    }

    private void InternalShowTooltip(string text)
    {
        SetText(text);
        tooltipText.color = Color.white;
        backgroundImage.color = backgroundColorCache;
        show = true;
    }

    private void InternalHideTooltip()
    {
        //gameObject.SetActive(false);
        tooltipText.color = Color.clear;
        backgroundImage.color = Color.clear;
        show = false;
    }

    public static void ShowTooltip(string text)
    {
        instance.InternalShowTooltip(text);
    }

    public static void HideTooltip()
    {
        instance.InternalHideTooltip();
    }
}

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridHighlight : MonoBehaviour
{
    public static GridHighlight main;

    [Header("Settings")] 
    [SerializeField] private float highlightRate = 1;
    [SerializeField]
    [Range(0, 1)] private float maxAlpha = 0.9f;
    [SerializeField] 
    [Range(0, 1)] private float minAlpha = 0.1f;
    [SerializeField] private Color ValidColor = Color.green;
    [SerializeField] private Color InvalidColor = Color.red;

    [Header("Debugs")]
    [SerializeField] private bool valid = true;
    [SerializeField] private bool show = true;

    private SpriteRenderer spriteRenderer;
    private Color currentC;
    [SerializeField] private float currentAlpha = 1;
    [SerializeField] private float direction = -1;

    private void Awake()
    {
        if (main == null)
            main = this;
        else
            Destroy(gameObject);

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        spriteRenderer.color = Color.clear;
    }

    [Client]
    public void SetShow(bool newShow)
    {
        show = newShow;

        if(!show)
            spriteRenderer.color = Color.clear;
    }

    [Client]
    public void SetValid(bool newValid)
    {
        valid = newValid;
        currentC = valid ? ValidColor : InvalidColor;
    }

    [Client]
    public void MoveTo(Vector3 newPosition)
    {
        transform.position = 
            new Vector3(Mathf.RoundToInt(newPosition.x), Mathf.RoundToInt(newPosition.y));
    }

    [ClientCallback]
    private void Update()
    {
        if (show)
        {
            currentAlpha = currentAlpha + direction * highlightRate * Time.deltaTime;
            currentAlpha = Mathf.Clamp(currentAlpha, minAlpha, maxAlpha);
            spriteRenderer.color = new Color(currentC.r, currentC.g, currentC.b, currentAlpha);

            if (currentAlpha >= maxAlpha)
                direction = -1;
            
            if(currentAlpha <= minAlpha) 
                direction = 1;
        }
    }
}

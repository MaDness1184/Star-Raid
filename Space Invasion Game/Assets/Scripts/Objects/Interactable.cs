using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ShaderController))]
public class Interactable : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private Sprite pickUpSprite;

    private ShaderController shaderController;

    private void Start()
    {
        shaderController = GetComponent<ShaderController>();
    }

    [Client]
    public void LocalHighlight(bool highlight)
    {
        shaderController.LocalHighlight(highlight);
    }

    public Sprite GetPickupSprite()
    {
        return pickUpSprite;
    }
}

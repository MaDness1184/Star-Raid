using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Placeable : Interactable
{
    [Header("Required Components")]
    [SerializeField] private Sprite _pickUpSprite;
    public Sprite pickUpSprite { get { return _pickUpSprite; } }

}

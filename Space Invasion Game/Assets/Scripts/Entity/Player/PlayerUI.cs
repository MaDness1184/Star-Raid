using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private const string HEART_SYMBOL = "♥";

    public static PlayerUI instance;

    private Text playerUIText;

    private string playerName;
    private string heartBuilder;
    private string inventoryString;

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        playerUIText = GetComponent<Text>();
    }

    public void UpdateText()
    {
        playerUIText.text = playerName + "\n" + heartBuilder + "\n" + inventoryString;
    }

    public void SetPlayerName(string newName)
    {
        playerName = newName;
        UpdateText();
    }

    public void SetPlayerHP(int newHP)
    {
        heartBuilder = "";
        for (int i = 0; i < newHP; i++)
            heartBuilder += HEART_SYMBOL;

        UpdateText();
    }
    public void SetInventoryString(string newInventoryString)
    {
        inventoryString = newInventoryString;
        UpdateText();
    }
}

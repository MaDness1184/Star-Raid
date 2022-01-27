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
    private int playerHP;
    private string heartBuilder;

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

    public void updateText(string text)
    {
        playerUIText.text = playerName + "\n" + heartBuilder + "\n" +text;
    }

    public void addText(string text)
    {
        playerUIText.text += text;
    }

    public void SetPlayerName(string newName)
    {
        playerName = newName;
    }

    public void SetPlayerHP(int newHP)
    {
        playerHP = newHP;

        heartBuilder = "";
        for (int i = 0; i < newHP; i++)
            heartBuilder += HEART_SYMBOL;
    }
}

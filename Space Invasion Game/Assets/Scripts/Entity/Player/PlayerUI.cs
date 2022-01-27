using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI instance;

    private Text playerUIText;

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
        playerUIText.text = text;
    }

    public void addText(string text)
    {
        playerUIText.text += text;
    }


}

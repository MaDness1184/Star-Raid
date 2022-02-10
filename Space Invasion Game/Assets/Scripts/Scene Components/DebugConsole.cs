using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DebugConsole : MonoBehaviour
{
    [SerializeField] private bool logToFile = false;

    [SerializeField] private GameObject debugUI;
    [SerializeField] private Text debugText;
    [SerializeField] private InputField inputField;

    public static DebugConsole main;

    private PlayerStatus playerStatus;

    private static string staticText = "";
    private static string cachedStaticText = "";
    private static string warningText = "";
    private static string updateText = "";

    private static string logText = "";
    private string filename = "";

    private Resolution[] resolutions;
    private int lastResIndex;
    private string logName = "";

    private void Awake()
    {
        if (main == null)
            main = this;
        else
            Destroy(gameObject);

#if UNITY_EDITOR
        logToFile = false;
#endif

        resolutions = Screen.resolutions;
        lastResIndex = resolutions.Length - 1;

        logName = DateTime.Today.ToString("MM") +
           DateTime.Today.ToString("dd") +
           DateTime.Today.ToString("yy") + "-" +
           DateTime.Now.ToString("HH") +
           DateTime.Now.ToString("mm") +
           DateTime.Now.ToString("ss") + "-" +
           Random.Range(1000, 9999).ToString();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        ShowConsole(false);
    }

    void OnEnable()
    {
        Application.logMessageReceived += InternalLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= InternalLog;
    }

    private void LateUpdate()
    {
        debugText.text = (staticText.Length > 0 ? "\n>> Log <<\n" + staticText : "") +
           (warningText.Length > 0 ? "\n>> Warning <<\n" + warningText : "" +
           (updateText.Length > 0 ? "\n>> Update <<\n" + updateText : "") +
           (logText.Length > 0 ? "\n>> Console <<\n" + logText : ""));

        updateText = "";
    }

    #region Log Methods - How to log to debug console

    // Same as Debug.Log. This will log the text to the UI console, the editor console and 
    //  to file if enabled
    // Use: DebugConsole.Log("text");
    public static void Log(string text)
    {
        staticText += "\n" + text;
        cachedStaticText = text;
        Debug.Log(text);
    }

    // Clear all static log 
    public static void ClearLog()
    {
        staticText = "";
    }

    // Used for value that constantly change per Update like velocity
    // Use: DebugConsole.LogUpdate("Current velocity = " + velocity);
    public static void LogUpdate(string text)
    {
        updateText += "\n" +text;
    }

    // Same as Debug.LogWarning
    // Use: DebugConsole.LogWarning("text");
    public static void LogWarning(string warning, string name = "Undifined")
    {
        string builder = "\nWarning::" + name + ": " + warning;
        warningText += builder;
        Debug.LogWarning(builder);
    }

    public void InternalLog(string logString, string stackTrace, LogType type)
    {
        if (logToFile)
        {
            if (filename == "")
            {
                string directory = System.Environment.GetFolderPath(
                   System.Environment.SpecialFolder.Desktop) + "/Star Raid Log";

                System.IO.Directory.CreateDirectory(directory);

                filename = directory + "/log-" + logName + ".txt";
            }
            try { System.IO.File.AppendAllText(filename, logString + "\n"); }
            catch { }
        }

        if (logString == cachedStaticText)
        {
            cachedStaticText = "";
        }
        else
        {
            logText = logString + "\n" + logText;
        }
    }

    #endregion

    #region RunCommands Methods - Modify or add command here

    // Rules:
    // 1. A Command can only have max 2 keywords [COMMAND] [ARGUMENT]
    // Example: JumpToLevel 2
    // 2. A Command could only need 1 keyword to run
    // Example: Fullscreen
    // 3. Commands are case insensitive
    // 4. input[0] will always contain the command,
    //    input[1] will always contain the argument
    // 5. A Command can have multiple shorthand name

    public void RunCommand()
    {
        if (!debugUI.activeInHierarchy) return;

        inputField.Select();
        Log($"> {inputField.text}");

        string[] input = inputField.text.ToLower().Split(' ');
        inputField.text = "";
        bool foundSingle = true;
        bool foundDouble = true;

        // Case for one keyword only, do not put command with 2 keywords here
        switch (input[0]) 
        {
            case "command":
                // Function
                break;
            case "disconnect":
                Disconnect();
                break;
            case "full":
            case "fullscreen":
                Screen.SetResolution(resolutions[lastResIndex].width, resolutions[lastResIndex].height, true);
                break;
            case "fullscreen-windowed":
                Screen.SetResolution(resolutions[lastResIndex].width, resolutions[lastResIndex].height, false);
                break;
            case "win":
            case "windowed":
                Screen.SetResolution(1024, 576, false);
                break;
            case "quit":
                Application.Quit();
                break;
            case "help":
                Log("Type in the input field below then press [Run] button or [Enter] to run" +
                    "\nType 'Commands' for full list of commands");
                break;
            case "commands":
                Log("List of commands:" +
                    "\n" +
                    "\n     LogToFile [bool]" +
                    "\n     EnemySpawn [bool]" +
                    "\n     SetSpawnLimit [int]" +
                    "\n     GodMode [bool]" +
                    "\n     VulnerableMode [bool]" +
                    "\n     Fullscreen" +
                    "\n     Fullscreen-Windowed" +
                    "\n     Windowed" +
                    "\n     Quit" +
                    "\n");
                break;
            default:
                foundSingle = false;
                break;
        }

        if (input.Length <= 1)
        {
            if (!foundSingle)
                Log("Command not found (missing argument?)");

            return;
        }

        // Case for two keywords
        // Check rule #4
        // Case is for command, check input[1] for argument
        // Use CheckArgumentBool(string input, out bool output)
        //  to automatically log bad output to the console
        switch (input[0])
        {
            // Command example: setspawnlimit 10
            case "setspawnlimit":
                if (CheckArgumentInt(input[1], out int limit))
                {
                    GameManager.instance.SetSpawnLimit(limit);
                    Log($"Spawn limit set to {limit}");
                }  
                break;
            // Command example: logtofile true
            case "logtofile":
                if (CheckArgumentBool(input[1], out logToFile))
                    Log($"Log to file = {logToFile}");
                break;
            case "enemyspawn":
                if (CheckArgumentBool(input[1], out bool enable))
                {
                    GameManager.instance.EnableEnemySpawn(enable);
                    Log($"Enemy spawn = {enable}");
                }
                break;
            case "godmode":
                if (CheckArgumentBool(input[1], out enable))
                {
                    playerStatus.EnableGodMode(enable);
                    Log($"Godmode = {enable}");
                }
                break;
            case "vulnerablemode":
                if (CheckArgumentBool(input[1], out enable))
                {
                    playerStatus.EnableVulnerableMode(enable);
                    Log($"Godmode = {enable}");
                }
                break;
            default:
                foundDouble = false;
                break;
        }

        if(!foundDouble) Log("Command not found (reached end of list)");
    }

    #endregion

    #region Support methods go here

    private void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
        }
    }

    public void ShowConsole(bool show)
    {
        debugUI.SetActive(show);
        if (show)
        {
            inputField.Select();
            if (playerStatus)
                playerStatus.Stun(true);
        }
        else
        {
            if (playerStatus)
                playerStatus.Stun(false);
        }
    }

    public void ToggleConsole()
    {
        ShowConsole(!debugUI.activeInHierarchy);
    }

    private bool CheckArgumentBool(string input, out bool output)
    {
        if (bool.TryParse(input, out output))
        {
            return true;
        }
        else
        {
            Log($"Bad argument: Cannot parse '{input}' to [bool]");
            return true;
        }
    }

    private bool CheckArgumentInt(string input, out int output)
    {
        if (int.TryParse(input, out output))
        {
            return true;
        }
        else
        {
            Log($"Bad argument: Cannot parse '{input}' to [int]");
            return true;
        }
    }

    public void SetPlayer(PlayerStatus playerStatus)
    {
        this.playerStatus = playerStatus;
    }

    #endregion
}

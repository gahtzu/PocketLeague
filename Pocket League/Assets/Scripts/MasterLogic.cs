using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MasterLogic : MonoBehaviour
{
    #region Inspector Properties
    [Header("Misc")]
    [Tooltip("Speed of the player... duh")]
    [SerializeField]
    public float playerSpeed;
    [Tooltip("When applying DI to the knockback angle, how much should it be considered?\n1 = you can have a 45 degree influence.\n.5 = you can have half of 45 degree influence, etc.")]
    [SerializeField]
    public float DirectionalInfluenceMultiplier;
    [Tooltip("Number of stocks that a player starts with.")]
    [SerializeField]
    public int stockCount;
    [Tooltip("Should both players reset their percent after a stock is taken?")]
    [SerializeField]
    public bool ResetPercentOnKill;
    [Tooltip("How many pixels does the percentages string offset from the center of the player?")]
    [SerializeField]
    public float percentCharacterOffsetX;
    [Tooltip("1=smallest, 3=biggest")]
    [SerializeField]
    public int tableSize;

    [Header("Prefabs")]
    public GameObject playerObj;
    #endregion

    #region Variables
    [HideInInspector]
    private List<PocketPlayerController> players = new List<PocketPlayerController>();
    [HideInInspector]
    public bool viewDebug = false;
    [HideInInspector]
    public GameStateMachine gameStateMachine = new GameStateMachine();
    private int frameCount = 0, victoryPlayer = 1;
    private float nextUpdate = 0.0f,
                  fps = 0.0f,
                  wordBoxOffsetX,
                  updateRate = 1f;  // 1 update per sec.
    private List<Vector3> spawnPositions = new List<Vector3>();
    private Camera mainCamera;
    private Text goText;
    #endregion

    void Awake()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;
        nextUpdate = Time.time;
    }

    void Start()
    {
        goText = GameObject.Find("GoText").GetComponent<Text>();
        SetGoTextProperties(1, "", goText.color);
        spawnPositions.AddRange(new List<Vector3>() { new Vector3(-10f, .5f, 0f), new Vector3(10f, .5f, 0f) });

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Table"))
            go.SetActive(go.name.Contains(tableSize.ToString()));

        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

        gameStateMachine.Subscribe(Countdown, GameStateId.Countdown, true);
        gameStateMachine.Subscribe(Battle, GameStateId.Battle, true);
        gameStateMachine.Subscribe(Death, GameStateId.Death, true);
        gameStateMachine.Subscribe(Results, GameStateId.Results, true);

        for (int i = 1; i < 3; i++)
            CreatePlayer(i);

        players[0].otherPlayer = GameObject.Find("Player 2");
        players[1].otherPlayer = GameObject.Find("Player 1");

        StartCoroutine("CountDown");
    }

    void Countdown() { }
    void Battle() { }
    void Death() { }
    void Results() { }

    public void CreatePlayer(int id)
    {
        GameObject newPlayer = GameObject.Instantiate(playerObj, spawnPositions[id - 1], Quaternion.identity) as GameObject;
        newPlayer.name = "Player " + id;

        PocketPlayerController newController = newPlayer.GetComponent<PocketPlayerController>();
        newController.InitializePlayer(id);
        newController.playerDetails.stocks = stockCount;

        players.Add(newController);
    }

    public void GetReadyForCountDown(PocketPlayerController player)
    {
        player.model.transform.localScale = new Vector3(1.173f, 1.173f, 1.173f);
        player.model.SetActive(true);

        foreach (PocketPlayerController _player in players)
        {
            _player.stateMachine.ChangeState(PlayerState.Actionable);
            _player.StopAllCoroutines();
        }

        player.gameObject.transform.position = spawnPositions[player.playerDetails.id - 1];
        player.otherPlayer.transform.position = spawnPositions[player.otherPlayer.GetComponent<PocketPlayerController>().playerDetails.id - 1];
        player.playerDetails.percent = 0f;
        if (ResetPercentOnKill)
            player.otherPlayer.GetComponent<PocketPlayerController>().playerDetails.percent = 0f;


        StopCoroutine("CountDown");

        StartCoroutine("CountDown");
    }

    private IEnumerator CountDown()
    {
        gameStateMachine.ChangeState(GameStateId.Countdown);

        int framesUntilNextNumber = 125, startingSize = 120;
        float incrementToDecreaseSize = .3f, incrementTotal = 0f;
        SetGoTextProperties(startingSize, "3", Color.white);
        Color startingColor = goText.color;

        //3.. 2.. 1..
        for (int j = 3; j > 0; j--)
        {
            SetGoTextProperties(startingSize, j.ToString());
            for (int i = 0; i < framesUntilNextNumber; i++)
            {
                incrementTotal += incrementToDecreaseSize;
                SetGoTextProperties(startingSize - Mathf.FloorToInt(incrementTotal));
                if (isGameStateActive(GameStateId.Countdown)) { yield return new WaitForEndOfFrame(); }
            }
            incrementTotal = 0f;
        }

        //..GO!
        gameStateMachine.ChangeState(GameStateId.Battle);
        SetGoTextProperties(140, "GO!", Color.green);
        for (int i = 0; i < framesUntilNextNumber; i++) { yield return new WaitForEndOfFrame(); }
        SetGoTextProperties(0, "", startingColor);
    }


    public void KillPlayer(PocketPlayerController player, GameObject hole)
    {
        gameStateMachine.ChangeState(GameStateId.Death);
        StartCoroutine(_KillPlayer(player, hole));
    }

    private IEnumerator _KillPlayer(PocketPlayerController player, GameObject hole)
    {
        player.playerDetails.stocks--;
        player.StopAllCoroutines();
        player.stateMachine.ChangeState(PlayerState.Dead);

        //fall into the abyss
        for (int i = 0; i < 50; i++)
        {
            player.model.transform.localScale -= new Vector3(.02f, .05f, .05f);
            player.transform.position = Vector3.MoveTowards(player.transform.position, hole.transform.position, .035f);
            yield return new WaitForEndOfFrame();
        }

        if (player.playerDetails.stocks > 0)
            GetReadyForCountDown(player);
        else
            EndGame(player);
    }


    public void EndGame(PocketPlayerController player)
    {
        StopCoroutine("CountDown");
        gameStateMachine.ChangeState(GameStateId.Results);
        player.stateMachine.ChangeState(PlayerState.Dead);
        victoryPlayer = player.playerDetails.id == 1 ? 2 : 1;
        SetGoTextProperties(50, "Player " + victoryPlayer + " is the winner! Hold [Start] to play again!", Color.cyan);
    }


    private void Update()
    {
        frameCount++;

        if (Time.time > nextUpdate)
        {
            nextUpdate += 1.0f / updateRate;
            fps = frameCount * updateRate;
            frameCount = 0;
        }
    }

    private void OnGUI()
    {
        GUIStyle green = new GUIStyle(),
                 small = new GUIStyle(),
                 header = new GUIStyle(),
                 viewDebugStyle = new GUIStyle(),
                 percents = new GUIStyle(),
                 percentsAttached = new GUIStyle(),
                 bigOl = new GUIStyle();
        GUI.color = Color.white;
        Color greyishRed = Color.grey + new Color(.2f, 0f, 0f);

        green.fontSize = 14;
        green.normal.textColor = Color.green;
        header.fontSize = 20;
        header.normal.textColor = Color.white;
        viewDebugStyle.fontSize = 17;
        viewDebugStyle.normal.textColor = GUI.color = new Color(1f, 1f, 1f, .80f);

        small.fontSize = 14;
        small.normal.textColor = Color.white;
        percents.fontSize = 30;
        percents.normal.textColor = Color.white;
        percentsAttached.fontSize = 25;
        percentsAttached.normal.textColor = Color.grey;

        bigOl.fontSize = 50;
        bigOl.normal.textColor = Color.green;
        try
        {
            foreach (PocketPlayerController player in players)
            {
                if (!player.isPlayerStateActive(PlayerState.Dead))
                {
                    wordBoxOffsetX = -1f * (Mathf.Floor(player.playerDetails.percent).ToString() + "%").Length * percentCharacterOffsetX / 2f;

                    Color newColor = ScaleMultiplier(Color.white, greyishRed, player.playerDetails.percent / 100f);
                    newColor.a = ScaleMultiplier(-.2f, .75f, Vector3.Distance(player.transform.position, player.otherPlayer.transform.position) / 5f);

                    percentsAttached.normal.textColor = newColor;
                    GUI.Box(new Rect(mainCamera.WorldToScreenPoint(player.gameObject.transform.position).x - wordBoxOffsetX, Screen.height - mainCamera.WorldToScreenPoint(player.gameObject.transform.position).y - 53, 100f, 100f), Mathf.Floor(player.playerDetails.percent).ToString() + "%", percentsAttached);
                }
            }
        }
        catch (Exception ex) { print(ex.Message); }
        GUILayout.Label(" ");
        GUILayout.Label(" ");
        GUILayout.Label(" ");



        GUI.Box(new Rect(Screen.width - 440f, 45f, Screen.width, Screen.height), "Hold [Start] to restart this match.", viewDebugStyle);
        GUI.Box(new Rect(Screen.width - 440f, 67f, Screen.width, Screen.height), "Press [Select] to toggle Debug Info", viewDebugStyle);
        GUI.Box(new Rect(Screen.width - 440f, 89f, Screen.width, Screen.height), "Press [Start] to skip the countdown", viewDebugStyle);


        if (viewDebug)
        {
            GUI.color = Color.white;
            GUILayout.Label("                    FPS: " + fps, small);
            GUILayout.Label(" ");
            for (int j = 1; j < players.Count + 1; j++)
            {
                GUILayout.Label("             Player " + j.ToString() + " State: " + players[j - 1].stateMachine.GetCurrentStateEnum().ToString(), header);
                GUILayout.Label("             Inputs:", header);
                GUILayout.Label("                    X=" + Input.GetAxis("Player" + j + "Horizontal").ToString("0.###"), small);
                GUILayout.Label("                    Y=" + Input.GetAxis("Player" + j + "Vertical").ToString("0.###"), small);

                for (int i = 0; i < 10; i++)
                {
                    if (Input.GetKey("joystick " + j + " button " + i))
                        GUILayout.Label("                    " + ((Button)i).ToString(), green);
                    else
                        GUILayout.Label("                    " + ((Button)i).ToString(), small);
                }

                GUILayout.Label(" ");


            }
            GUILayout.Label("             CONTROLLERS DETECTED: ", header);
            foreach (string s in Input.GetJoystickNames())
                GUILayout.Label("                   -" + s, small);
        }

        GUILayout.BeginArea(new Rect(Screen.width / 3.5f, Screen.height / 2f, Screen.width, Screen.height));
        GUILayout.EndArea();
        if (!isGameStateActive(GameStateId.Results))
        {
            GUI.color = new Color(1f, 1f, 1f, .75f);
            string player1stocks = "[", player2stocks = "[";
            for (int i = 0; i < players[0].playerDetails.stocks; i++)
                player1stocks += "O";
            player1stocks += "]";
            for (int i = 0; i < players[1].playerDetails.stocks; i++)
                player2stocks += "O";
            player2stocks += "]";

            GUI.Box(new Rect((Screen.width / 6f) - 50, Screen.height - (Screen.height / 6f), Screen.width, Screen.height), "Player 1:  " + Mathf.Floor(players[0].playerDetails.percent) + "%\nStocks: " + players[0].playerDetails.stocks, percents);
            GUI.Box(new Rect(Screen.width - (Screen.width / 6f) - 120, Screen.height - (Screen.height / 6f), Screen.width, Screen.height), "Player 2:  " + Mathf.Floor(players[1].playerDetails.percent) + "%\nStocks: " + players[1].playerDetails.stocks, percents);
            GUI.color = Color.white;
        }

    }


    #region Helpers
    public bool isGameStateActive(GameStateId state)
    {
        return gameStateMachine.GetCurrentStateId() == (int)state;
    }

    private Color ScaleMultiplier(Color min, Color max, float multiple)
    {
        return min + ((max - min) * multiple);
    }
    private float ScaleMultiplier(float min, float max, float multiple)
    {
        return min + ((max - min) * multiple);
    }

    private void SetGoTextProperties(int fontSize, string text, Color color)
    {
        goText.fontSize = fontSize;
        goText.text = text;
        goText.color = color;
    }
    private void SetGoTextProperties(int fontSize, string text)
    {
        goText.fontSize = fontSize;
        goText.text = text;
    }
    private void SetGoTextProperties(int fontSize)
    {
        goText.fontSize = fontSize;
    }
    #endregion


}

public enum Button
{
    A = 0,
    B = 1,
    X = 2,
    Y = 3,
    LeftBumper = 4,
    RightBumper = 5,
    Select = 6,
    Start = 7,
    LStickButton = 8,
    RStickButton = 9
}

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
    [Header("Global Variables (Hover to see more details)")]
    [Tooltip("Speed of the player... duh")]
    [SerializeField]
    public float playerSpeed;
    [Tooltip("When attacked at 0%, how many frames should hitstun/knockback last?")]
    [SerializeField]
    public int minHitstunLength;
    [Tooltip("When attacked at 100%, how many frames should hitstun/knockback last?")]
    [SerializeField]
    public int maxHitstunLength;
    [Tooltip("With how much velocity are you knocked back when attacked with a NON-charged attack?")]
    [SerializeField]
    public float minKnockbackVelocity;
    [Tooltip("With how much velocity are you knocked back when attacked with a FULLY-charged attack?")]
    [SerializeField]
    public float maxKnockbackVelocity;
    [Tooltip("How much EXTRA velocity is added when attacked at 0%")]
    [SerializeField]
    public float minKnockbackVelocityAdditionFromPercent;
    [Tooltip("How much EXTRA velocity is added when attacked at 100%")]
    [SerializeField]
    public float maxKnockbackVelocityAdditionFromPercent;
    [Tooltip("How big is your hitbox when attacking (NON-CHARGED)")]
    [SerializeField]
    public Vector3 smallHitboxScale;
    [Tooltip("How far away from the player's center should their hitbox be placed when attacking? (NON-CHARGED)")]
    [SerializeField]
    public float smallHitboxOffset;
    [Tooltip("How big is your hitbox when attacking (FULLY-CHARGED)")]
    [SerializeField]
    public Vector3 bigHitboxScale;
    [Tooltip("How far away from the player's center should their hitbox be placed when attacking? (FULLY-CHARGED)")]
    [SerializeField]
    public float bigHitboxOffset;
    [Tooltip("Our quickest attack will still take this many frames to automatically \"charge\"")]
    [SerializeField]
    public int minChargeFrames;
    [Tooltip("What is the maximum number of frames that an attack can be charged for? (automatically releases attack after)")]
    [SerializeField]
    public int maxChargeFrames;
    [Tooltip("How many frames does of lag do our attacks have? (NON-CHARGED)")]
    [SerializeField]
    public float minAttackCooldownFrames;
    [Tooltip("How many frames does of lag do our attacks have? (FULLY-CHARGED)")]
    [SerializeField]
    public float maxAttackCooldownFrames;
    [Tooltip("How many frames is our hitbox active while we are in attack lag? (NON-CHARGED)")]
    [SerializeField]
    public float minAttackHitboxActivationFrames;
    [Tooltip("How many frames is our hitbox active while we are in attack lag? (FULLY-CHARGED)")]
    [SerializeField]
    public float maxAttackHitboxActivationFrames;
    [Tooltip("How much percent do we add to the opponent when attacking them? (NON-CHARGED)")]
    [SerializeField]
    public float minPercentDealt;
    [Tooltip("How much percent do we add to the opponent when attacking them? (FULLY-CHARGED)")]
    [SerializeField]
    public float maxPercentDealt;
    [Tooltip("Number of stocks that a player starts with.")]
    [SerializeField]
    public int stockCount;
    [Tooltip("Should both players reset their percent after a stock is taken?")]
    [SerializeField]
    public bool ResetPercentOnKill;
    [Tooltip("Should players be able to influence their direction while in hitstun?")]
    [SerializeField]
    public bool AllowMovementDuringHitstun;
    [Tooltip("(Only applies if 'AllowMovementDuringHitstun' is checked) How much should your movement influence your trajectory while in hitstun? 2 = you can move a lot during hitstun; 1 = regular movement during hitstun; 0 = none")]
    [SerializeField]
    public float MovementReductionDuringHitstun;
    [Tooltip("How many pixels does the percentages string offset from the center of the player?")]
    [SerializeField]
    private float percentCharacterOffsetX;
    [Header("Prefabs")]
    public GameObject playerObj;

    ///////  THESE ARE OLD CONCEPTS. WE PROB WONT USE MOST, BUT I LEAVE IT HERE AS A REMINDER  ///////////////////

    //[SerializeField]
    //public int techWindowFrames; //before hitting the rail, how many frames early can you input a tech successfully?
    //[SerializeField]
    //public int missedTechPunishmentFrames; //how many frames until you can attempt to tech again
    //[SerializeField]
    //public int techDurationFrames; //how long invincibility lasts after a tech
    //[SerializeField]
    //public float speedWhileChargingMultiplier; //1 = no speed change while charging; 0 = can't move while charging
    //[SerializeField]
    //public float directionalInfluenceWeight; //1 = fully change knockback vector; 0 = no effect on knockback vector
    //[SerializeField]
    //public float projectileSpeed; //how fast projectiles move
    //[SerializeField]
    //public int projectileCooldownFrames; //how long projectile attack recovery takes
    //[SerializeField]
    //public int projectileHaltFrames; //how long our position is frozen while shooting a projectile
    //[SerializeField]
    //public int projectileStunFrames; //how long opponent is stunned when hit with projectile
    //[SerializeField]
    //public bool useSmashCamera; //true = camera is dynamic relative to players; false = camera is still with entire table in view

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
                  updateRate = 4.0f;  // 4 updates per sec.
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
        spawnPositions.AddRange(new List<Vector3>() { new Vector3(-10f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(0f, -5f, 0f), new Vector3(0f, 5f, 0f) });
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
        victoryPlayer = player.playerDetails.id == 1 ? 2 : 1;
        player.stateMachine.ChangeState(PlayerState.Dead);

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
        }
        string spaces = "                                                           ";
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


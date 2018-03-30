using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    #region Local Variables

    [HideInInspector]
    private List<PocketPlayerController> players = new List<PocketPlayerController>();
    private int frameCount = 0;
    private float nextUpdate = 0.0f;
    private float fps = 0.0f;
    private float updateRate = 4.0f;  // 4 updates per sec.
    private List<Vector3> spawnPositions = new List<Vector3>();
    [HideInInspector]
    public bool isGameOver = false;
    private int victoryPlayer = 1;
    private Camera gamecam;
    private float wordBoxOffsetX;
    [HideInInspector]
    public bool viewDebug = false;
    [HideInInspector]
    public bool disablePlayersInputs = true;
    private Text goText;

    #endregion

    private void Start()
    {
        goText = GameObject.Find("GoText").GetComponent<Text>();
        goText.fontSize = 0;
        goText.text = "";

        spawnPositions.Add(new Vector3(-10f, 0f, 0f));
        spawnPositions.Add(new Vector3(10f, 0f, 0f));
        gamecam = GameObject.Find("Main Camera").GetComponent<Camera>();
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;

        nextUpdate = Time.time;

        for (int i = 1; i < 3; i++)
            InitializePlayer(i);

        GameObject.Find("Player 1").GetComponent<PocketPlayerController>().otherPlayer = GameObject.Find("Player 2");
        GameObject.Find("Player 2").GetComponent<PocketPlayerController>().otherPlayer = GameObject.Find("Player 1");

        StartCoroutine("ResetPlayers");
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

    private IEnumerator ResetPlayers()
    {
        disablePlayersInputs = true;
        int framesUntilNextNumber = 75;
        int startingSize = 120;
        float incrementToDecreaseSize = 1.35f;
        float incrementTotal = 0f;
        Color startingColor = goText.color;

        //3333333333333333333
        SetGoTextProperties(startingSize, "3");
        for (int i = 0; i < framesUntilNextNumber; i++)
        {
            incrementTotal += incrementToDecreaseSize;
            SetGoTextProperties(startingSize - Mathf.FloorToInt(incrementTotal));
            yield return new WaitForEndOfFrame();
        }
        incrementTotal = 0f;


        //2222222222222222222
        SetGoTextProperties(startingSize, "2");
        for (int i = 0; i < framesUntilNextNumber; i++)
        {
            incrementTotal += incrementToDecreaseSize;
            SetGoTextProperties(startingSize - Mathf.FloorToInt(incrementTotal));
            yield return new WaitForEndOfFrame();
        }
        incrementTotal = 0f;


        //11111111111111111111111111
        SetGoTextProperties(startingSize, "1");
        for (int i = 0; i < framesUntilNextNumber; i++)
        {
            incrementTotal += incrementToDecreaseSize;
            SetGoTextProperties(startingSize - Mathf.FloorToInt(incrementTotal));
            yield return new WaitForEndOfFrame();
        }
        incrementTotal = 0f;
        disablePlayersInputs = false;


        //GOOOOOOOOOOOO!!!!
        SetGoTextProperties(140, "GO!", Color.green);
        for (int i = 0; i < framesUntilNextNumber; i++)
            yield return new WaitForEndOfFrame();
        incrementTotal = 0f;
        SetGoTextProperties(0, "", startingColor);
    }

    public void InitializePlayer(int id)
    {
        GameObject newPlayer = GameObject.Instantiate(playerObj, spawnPositions[id - 1], Quaternion.identity) as GameObject;
        newPlayer.name = "Player " + id;
        PocketPlayerController newController = newPlayer.GetComponent<PocketPlayerController>();
        newController.InitializePlayer(id);
        newController.playerDetails.stocks = stockCount;
        players.Add(newController);
    }

    public void LoseStock(PocketPlayerController player)
    {
        player.playerDetails.stocks--;
        disablePlayersInputs = true;

        foreach (PocketPlayerController _player in players)
        {
            _player.stateMachine.ChangeState(StateId.Idle, true);
            _player.StopAllCoroutines();
        }

        if (player.playerDetails.stocks == 0)
            EndGame();
        else
        {
            player.gameObject.transform.position = spawnPositions[player.playerDetails.id - 1];
            player.otherPlayer.transform.position = spawnPositions[player.otherPlayer.GetComponent<PocketPlayerController>().playerDetails.id - 1];
            player.playerDetails.percent = 0f;
            if (ResetPercentOnKill)
                player.otherPlayer.GetComponent<PocketPlayerController>().playerDetails.percent = 0f;
            StartCoroutine("ResetPlayers");
        }

    }

    public void EndGame()
    {
        isGameOver = true;

        for (int i = 0; i < players.Count; i++)
            if (players[i].playerDetails.stocks == 0)
            {
                victoryPlayer = players[i].otherPlayer.GetComponent<PocketPlayerController>().playerDetails.id;
                Destroy(players[i].gameObject);
            }

        SetGoTextProperties(50, "Player " + victoryPlayer + " is the winner! Press [Start] to play again!");
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
        viewDebugStyle.fontSize = 25;
        viewDebugStyle.normal.textColor = GUI.color = new Color(1f, 1f, 1f, .75f);

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
                wordBoxOffsetX = -1f * (Mathf.Floor(player.playerDetails.percent).ToString() + "%").Length * percentCharacterOffsetX / 2f;
                Color newColor = ScaleMultiplier(Color.white, greyishRed, player.playerDetails.percent / 100f);
                newColor.a = ScaleMultiplier(-.2f, .75f, Vector3.Distance(player.transform.position, player.otherPlayer.transform.position) / 8f);
                percentsAttached.normal.textColor = newColor;
                GUI.Box(new Rect(gamecam.WorldToScreenPoint(player.gameObject.transform.position).x - wordBoxOffsetX, Screen.height - gamecam.WorldToScreenPoint(player.gameObject.transform.position).y - 53, 100f, 100f), Mathf.Floor(player.playerDetails.percent).ToString() + "%", percentsAttached);
            }
        }
        catch { }
        GUILayout.Label(" ");
        GUILayout.Label(" ");
        GUILayout.Label(" ");
        if (!viewDebug)
        {
            GUI.Box(new Rect((Screen.width / 2.5f), Screen.height - (Screen.height / 8f), Screen.width, Screen.height), "Press [Select] to toggle Debug Info", viewDebugStyle);
        }
        else
        {
            GUI.color = Color.white;
            GUILayout.Label("                    FPS: " + fps, small);
            GUILayout.Label(" ");
            for (int j = 1; j < players.Count + 1; j++)
            {
                GUILayout.Label("             Player " + j.ToString() + " State: " + players[j - 1].stateMachine.GetCurrentState().name, header);
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
        if (!isGameOver)
        {
            GUI.color = new Color(1f, 1f, 1f, .75f);
            string player1stocks = "[", player2stocks = "[";
            for (int i = 0; i < players[0].playerDetails.stocks; i++)
                player1stocks += "O";
            player1stocks += "]";
            for (int i = 0; i < players[1].playerDetails.stocks; i++)
                player2stocks += "O";
            player2stocks += "]";

            GUI.Box(new Rect((Screen.width / 4f) - 50, Screen.height - (Screen.height / 4f), Screen.width, Screen.height), "Player 1\n" + Mathf.Floor(players[0].playerDetails.percent) + "%\nStocks: " + players[0].playerDetails.stocks, percents);
            GUI.Box(new Rect(Screen.width - (Screen.width / 4f) - 50, Screen.height - (Screen.height / 4f), Screen.width, Screen.height), "Player 2\n" + Mathf.Floor(players[1].playerDetails.percent) + "%\nStocks: " + players[1].playerDetails.stocks, percents);
            GUI.color = Color.white;
        }

    }

    private Color ScaleMultiplier(Color min, Color max, float multiple)
    {
        return min + ((max - min) * multiple);
    }
    private float ScaleMultiplier(float min, float max, float multiple)
    {
        return min + ((max - min) * multiple);
    }
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


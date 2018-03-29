using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public bool ApplyMovementDuringHitstun;


    ///////  THESE FIELDS ARE COMING SOON  ///////////////////////////////////////////////////////////////////////
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

    [Header("Prefabs")]
    public GameObject playerObj;

    #endregion

    #region Local Variables

    [HideInInspector]
    private List<PocketPlayerController> players = new List<PocketPlayerController>();
    private int frameCount = 0;
    private float nextUpdate = 0.0f;
    private float fps = 0.0f;
    private float updateRate = 4.0f;  // 4 updates per sec.
    private List<Vector3> spawnPositions = new List<Vector3>();

    #endregion

    private void Start()
    {
        spawnPositions.Add(new Vector3(-10f, 0f, 0f));
        spawnPositions.Add(new Vector3(10f, 0f, 0f));

        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;

        nextUpdate = Time.time;

        for (int i = 1; i < 3; i++)
            InitializePlayer(i);

        GameObject.Find("Player 1").GetComponent<PocketPlayerController>().otherPlayer = GameObject.Find("Player 2");
        GameObject.Find("Player 2").GetComponent<PocketPlayerController>().otherPlayer = GameObject.Find("Player 1");
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
        if (player.playerDetails.stocks == 0)
            EndGame();
        else
        {
            player.gameObject.transform.position = spawnPositions[player.playerDetails.id - 1];
            player.otherPlayer.transform.position = spawnPositions[player.otherPlayer.GetComponent<PocketPlayerController>().playerDetails.id - 1];

            player.playerDetails.percent = 0f;
            if (ResetPercentOnKill)
                player.otherPlayer.GetComponent<PocketPlayerController>().playerDetails.percent = 0f;
        }
    }

    public void EndGame()
    {
        if (players[0].playerDetails.stocks == 0)
        {
            print("Player 1 wins");
        }
        else
        {
            print("Player 2 wins");
        }
        print("Press start to restart");
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
                 percents = new GUIStyle();
        GUI.color = Color.white;

        green.fontSize = 14;
        green.normal.textColor = Color.green;
        header.fontSize = 20;
        header.normal.textColor = Color.white;
        small.fontSize = 14;
        small.normal.textColor = Color.white;
        percents.fontSize = 30;
        percents.normal.textColor = Color.white;

        GUILayout.Label(" ");
        GUILayout.Label(" ");
        GUILayout.Label(" ");

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
        string spaces = "                                                           ";
        GUILayout.Label(spaces + "[P1] " + Mathf.Floor(players[0].playerDetails.percent) + "%" + spaces + "[P2] " + Mathf.Floor(players[1].playerDetails.percent) + "%", percents);

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


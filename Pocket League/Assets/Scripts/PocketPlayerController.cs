using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PocketPlayerController : MonoBehaviour
{

    #region Variables

    [HideInInspector]
    public PocketPlayerMachine stateMachine = new PocketPlayerMachine();
    private GameStateMachine gameStateMachine;

    [HideInInspector]
    public MasterLogic masterLogic;
    [HideInInspector]
    public float chargeMultiple = 0f, vert = 0f, horiz = 0f;
    [HideInInspector]
    public GameObject otherPlayer;
    [HideInInspector]
    public int chargeCounter = 0;
    [HideInInspector]
    public PlayerDetails playerDetails = new PlayerDetails(-1);
    [HideInInspector]
    public Vector3 knockBackTrajectory = new Vector3(0f, 0f, 0f);
    private int startCounter = 0;

    private List<Button> ButtonList_OnKeyDown = new List<Button>();
    private List<Button> ButtonList_OnKeyUp = new List<Button>();
    private List<Button> ButtonList_OnKey = new List<Button>();

    [HideInInspector]
    public GameObject hitBox, model;

    private Color color_idle = Color.white,
                  color_run = Color.white,
                  color_charge = Color.yellow,
                  color_attack = Color.red,
                  color_hitstun = Color.white;

    private Vector3 moveVector = new Vector3();
    private bool hasController = false;
    #endregion

    public void InitializePlayer(int playerId)
    {
        playerDetails = new PlayerDetails(playerId);
        masterLogic = GameObject.FindObjectOfType<MasterLogic>();
        gameStateMachine = masterLogic.gameStateMachine;

        model = transform.Find("PlayerModel").gameObject;

        SetBallColor(color_idle, isBlank: true);
        hitBox = transform.Find("hitbox").gameObject;
        hitBox.transform.localScale = masterLogic.smallHitboxScale;
        hitBox.transform.localPosition += new Vector3(0f, 0f, masterLogic.smallHitboxOffset);

        stateMachine.Subscribe(BeginCharge, PlayerState.Charge, true);
        stateMachine.Subscribe(AttackRecovery, PlayerState.AttackRecovery, true);
        stateMachine.Subscribe(GetHit, PlayerState.Hitstun, true);
        stateMachine.Subscribe(Idle, PlayerState.Idle, true);
        stateMachine.Subscribe(Run, PlayerState.Run, true);
        stateMachine.Subscribe(Dead, PlayerState.Dead, true);

        hasController = Input.GetJoystickNames().Length >= playerId;

    }

    void Dead()
    {
        SetBallColor(new Color(.25f, .25f, .25f, 1f));
    }

    void Idle()
    {
        SetBallColor(color_idle, isBlank: true);
        hitBox.GetComponent<BoxCollider>().enabled = false;
        hitBox.GetComponent<MeshRenderer>().enabled = false;
    }

    void Run()
    {
        SetBallColor(color_run, isBlank:true);
        hitBox.GetComponent<BoxCollider>().enabled = false;
        hitBox.GetComponent<MeshRenderer>().enabled = false;
    }

    void BeginCharge()
    {
        SetBallColor(color_charge);
        StopCoroutine("chargeAttack");
        StartCoroutine("chargeAttack");
    }

    void AttackRecovery()
    {
        SetBallColor(color_attack);
        StopCoroutine("attackRecovery");
        StartCoroutine("attackRecovery");
    }

    void GetHit()
    {
        SetBallColor(color_hitstun);
        StopCoroutine("chargeAttack");
        StopCoroutine("getHit");
        StartCoroutine("getHit");
    }

    private IEnumerator getHit()
    {
        //disable opponents hitbox until their next attack
        otherPlayer.transform.Find("hitbox").GetComponent<BoxCollider>().enabled = false;

        //values weighted from 0 to 1
        float _chargeMultiple = otherPlayer.GetComponent<PocketPlayerController>().chargeMultiple;


        float _percentMultiple = playerDetails.percent / 100f;

        //add percent from being hit
        playerDetails.percent += ScaleMultiplier(masterLogic.minPercentDealt, masterLogic.maxPercentDealt, _chargeMultiple);

        //hitstun length, and velocity (based on percent and charge)
        float hitstunLength = ScaleMultiplier(masterLogic.minHitstunLength, masterLogic.maxHitstunLength, _percentMultiple);
        float velocity = ScaleMultiplier(masterLogic.minKnockbackVelocity, masterLogic.maxKnockbackVelocity, _chargeMultiple);
        velocity += ScaleMultiplier(masterLogic.minKnockbackVelocityAdditionFromPercent, masterLogic.maxKnockbackVelocityAdditionFromPercent, _percentMultiple);

        //direction the player is attacking:
        Vector3 attackAngleTrajectory = (otherPlayer.transform.Find("hitbox").position - otherPlayer.transform.position).normalized;
        //relative angle between players
        Vector3 playerAngleTrajectory = (transform.position - otherPlayer.transform.position).normalized;

        //OPTION 1: attack sends opponent at the current angle between players
        //knockBackTrajectory = playerAngleTrajectory.normalized * velocity;

        //OPTION 2: knockback direction based on the midpoint of attack angle and player angle
        //knockBackTrajectory = ((attackAngleTrajectory + playerAngleTrajectory) / 2f).normalized * velocity;

        //OPTION 3: attack sends opponent at the angle of the attack
        knockBackTrajectory = attackAngleTrajectory.normalized * velocity;

        for (int i = 0; i < Mathf.Floor(hitstunLength); i++)
        {
            transform.LookAt(transform.position + knockBackTrajectory);

            transform.Translate(knockBackTrajectory, Space.World);
            model.transform.Rotate(new Vector3(7f, 0f, 0f), Space.Self);
            yield return new WaitForEndOfFrame();
        }

        stateMachine.ChangeState(PlayerState.Actionable);
    }

    private IEnumerator chargeAttack()
    {
        chargeCounter = 0;
        while (isPlayerStateActive(PlayerState.Charge))
        {
            if (chargeCounter > masterLogic.maxChargeFrames)
            {   //held for maximum charge
                chargeCounter = masterLogic.maxChargeFrames;
                stateMachine.ChangeState(PlayerState.AttackRecovery);
            }
            else if (chargeCounter >= masterLogic.minChargeFrames && !ButtonList_OnKey.Contains(Button.X))
            {   //we let go of charge
                stateMachine.ChangeState(PlayerState.AttackRecovery);
            }
            else
            {   //still charging
                yield return new WaitForEndOfFrame();
                chargeCounter++;
            }
        }
    }

    private IEnumerator attackRecovery()
    {
        ToggleHitbox(hitBox, true);
        chargeMultiple = ((float)chargeCounter - masterLogic.minChargeFrames) / (masterLogic.maxChargeFrames - masterLogic.minChargeFrames);

        float framesToRecover = ScaleMultiplier(masterLogic.minAttackCooldownFrames, masterLogic.maxAttackCooldownFrames, chargeMultiple);
        float hitboxActivationFrames = ScaleMultiplier(masterLogic.minAttackHitboxActivationFrames, masterLogic.maxAttackHitboxActivationFrames, chargeMultiple);
        float hitboxOffset = ScaleMultiplier(masterLogic.smallHitboxOffset, masterLogic.bigHitboxOffset, chargeMultiple);

        hitBox.transform.localScale = ScaleMultiplier(masterLogic.smallHitboxScale, masterLogic.bigHitboxScale, chargeMultiple);
        hitBox.transform.localPosition = Vector3.zero + new Vector3(0f, 0f, hitboxOffset);

        for (float i = 0; i < framesToRecover; i++)
        {
            if (i > hitboxActivationFrames)
                ToggleHitbox(hitBox, false);
            if (isPlayerStateActive(PlayerState.AttackRecovery))
                yield return new WaitForEndOfFrame();
        }

        ToggleHitbox(hitBox, false);

        if (isPlayerStateActive(PlayerState.AttackRecovery))
            stateMachine.ChangeState(PlayerState.Actionable);
    }

    private void LateUpdate()
    {
        ButtonList_OnKey.Clear();
        ButtonList_OnKeyDown.Clear();
        ButtonList_OnKeyUp.Clear();

        if (playerDetails.id > 0)
        {
            stateMachine.ChangeState(PlayerState.Idle);

            if (hasController)
            {
                vert = Input.GetAxis("Player" + playerDetails.id + "Vertical");
                horiz = Input.GetAxis("Player" + playerDetails.id + "Horizontal");
            }
            else if (playerDetails.id == 1)
            {
                vert = 0f;
                horiz = 0f;
                vert += Input.GetKey(KeyCode.W) ? 1f : 0f;
                vert += Input.GetKey(KeyCode.S) ? -1f : 0f;
                horiz += Input.GetKey(KeyCode.A) ? -1f : 0f;
                horiz += Input.GetKey(KeyCode.D) ? 1f : 0f;
            }

            //by normalizing the vector, we solve the 'diagonal is faster' problem
            moveVector = new Vector3(horiz, 0f, vert).normalized;

            //slide along the wall if near
            RaycastHit[] hits = (Physics.RaycastAll(transform.position, moveVector, .5f));
            foreach (RaycastHit hit in hits)
                if (hit.transform.tag == "Wall")
                {
                    moveVector = (moveVector - (Vector3.Dot(moveVector, hit.normal)) * hit.normal).normalized;
                    break;
                }

            //apply the player speed to our normalized movement vector
            moveVector *= masterLogic.playerSpeed;

            //only want to be moving during these scenes   
            if (!masterLogic.isGameStateActive(GameStateId.Countdown))
                if (horiz != 0f || vert != 0f)
                    stateMachine.ChangeState(PlayerState.Run);

            //allow movement via joystick if we are running
            if (isPlayerStateActive(PlayerState.Run))
            {
                if (isPlayerStateActive(PlayerState.Run))
                    transform.LookAt(transform.position + moveVector);

                transform.Translate(moveVector, Space.World);
                model.transform.Rotate(new Vector3(7f, 0f, 0f), Space.Self);
            }

            if (hasController)
            {
                //check all 10 xbox controller buttons
                for (int i = 0; i < 10; i++)
                {
                    if (Input.GetKeyDown("joystick " + playerDetails.id + " button " + i)) { ButtonList_OnKeyDown.Add((Button)i); }
                    if (Input.GetKeyUp("joystick " + playerDetails.id + " button " + i)) { ButtonList_OnKeyUp.Add((Button)i); }
                    if (Input.GetKey("joystick " + playerDetails.id + " button " + i)) { ButtonList_OnKey.Add((Button)i); }

                }
            }

            else if (!hasController && playerDetails.id == 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    KeyCode thisKey = new KeyCode();
                    int buttonNum = 0;

                    switch (i)
                    {
                        case 0: thisKey = KeyCode.J; buttonNum = 2; break;
                        case 1: thisKey = KeyCode.K; buttonNum = 7; break;
                        case 2: thisKey = KeyCode.L; buttonNum = 6; break;
                    }

                    if (Input.GetKeyDown(thisKey)) ButtonList_OnKeyDown.Add((Button)buttonNum);
                    if (Input.GetKeyUp(thisKey)) ButtonList_OnKeyUp.Add((Button)buttonNum);
                    if (Input.GetKey(thisKey)) ButtonList_OnKey.Add((Button)buttonNum);
                }
            }

            //start attack
            if (ButtonList_OnKeyDown.Contains(Button.X) && (masterLogic.isGameStateActive(GameStateId.Battle) || masterLogic.isGameStateActive(GameStateId.Results)))
                stateMachine.ChangeState(PlayerState.Charge);
            //skip the countdown
            if (ButtonList_OnKeyDown.Contains(Button.Start))
                gameStateMachine.ChangeState(GameStateId.Battle);
            //view debug info
            if (ButtonList_OnKeyDown.Contains(Button.Select))
                masterLogic.viewDebug = !masterLogic.viewDebug;

            //hold start for x frames to reload the scene
            if (ButtonList_OnKey.Contains(Button.Start))
            {
                startCounter++;
                if (startCounter > 55) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
            }
            else { startCounter = 0; }
        }
    }

    private void ToggleHitbox(GameObject hitbox, bool isEnabled)
    {
        hitbox.GetComponent<BoxCollider>().enabled = isEnabled;
        hitbox.GetComponent<MeshRenderer>().enabled = isEnabled;
    }

    private float ScaleMultiplier(float min, float max, float multiple)
    {
        return min + ((max - min) * multiple);
    }
    private Vector3 ScaleMultiplier(Vector3 min, Vector3 max, float multiple)
    {
        return min + ((max - min) * multiple);
    }

    public bool isPlayerStateActive(PlayerState state)
    {
        return (PlayerState)stateMachine.GetCurrentStateEnum() == state;
    }

    public void SetBallColor(Color c, bool isBlank = false, float influence = 1f)
    {
        if (isBlank)
            model.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        else
        {
            model.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            model.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.Lerp(new Color(0f, 0f, 0f, 0f), c, influence));
        }
    }
}

public class PlayerDetails
{
    public int id { get; set; }
    public float percent { get; set; }
    public int stocks { get; set; }
    public PlayerDetails(int _id)
    {
        id = _id;
        percent = 0f;
    }
}
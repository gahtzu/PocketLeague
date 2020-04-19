using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PocketPlayerController : MonoBehaviour
{

    #region Variables
    private GameStateMachine gameStateMachine;
    [HideInInspector]
    public PocketPlayerMachine stateMachine = new PocketPlayerMachine();
    [HideInInspector]
    public MasterLogic masterLogic;
    [HideInInspector]
    public float chargeMultiple = 0f;
    [HideInInspector]
    public Vector2 JoystickPosition = new Vector2(0f, 0f);
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
                  color_attack = Color.yellow,
                  color_hitstun = Color.magenta,
                  color_dead = new Color(.25f, .25f, .25f, 1f);

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
        SetBallColor(color_dead);
    }

    void Idle()
    {
        SetBallColor(color_idle, isBlank: true);
        ToggleHitbox(hitBox, false);
    }

    void Run()
    {
        SetBallColor(color_run, isBlank: true);
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
        SetBallColor(color_attack, isBlank: true);
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

        //knockBackTrajectory = playerAngleTrajectory.normalized * velocity;
        knockBackTrajectory = attackAngleTrajectory.normalized * velocity;
        //knockBackTrajectory = ((attackAngleTrajectory + playerAngleTrajectory) / 2f).normalized * velocity;

        for (int i = 0; i < Mathf.Floor(hitstunLength); i++)
        {
            MovePlayer(knockBackTrajectory);
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
        stateMachine.ChangeState(PlayerState.Idle);
        GetInputs();

        //by normalizing the vector, we solve the 'diagonal is faster' problem
        moveVector = new Vector3(JoystickPosition.x, 0f, JoystickPosition.y).normalized;

        //slide along the wall if near
        RaycastHit[] hits = (Physics.RaycastAll(transform.position, moveVector, .65f));
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
            if (JoystickPosition != new Vector2(0f, 0f))
                stateMachine.ChangeState(PlayerState.Run);

        //allow movement via joystick if we are running
        if (isPlayerStateActive(PlayerState.Run))
            MovePlayer(moveVector);

        if (ButtonPressed(Button.X) && (masterLogic.isGameStateActive(GameStateId.Battle)))
            stateMachine.ChangeState(PlayerState.Charge); //start attack
        if (ButtonPressed(Button.Start))
            gameStateMachine.ChangeState(GameStateId.Battle); //skip the countdown
        if (ButtonPressed(Button.Select))
            masterLogic.viewDebug = !masterLogic.viewDebug; //view debug info

        if (ButtonHeld(Button.Start))
        {
            startCounter++; //hold start for 55 frames to reload the scene
            if (startCounter > 55) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        }
        else { startCounter = 0; }

    }

    public void GetInputs()
    {
        ButtonList_OnKey.Clear();
        ButtonList_OnKeyDown.Clear();
        ButtonList_OnKeyUp.Clear();

        if (hasController)
        {
            JoystickPosition.x = Input.GetAxis("Player" + playerDetails.id + "Horizontal");
            JoystickPosition.y = Input.GetAxis("Player" + playerDetails.id + "Vertical");
        }
        else
        {
            JoystickPosition.y = Input.GetKey(KeyCode.W) ? 1f : 0f;
            JoystickPosition.y += Input.GetKey(KeyCode.S) ? -1f : 0f;
            JoystickPosition.x = Input.GetKey(KeyCode.A) ? -1f : 0f;
            JoystickPosition.x += Input.GetKey(KeyCode.D) ? 1f : 0f;
        }

        if (hasController)
        {
            for (int i = 0; i < 10; i++)
                RegisterControllerInputs("joystick " + playerDetails.id + " button " + i, i);
        }
        else
        {
            RegisterKeyboardInputs(KeyCode.J, 2);
            RegisterKeyboardInputs(KeyCode.K, 7);
            RegisterKeyboardInputs(KeyCode.L, 6);
        }
    }

    public void MovePlayer(Vector3 movementVector)
    {
        transform.LookAt(transform.position + movementVector);
        transform.Translate(movementVector, Space.World);
        model.transform.Rotate(new Vector3(7f, 0f, 0f), Space.Self);
    }

    public void RegisterKeyboardInputs(KeyCode keycode, int buttonNumber)
    {
        if (Input.GetKeyDown(keycode)) ButtonList_OnKeyDown.Add((Button)buttonNumber);
        if (Input.GetKeyUp(keycode)) ButtonList_OnKeyUp.Add((Button)buttonNumber);
        if (Input.GetKey(keycode)) ButtonList_OnKey.Add((Button)buttonNumber);
    }

    public void RegisterControllerInputs(string keycode, int buttonNumber)
    {
        if (Input.GetKeyDown(keycode)) ButtonList_OnKeyDown.Add((Button)buttonNumber);
        if (Input.GetKeyUp(keycode)) ButtonList_OnKeyUp.Add((Button)buttonNumber);
        if (Input.GetKey(keycode)) ButtonList_OnKey.Add((Button)buttonNumber);
    }

    public bool ButtonReleased(Button button)
    {
        return ButtonList_OnKeyUp.Contains(button);
    }
    public bool ButtonPressed(Button button)
    {
        return ButtonList_OnKeyDown.Contains(button);
    }
    public bool ButtonHeld(Button button)
    {
        return ButtonList_OnKey.Contains(button);
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
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
    public ChargeAttack ChargeAttackProperties;
    [HideInInspector]
    public Teleport TeleportProperties;
    [HideInInspector]
    public Projectile ProjectileProperties;
    [HideInInspector]
    public SwipeAttack SwipeAttackProperties;
    [HideInInspector]
    public float chargeMultiple = 0f, knockbackVelocity = 0f;
    [HideInInspector]
    public Vector2 JoystickPosition = new Vector2(0f, 0f);
    [HideInInspector]
    public GameObject otherPlayer;
    [HideInInspector]
    public int chargeCounter = 0;
    [HideInInspector]
    public PlayerDetails playerDetails = new PlayerDetails(-1);
    [HideInInspector]
    public Vector3 knockbackTrajectory = new Vector3(0f, 0f, 0f);
    private int startCounter = 0;
    [HideInInspector]
    public bool isSwipingLeft = false;
    private List<Button> ButtonList_OnKeyDown = new List<Button>();
    private List<Button> ButtonList_OnKeyUp = new List<Button>();
    private List<Button> ButtonList_OnKey = new List<Button>();

    [HideInInspector]
    public GameObject hitBox, model, line;

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
        SwipeAttackProperties = GameObject.FindObjectOfType<SwipeAttack>();
        ChargeAttackProperties = GameObject.FindObjectOfType<ChargeAttack>();
        ProjectileProperties = GameObject.FindObjectOfType<Projectile>();
        TeleportProperties = GameObject.FindObjectOfType<Teleport>();

        gameStateMachine = masterLogic.gameStateMachine;

        model = transform.Find("PlayerModel" + playerId).gameObject;
        model.SetActive(true);
        SetBallColor(color_idle, isBlank: true);
        line = transform.Find("Line").gameObject;
        hitBox = transform.Find("HitboxHolder").Find("hitbox").gameObject;
        hitBox.transform.localScale = ChargeAttackProperties.smallHitboxScale;
        hitBox.transform.localPosition += new Vector3(0f, 0f, ChargeAttackProperties.smallHitboxOffset);

        stateMachine.Subscribe(BeginCharge, PlayerState.Charge, true);
        stateMachine.Subscribe(ChargeAttackRecovery, PlayerState.ChargeAttackRecovery, true);
        stateMachine.Subscribe(GetHit, PlayerState.Hitstun, true);
        stateMachine.Subscribe(Idle, PlayerState.Idle, true);
        stateMachine.Subscribe(Run, PlayerState.Run, true);
        stateMachine.Subscribe(Dead, PlayerState.Dead, true);
        stateMachine.Subscribe(SwipeAttack, PlayerState.SwipeAttack, true);
        stateMachine.Subscribe(Teleport, PlayerState.Teleport, true);
        stateMachine.Subscribe(Projectile, PlayerState.Projectile, true);

        hasController = Input.GetJoystickNames().Length >= playerId;
    }

    void Dead()
    {
        SetBallColor(color_dead);
    }

    void Idle()
    {
        line.SetActive(false);
        SetBallColor(color_idle, isBlank: true);
        ToggleHitbox(hitBox, false);
    }

    void Run()
    {
        SetBallColor(color_run, isBlank: true);
        ToggleHitbox(hitBox, false);
    }

    void BeginCharge()
    {
        SetBallColor(color_charge, isBlank: true);
        StopCoroutine("chargeAttack");
        StartCoroutine("chargeAttack");
    }

    void ChargeAttackRecovery()
    {
        line.SetActive(false);
        SetBallColor(color_attack, isBlank: true);
        StopCoroutine("changeAttackRecovery");
        StartCoroutine("changeAttackRecovery");
    }

    void GetHit()
    {
        SetBallColor(color_hitstun);
        StopCoroutine("chargeAttack");
        StopCoroutine("swipeAttack");
        StopCoroutine("teleport");
        StopCoroutine("projectile");

        StopCoroutine("getHit");
        StartCoroutine("getHit");
    }

    void SwipeAttack()
    {
        SetBallColor(color_charge, isBlank: true);
        StopCoroutine("swipeAttack");
        StartCoroutine("swipeAttack");
    }

    void Teleport()
    {
        SetBallColor(color_charge, isBlank: true);
        StopCoroutine("teleport");
        StartCoroutine("teleport");
    }

    void Projectile()
    {
        SetBallColor(color_charge, isBlank: true);
        StopCoroutine("projectile");
        StartCoroutine("projectile");
    }


    private IEnumerator getHit()
    {
        //disable opponents hitbox until their next attack
        ToggleHitbox(otherPlayer.transform.Find("HitboxHolder").Find("hitbox").gameObject, isEnabled: false, alsoToggleVisual: false);

        //values weighted from 0 to 1
        float _chargeMultiple = otherPlayer.GetComponent<PocketPlayerController>().chargeMultiple;
        float _percentMultiple = playerDetails.percent / 100f;

        //add percent from being hit
        playerDetails.percent += ScaleMultiplier(ChargeAttackProperties.minPercentDealt, ChargeAttackProperties.maxPercentDealt, _chargeMultiple);

        //hitstun length, and velocity (based on percent and charge)
        float hitstunLength = ScaleMultiplier(ChargeAttackProperties.minHitstunLength, ChargeAttackProperties.maxHitstunLength, _percentMultiple);
        knockbackVelocity = ScaleMultiplier(ChargeAttackProperties.minKnockbackVelocity, ChargeAttackProperties.maxKnockbackVelocity, _chargeMultiple);
        knockbackVelocity += ScaleMultiplier(ChargeAttackProperties.minKnockbackVelocityAdditionFromPercent, ChargeAttackProperties.maxKnockbackVelocityAdditionFromPercent, _percentMultiple);

        //direction the player is attacking:
        Vector3 attackAngleTrajectory = (otherPlayer.transform.position - otherPlayer.transform.Find("Front").position).normalized;
        //relative angle between players
        Vector3 playerAngleTrajectory = (transform.position - otherPlayer.transform.position).normalized;

        knockbackTrajectory = (attackAngleTrajectory.normalized * knockbackVelocity *-1f).ApplyDirectionalInfluence(JoystickPosition, knockbackVelocity, masterLogic.DirectionalInfluenceMultiplier);

        for (int i = 0; i < Mathf.Floor(hitstunLength); i++)
        {
            MovePlayer(knockbackTrajectory);
            yield return new WaitForEndOfFrame();
        }

        stateMachine.ChangeState(PlayerState.Actionable);
    }

    private IEnumerator chargeAttack()
    {
        chargeCounter = 0;
        while (isPlayerStateActive(PlayerState.Charge))
        {
            float weight = (float)chargeCounter / (float)ChargeAttackProperties.maxChargeFrames;
            SetBallColor(new Color(weight, 0f, 0f) * 1.5f, false, weight * 1.5f);
           
            if (chargeCounter > ChargeAttackProperties.minChargeFrames + 2)
                line.SetActive(true);
            if (chargeCounter > ChargeAttackProperties.maxChargeFrames)
            {   //held for maximum charge
                chargeCounter = ChargeAttackProperties.maxChargeFrames;
                stateMachine.ChangeState(PlayerState.ChargeAttackRecovery);

                //chargeCounter = ChargeAttackProperties.maxChargeFrames;
            }
            else if (chargeCounter >= ChargeAttackProperties.minChargeFrames && !ButtonList_OnKey.Contains(Button.B))
            {   //we let go of charge
                stateMachine.ChangeState(PlayerState.ChargeAttackRecovery);
            }
            else
            {   //still charging
                yield return new WaitForEndOfFrame();
                chargeCounter++;
            }
        }
    }

    private void ResetHitboxOrientation()
    {
        hitBox.transform.parent.localPosition = Vector3.zero;
        hitBox.transform.localPosition = Vector3.zero;
        hitBox.transform.parent.localEulerAngles = Vector3.zero;
        hitBox.transform.localEulerAngles = Vector3.zero;

    }

    private IEnumerator changeAttackRecovery()
    {
        ResetHitboxOrientation();

        chargeMultiple = ((float)chargeCounter - ChargeAttackProperties.minChargeFrames) / (ChargeAttackProperties.maxChargeFrames - ChargeAttackProperties.minChargeFrames);

        float framesToRecover = ScaleMultiplier(ChargeAttackProperties.minAttackCooldownFrames, ChargeAttackProperties.maxAttackCooldownFrames, chargeMultiple);
        float hitboxActivationFrames = ScaleMultiplier(ChargeAttackProperties.minAttackHitboxActivationFrames, ChargeAttackProperties.maxAttackHitboxActivationFrames, chargeMultiple);
        float hitboxOffset = ScaleMultiplier(ChargeAttackProperties.smallHitboxOffset, ChargeAttackProperties.bigHitboxOffset, chargeMultiple);

        hitBox.transform.localScale = ScaleMultiplier(ChargeAttackProperties.smallHitboxScale, ChargeAttackProperties.bigHitboxScale, chargeMultiple);
        hitBox.transform.localPosition = Vector3.zero + new Vector3(0f, 0f, hitboxOffset);
        ToggleHitbox(hitBox, true, true);

        for (float i = 0; i < framesToRecover; i++)
        {
            if (i > hitboxActivationFrames)
                ToggleHitbox(hitBox, false);
            if (isPlayerStateActive(PlayerState.ChargeAttackRecovery))
                yield return new WaitForEndOfFrame();
        }

        ToggleHitbox(hitBox, false);

        if (isPlayerStateActive(PlayerState.ChargeAttackRecovery))
            stateMachine.ChangeState(PlayerState.Actionable);

        chargeCounter = 0;
    }

    private IEnumerator swipeAttack()
    {
        chargeCounter = 0;

        for (int i = 0; i < SwipeAttackProperties.startupFrames; i++)
            yield return new WaitForEndOfFrame();

        ToggleHitbox(hitBox, true, true);
        ResetHitboxOrientation();


        if (SwipeAttackProperties.startingYRotationOffset < 0 && !isSwipingLeft)
            SwipeAttackProperties.startingYRotationOffset *= -1f;


        if (SwipeAttackProperties.startingYRotationOffset > 0 && isSwipingLeft)
            SwipeAttackProperties.startingYRotationOffset *= -1f;

        hitBox.transform.localScale = SwipeAttackProperties.hitBoxScale;
        hitBox.transform.localPosition = SwipeAttackProperties.startingOffset;
        hitBox.transform.parent.localEulerAngles = new Vector3(0f, SwipeAttackProperties.startingYRotationOffset);

        float increment = (SwipeAttackProperties.startingYRotationOffset * 2f) / (float)SwipeAttackProperties.framesToSwipe;

        for (int i = 0; i < SwipeAttackProperties.framesToSwipe; i++)
        {
            hitBox.transform.parent.Rotate(new Vector3(0f, -increment, 0f), Space.Self);
            yield return new WaitForEndOfFrame();

        }

        for (int i = 0; i < SwipeAttackProperties.hitboxLingerFrames; i++)
            yield return new WaitForEndOfFrame();


        ToggleHitbox(hitBox, false, true);
        ResetHitboxOrientation();

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
        if (isPlayerStateActive(PlayerState.Run) || isPlayerStateActive(PlayerState.Charge))
            MovePlayer(moveVector);

        if (ButtonPressed(Button.B) && (masterLogic.isGameStateActive(GameStateId.Battle)))
            stateMachine.ChangeState(PlayerState.Charge); //start attack

        else if (ButtonPressed(Button.A) && (masterLogic.isGameStateActive(GameStateId.Battle)))
        {
            isSwipingLeft = false;
            stateMachine.ChangeState(PlayerState.SwipeAttack); //start attack
        }
        else if (ButtonPressed(Button.X) && (masterLogic.isGameStateActive(GameStateId.Battle)))
        {
            isSwipingLeft = true;
            stateMachine.ChangeState(PlayerState.SwipeAttack); //start attack
        }
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
        if (!isPlayerStateActive(PlayerState.Charge))
            transform.LookAt(transform.position + movementVector);
        transform.Translate(movementVector * (isPlayerStateActive(PlayerState.Charge) ? ChargeAttackProperties.speedMultiplierWhileCharging : 1f), Space.World);
        model.transform.Rotate(new Vector3(7f, 0f, 0f) * (isPlayerStateActive(PlayerState.Charge) ? ChargeAttackProperties.speedMultiplierWhileCharging : 1f), Space.Self);
    }

    public void ReflectKnockbackTrajectory(Vector3 wallColliderNormal)
    {
        if (isPlayerStateActive(PlayerState.Hitstun) && masterLogic.isGameStateActive(GameStateId.Battle))
            knockbackTrajectory = Vector3.Reflect(knockbackTrajectory, wallColliderNormal).ApplyDirectionalInfluence(JoystickPosition, knockbackVelocity, masterLogic.DirectionalInfluenceMultiplier);
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

    private void ToggleHitbox(GameObject hitbox, bool isEnabled, bool alsoToggleVisual = true)
    {
        hitbox.GetComponent<BoxCollider>().enabled = isEnabled;
        if (alsoToggleVisual)
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

public static class VectorExtensions
{
    public static Vector3 ApplyDirectionalInfluence(this Vector3 origTrajectory, Vector2 JoystickPosition, float knockbackVelocity, float DirectionalInfluenceMultiplier)
    {
        Vector3 stickPos = new Vector3(JoystickPosition.x, 0f, JoystickPosition.y).normalized * DirectionalInfluenceMultiplier;
        Vector3 combined = stickPos + origTrajectory.normalized;
        combined = combined.normalized;
        return combined * knockbackVelocity;
    }
}
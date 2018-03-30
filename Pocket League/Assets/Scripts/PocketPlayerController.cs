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

    private Color color_idle = Color.blue,
                  color_run = Color.green,
                  color_charge = Color.yellow,
                  color_attack = Color.red,
                  color_hitstun = Color.magenta;

    private Vector3 moveVector = new Vector3();
    #endregion

    public void InitializePlayer(int playerId)
    {
        playerDetails = new PlayerDetails(playerId);
        masterLogic = GameObject.FindObjectOfType<MasterLogic>();
        model = transform.Find("PlayerModel").gameObject;
        model.GetComponent<Renderer>().material.color = color_idle;
        hitBox = transform.Find("hitbox").gameObject;
        hitBox.transform.localScale = masterLogic.smallHitboxScale;
        hitBox.transform.localPosition += new Vector3(0f, 0f, masterLogic.smallHitboxOffset);
        stateMachine.Subscribe(BeginCharge, StateId.Charge, true);
        stateMachine.Subscribe(AttackRecovery, StateId.AttackRecovery, true);
        stateMachine.Subscribe(GetHit, StateId.Hitstun, true);
        stateMachine.Subscribe(Idle, StateId.Idle, true);
        stateMachine.Subscribe(Run, StateId.Run, true);
        stateMachine.Subscribe(Dead, StateId.Dead, true);
    }

    void Dead()
    {
        model.GetComponent<Renderer>().material.color = new Color(.25f, .25f, .25f, 1f);
    }

    void Idle()
    {
        model.GetComponent<Renderer>().material.color = color_idle;
        hitBox.GetComponent<BoxCollider>().enabled = false;
        hitBox.GetComponent<MeshRenderer>().enabled = false;
    }

    void Run()
    {
        model.GetComponent<Renderer>().material.color = color_run;
        hitBox.GetComponent<BoxCollider>().enabled = false;
        hitBox.GetComponent<MeshRenderer>().enabled = false;
    }

    void BeginCharge()
    {
        model.GetComponent<Renderer>().material.color = color_charge;
        StopCoroutine("chargeAttack");
        StartCoroutine("chargeAttack");
    }

    void AttackRecovery()
    {
        model.GetComponent<Renderer>().material.color = color_attack;
        StopCoroutine("attackRecovery");
        StartCoroutine("attackRecovery");
    }

    void GetHit()
    {
        model.GetComponent<Renderer>().material.color = color_hitstun;
        StopCoroutine("attackRecovery");
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

        //vector A is the direction the player is facing 
        //vector B is the direction of the attacker to the victim
        Vector3 attackAngleTrajectory = (otherPlayer.transform.Find("hitbox").position - otherPlayer.transform.position).normalized;
        Vector3 playerAngleTrajectory = (transform.position - otherPlayer.transform.position).normalized;
        //couldn't decide which is better, so in-between seems like a good spot for now
        knockBackTrajectory = ((attackAngleTrajectory + playerAngleTrajectory) * .5f).normalized * velocity;

        for (int i = 0; i < Mathf.Floor(hitstunLength); i++)
        {
            transform.Translate(knockBackTrajectory, Space.World);
            yield return new WaitForEndOfFrame();
        }

        stateMachine.ChangeState(StateId.Idle, true, true);
    }

    private IEnumerator chargeAttack()
    {
        chargeCounter = 0;
        while (isCurrentCharacterState(StateId.Charge))
        {
            if (chargeCounter > masterLogic.maxChargeFrames)
            {   //held for maximum charge
                chargeCounter = masterLogic.maxChargeFrames;
                stateMachine.ChangeState(StateId.AttackRecovery);
            }
            else if (chargeCounter >= masterLogic.minChargeFrames && !ButtonList_OnKey.Contains(Button.X))
            {   //we let go of charge
                stateMachine.ChangeState(StateId.AttackRecovery);
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
            if (isCurrentCharacterState(StateId.AttackRecovery))
                yield return new WaitForEndOfFrame();
        }

        ToggleHitbox(hitBox, false);

        if (isCurrentCharacterState(StateId.AttackRecovery))
            stateMachine.ChangeState(StateId.Idle, forceTransition: true);
    }

    private void LateUpdate()
    {
        ButtonList_OnKey.Clear();
        ButtonList_OnKeyDown.Clear();
        ButtonList_OnKeyUp.Clear();

        if (playerDetails.id > 0)
        {
            vert = Input.GetAxis("Player" + playerDetails.id + "Vertical");
            horiz = Input.GetAxis("Player" + playerDetails.id + "Horizontal");

            //by normalizing the vector, we solve the 'diagonal is faster' problem
            moveVector = (new Vector3(horiz, 0f, vert).normalized) * masterLogic.playerSpeed;

            //only want to be moving during these scenes   
            if (masterLogic.isCurrentSceneState(SceneStateId.Battle) || masterLogic.isCurrentSceneState(SceneStateId.Results))
            {
                if (horiz != 0f || vert != 0f)
                    stateMachine.ChangeState(StateId.Run);
                else if (!isCurrentCharacterState(StateId.Dead))
                    stateMachine.ChangeState(StateId.Idle);

                //allow movement via joystick if we are running, or if we are in hitstun (when AllowMovementDuringHitstun=true)
                if (isCurrentCharacterState(StateId.Run) || (masterLogic.AllowMovementDuringHitstun && isCurrentCharacterState(StateId.Hitstun)))
                {
                    if (isCurrentCharacterState(StateId.Hitstun))
                        moveVector *= masterLogic.MovementReductionDuringHitstun;
                    else if (isCurrentCharacterState(StateId.Run))
                        transform.LookAt(transform.position + moveVector);

                    transform.Translate(moveVector, Space.World);
                }
            }

            //check all 10 xbox controller buttons
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown("joystick " + playerDetails.id + " button " + i)) { ButtonList_OnKeyDown.Add((Button)i); }
                if (Input.GetKeyUp("joystick " + playerDetails.id + " button " + i)) { ButtonList_OnKeyUp.Add((Button)i); }
                if (Input.GetKey("joystick " + playerDetails.id + " button " + i)) { ButtonList_OnKey.Add((Button)i); }
            }

            //start attack
            if (ButtonList_OnKeyDown.Contains(Button.X) && (masterLogic.isCurrentSceneState(SceneStateId.Battle) || masterLogic.isCurrentSceneState(SceneStateId.Results)))
                stateMachine.ChangeState(StateId.Charge);
            //skip the countdown
            if (ButtonList_OnKeyDown.Contains(Button.Start))
                masterLogic.stateMachine.ChangeState(SceneStateId.Battle);
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

    public bool isCurrentCharacterState(StateId state)
    {
        return (StateId)stateMachine.GetCurrentStateEnum() == state;
    }
    public StateId getCurrentStateIdEnum()
    {
        return (StateId)stateMachine.GetCurrentStateEnum();
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
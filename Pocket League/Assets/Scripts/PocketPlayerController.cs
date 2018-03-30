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

    private List<Button> ButtonList_OnKeyDown = new List<Button>();
    private List<Button> ButtonList_OnKeyUp = new List<Button>();
    private List<Button> ButtonList_OnKey = new List<Button>();

    private GameObject hitBox, model;

    private Color color_idle = Color.blue,
                  color_run = Color.green,
                  color_charge = Color.yellow,
                  color_attack = Color.red,
                  color_hitstun = Color.magenta;

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

        stateMachine.ChangeState(StateId.Idle);
    }

    private IEnumerator chargeAttack()
    {
        chargeCounter = 0;
        while ((StateId)stateMachine.GetCurrentStateEnum() == StateId.Charge)
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
        ToggleColliderAndMeshRenderer(hitBox, true);
        chargeMultiple = ((float)chargeCounter - masterLogic.minChargeFrames) / (masterLogic.maxChargeFrames - masterLogic.minChargeFrames);

        float framesToRecover = ScaleMultiplier(masterLogic.minAttackCooldownFrames, masterLogic.maxAttackCooldownFrames, chargeMultiple);
        float hitboxActivationMultiple = ScaleMultiplier(masterLogic.minAttackHitboxActivationFrames, masterLogic.maxAttackHitboxActivationFrames, chargeMultiple);
        float hitboxOffsetMultiple = ScaleMultiplier(masterLogic.smallHitboxOffset, masterLogic.bigHitboxOffset, chargeMultiple);

        hitBox.transform.localScale = ScaleMultiplier(masterLogic.smallHitboxScale, masterLogic.bigHitboxScale, chargeMultiple);
        hitBox.transform.localPosition = Vector3.zero + new Vector3(0f, 0f, hitboxOffsetMultiple);

        for (float i = 0; i < framesToRecover; i++)
        {
            if (i > hitboxActivationMultiple)
            {
                ToggleColliderAndMeshRenderer(hitBox, false);
            }
            yield return new WaitForEndOfFrame();
        }
        ToggleColliderAndMeshRenderer(hitBox, false);
        stateMachine.ChangeState(StateId.Idle);
    }

    private void ToggleColliderAndMeshRenderer(GameObject go, bool isEnabled)
    {
        go.GetComponent<BoxCollider>().enabled = isEnabled;
        go.GetComponent<MeshRenderer>().enabled = isEnabled;
    }

    private float ScaleMultiplier(float min, float max, float multiple)
    {
        return min + ((max - min) * multiple);
    }

    private Vector3 ScaleMultiplier(Vector3 min, Vector3 max, float multiple)
    {
        return min + ((max - min) * multiple);
    }

    private void LateUpdate()
    {
        if (playerDetails.id > 0 && !masterLogic.disablePlayersInputs)
        {
            // JOYSTICKS
            vert = Input.GetAxis("Player" + playerDetails.id + "Vertical");
            horiz = Input.GetAxis("Player" + playerDetails.id + "Horizontal");

            Vector3 moveVector = new Vector3(horiz, 0f, vert) * masterLogic.playerSpeed;

            if ((StateId)stateMachine.GetCurrentStateEnum() == StateId.Idle && (horiz != 0f || vert != 0f))
                stateMachine.ChangeState(StateId.Run);
            if ((StateId)stateMachine.GetCurrentStateEnum() == StateId.Run && (horiz == 0f && vert == 0f))
                stateMachine.ChangeState(StateId.Idle);

            if ((StateId)stateMachine.GetCurrentStateEnum() == StateId.Run || (masterLogic.AllowMovementDuringHitstun && (StateId)stateMachine.GetCurrentStateEnum() == StateId.Hitstun))
            {
                if ((StateId)stateMachine.GetCurrentStateEnum() == StateId.Hitstun)
                    moveVector *= masterLogic.MovementReductionDuringHitstun;

                transform.LookAt(transform.position + moveVector);
                transform.Translate(moveVector, Space.World);
            }

            // BUTTONS
            ButtonList_OnKey.Clear();
            ButtonList_OnKeyDown.Clear();
            ButtonList_OnKeyUp.Clear();

            for (int i = 0; i < 10; i++)
            {
                Button button = (Button)i;

                if (Input.GetKeyDown("joystick " + playerDetails.id + " button " + i))
                    ButtonList_OnKeyDown.Add(button);
                if (Input.GetKeyUp("joystick " + playerDetails.id + " button " + i))
                    ButtonList_OnKeyUp.Add(button);
                if (Input.GetKey("joystick " + playerDetails.id + " button " + i))
                    ButtonList_OnKey.Add(button);
            }
            if (ButtonList_OnKeyDown.Contains(Button.X))
            {
                stateMachine.ChangeState(StateId.Charge);
            }
            if (ButtonList_OnKeyDown.Contains(Button.Start))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (ButtonList_OnKeyDown.Contains(Button.Select))
            {
                masterLogic.viewDebug = !masterLogic.viewDebug;
            }
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
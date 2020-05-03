using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PocketPlayerController : Bolt.EntityBehaviour<IPocketPlayerState>
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
    public float chargeWeight = 0f, knockbackVelocity = 0f;
    [HideInInspector]
    public Vector2 JoystickPosition = new Vector2(0f, 0f);
    [HideInInspector]
    public GameObject otherPlayer;
    [HideInInspector]
    public PocketPlayerController otherPlayerController;
    [HideInInspector]
    public Vector3 currProjectileDir;

    [HideInInspector]
    public int chargeCounter = 0;
    [HideInInspector]
    public PlayerDetails playerDetails = new PlayerDetails(-1);
    [HideInInspector]
    public Vector3 knockbackTrajectory = new Vector3(0f, 0f, 0f);
    private int startCounter = 0;
    [HideInInspector]
    public bool isSwipingRight = false;
    private List<MasterLogic.Button> ButtonList_OnKeyDown = new List<MasterLogic.Button>();
    private List<MasterLogic.Button> ButtonList_OnKeyUp = new List<MasterLogic.Button>();
    private List<MasterLogic.Button> ButtonList_OnKey = new List<MasterLogic.Button>();

    [HideInInspector]
    public GameObject hitBox, hurtBox, model, line;
    private GameObject BallLight;
    private Color color_idle = Color.white,
                  color_run = Color.white,
                  color_charge = Color.yellow,
                  color_attack = Color.yellow,
                  color_hitstun = Color.magenta,
                  color_dead = new Color(.25f, .25f, .25f, 1f);
    [HideInInspector]
    public int framesWithoutTeleport = 0, framesWithoutProjectile = 0;
    [HideInInspector]
    public Transform hitboxHolder;
    private Vector3 moveVector = new Vector3();
    private bool hasController = false;
    [HideInInspector]
    public BufferButton bufferedButton = new BufferButton() { button = MasterLogic.Button.Select, framesLeft = 0 };
    public class BufferButton
    {
        public MasterLogic.Button button { get; set; }
        public int framesLeft { get; set; }
    }
    #endregion

    public override void Attached()
    {
        transform.position = new Vector3(-10f, .5f, 0f);
        state.SetTransforms(state.PocketPlayerTransform, transform);
        state.AddCallback("PocketPlayerS", () => {
            if(state.PocketPlayerS == 1)
            {
                isSwipingRight = false;
                stateMachine.ChangeState(PlayerState.SwipeAttack); //start attack
            }
        });
    }

    public override void SimulateOwner()
    {
        DaMove();
    }

    public void InitializePlayer(int playerId)
    {
        BallLight = GameObject.Find("Ball Light" + playerId);
        playerDetails = new PlayerDetails(playerId);
        masterLogic = GameObject.FindObjectOfType<MasterLogic>();

        SwipeAttackProperties = playerId == 1 ? masterLogic.swipeAttack_P1 : masterLogic.swipeAttack_P2;
        ChargeAttackProperties = playerId == 1 ? masterLogic.chargeAttack_P1 : masterLogic.chargeAttack_P2;
        ProjectileProperties = playerId == 1 ? masterLogic.projectile_P1 : masterLogic.projectile_P2;
        TeleportProperties = playerId == 1 ? masterLogic.teleport_P1 : masterLogic.teleport_P2;

        gameStateMachine = masterLogic.gameStateMachine;

        model = transform.Find("PlayerModel" + playerId).gameObject;
        model.SetActive(true);
        SetBallColor(color_idle, isBlank: true);
        line = transform.Find("Line").gameObject;
        hitboxHolder = transform.Find("HitboxHolder");
        hitBox = hitboxHolder.Find("hitbox").gameObject;
        hitBox.transform.localScale = ChargeAttackProperties.smallHitboxScale;
        hitBox.transform.localPosition += new Vector3(0f, 0f, ChargeAttackProperties.smallHitboxOffset);
        hurtBox = transform.Find("hurtbox").gameObject;

        stateMachine.Subscribe(BeginCharge, PlayerState.Charge, true);
        stateMachine.Subscribe(ChargeAttackRecovery, PlayerState.ChargeAttackRecovery, true);
        stateMachine.Subscribe(GetHitByAttack, PlayerState.Hitstun, true);
        stateMachine.Subscribe(GetHitByProjectile, PlayerState.BulletHitstun, true);
        stateMachine.Subscribe(Idle, PlayerState.Idle, true);
        stateMachine.Subscribe(Run, PlayerState.Run, true);
        stateMachine.Subscribe(Dead, PlayerState.Dead, true);
        stateMachine.Subscribe(SwipeAttack, PlayerState.SwipeAttack, true);
        stateMachine.Subscribe(Teleport, PlayerState.Teleport, true);
        stateMachine.Subscribe(Projectile, PlayerState.Projectile, true);
        stateMachine.Subscribe(Actionable, PlayerState.Actionable, true);

        hasController = Input.GetJoystickNames().Length >= playerId;

        framesWithoutTeleport = TeleportProperties.rechargeFrames;
        framesWithoutProjectile = ProjectileProperties.rechargeFrames;
    }

    void Dead()
    {
        StopAllCoroutines();
        SetBallColor(color_dead);
    }

    void Idle()
    {
        SetBallColor(color_idle, isBlank: true);
        ToggleHitbox(hitBox, false);
    }

    void Run()
    {
        StopAllCoroutines();
        SetBallColor(color_run, isBlank: true);
        ToggleHitbox(hitBox, false);
    }

    void BeginCharge()
    {
        StopAllCoroutines();
        chargeCounter = 0;
        SetBallColor(color_charge, isBlank: true);
        StartCoroutine("chargeAttack");
    }

    void ChargeAttackRecovery()
    {
        StopAllCoroutines();
        line.SetActive(false);
        SetBallColor(color_attack, isBlank: true);
        StartCoroutine("chargeAttackRecovery");
    }

    void GetHitByAttack()
    {
        GetHit(false);
    }

    void GetHitByProjectile()
    {
        GetHit(true);
    }

    void GetHit(bool hitByProjectile)
    {
        SetBallColor(color_hitstun);
        StopAllCoroutines();
        line.SetActive(false);
        ResetHitboxOrientation();
        model.GetComponent<MeshRenderer>().enabled = true;
        ToggleHitbox(hurtBox, true, false);
        ToggleHitbox(hitBox, false, true);

        if (hitByProjectile)
        {
            StartCoroutine("getHitByProjectile");
        }
        else
        {
            switch (otherPlayerController.stateMachine.GetCurrentStateEnum())
            {
                case PlayerState.ChargeAttackRecovery: StartCoroutine("getHitByChargeAttack"); break;
                case PlayerState.SwipeAttack: StartCoroutine("getHitBySwipeAttack"); break;
                default: Debug.LogError("WEIRD! WE GOT HIT BY: " + otherPlayerController.stateMachine.GetCurrentStateEnum().ToString()); StartCoroutine("getHitByProjectile"); break;
            }
        }
    }


    void SwipeAttack()
    {
        StopAllCoroutines();
        SetBallColor(color_charge, isBlank: true);
        StartCoroutine("swipeAttack");
    }

    void Teleport()
    {
        StopAllCoroutines();
        ToggleHitbox(hitBox, false, true);
        model.GetComponent<MeshRenderer>().enabled = true;
        framesWithoutTeleport = 0;
        SetBallColor(color_charge, isBlank: true);
        StartCoroutine("teleport");
    }

    void Projectile()
    {
        StopAllCoroutines();
        framesWithoutProjectile = 0;
        SetBallColor(color_charge, isBlank: true);
        StartCoroutine("projectile");
    }

    void Actionable()
    {
        line.SetActive(false);
        ResetHitboxOrientation();
        model.GetComponent<MeshRenderer>().enabled = true;
        ToggleHitbox(hurtBox, true, false);
        ToggleHitbox(hitBox, false, false);
    }

    private IEnumerator chargeAttack()
    {
        while (isPlayerStateActive(PlayerState.Charge))
        {
            float weight = (float)chargeCounter / (float)ChargeAttackProperties.maxChargeFrames;

            SetBallColor(new Color(weight, 0f, 0f) * 1.5f, false, weight * 1.5f);
            if (chargeCounter > ChargeAttackProperties.minChargeFrames + 15)
            {
                line.SetActive(true);
            }

            if (chargeCounter > ChargeAttackProperties.maxChargeFrames)
            {   //held for maximum charge
                chargeCounter = ChargeAttackProperties.maxChargeFrames;
                stateMachine.ChangeState(PlayerState.ChargeAttackRecovery);
            }
            else if (chargeCounter >= ChargeAttackProperties.minChargeFrames && !ButtonList_OnKey.Contains(GetButtonMappingForMove("ChargeAttack")))
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

    private IEnumerator getHitByChargeAttack()
    {
        //disable opponents hitbox until their next attack
        ToggleHitbox(otherPlayerController.hitBox, isEnabled: false, alsoToggleVisual: false);

        float _chargeWeight = otherPlayer.GetComponent<PocketPlayerController>().chargeWeight;
        float _percentWeight = playerDetails.percent / 100f;

        //add percent from being hit
        playerDetails.percent += ScaleMultiplier(ChargeAttackProperties.minPercentDealt, ChargeAttackProperties.maxPercentDealt, _chargeWeight);

        //hitstun length, and velocity (based on percent and charge)
        float hitstunLength = ScaleMultiplier(ChargeAttackProperties.minHitstunLength, ChargeAttackProperties.maxHitstunLength, _percentWeight);
        knockbackVelocity = ScaleMultiplier(ChargeAttackProperties.minKnockbackVelocity, ChargeAttackProperties.maxKnockbackVelocity, _chargeWeight);
        knockbackVelocity += ScaleMultiplier(ChargeAttackProperties.minKnockbackVelocityAdditionFromPercent, ChargeAttackProperties.maxKnockbackVelocityAdditionFromPercent, _percentWeight);

        //direction the player is attacking:
        Vector3 attackAngleTrajectory = (otherPlayer.transform.position - otherPlayer.transform.Find("Front").position).normalized;
        //relative angle between players
        Vector3 playerAngleTrajectory = (transform.position - otherPlayer.transform.position).normalized;

        knockbackTrajectory = (attackAngleTrajectory.normalized * knockbackVelocity * -1f).ApplyDirectionalInfluence(JoystickPosition, knockbackVelocity, masterLogic.DirectionalInfluenceMultiplier);

        for (int i = 0; i < Mathf.Floor(hitstunLength); i++)
        {
            MovePlayer(knockbackTrajectory);
            yield return new WaitForEndOfFrame();
        }

        stateMachine.ChangeState(PlayerState.Actionable);
    }

    private IEnumerator chargeAttackRecovery()
    {
        ResetHitboxOrientation();

        chargeWeight = ((float)chargeCounter - ChargeAttackProperties.minChargeFrames) / (ChargeAttackProperties.maxChargeFrames - ChargeAttackProperties.minChargeFrames);

        float framesToRecover = ScaleMultiplier(ChargeAttackProperties.minAttackCooldownFrames, ChargeAttackProperties.maxAttackCooldownFrames, chargeWeight);
        float hitboxActivationFrames = ScaleMultiplier(ChargeAttackProperties.minAttackHitboxActivationFrames, ChargeAttackProperties.maxAttackHitboxActivationFrames, chargeWeight);
        float hitboxOffset = ScaleMultiplier(ChargeAttackProperties.smallHitboxOffset, ChargeAttackProperties.bigHitboxOffset, chargeWeight);

        hitBox.transform.localScale = ScaleMultiplier(ChargeAttackProperties.smallHitboxScale, ChargeAttackProperties.bigHitboxScale, chargeWeight);
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
        for (int i = 0; i < SwipeAttackProperties.startupFrames; i++)
            yield return new WaitForEndOfFrame();

        ToggleHitbox(hitBox, true, true);

        if (SwipeAttackProperties.startingYRotationOffset < 0 && !isSwipingRight)
            SwipeAttackProperties.startingYRotationOffset *= -1f;
        if (SwipeAttackProperties.startingYRotationOffset > 0 && isSwipingRight)
            SwipeAttackProperties.startingYRotationOffset *= -1f;

        hitBox.transform.localScale = SwipeAttackProperties.hitBoxScale;
        hitBox.transform.localPosition = SwipeAttackProperties.startingOffset;
        hitboxHolder.localEulerAngles = new Vector3(0f, SwipeAttackProperties.startingYRotationOffset, 0f);

        float increment = (SwipeAttackProperties.startingYRotationOffset * 2f) / (float)SwipeAttackProperties.framesToSwipe;

        for (int i = 0; i < SwipeAttackProperties.framesToSwipe; i++)
        {
            hitboxHolder.Rotate(new Vector3(0f, -increment, 0f), Space.Self);
            yield return new WaitForEndOfFrame();
        }

        for (int i = 0; i < SwipeAttackProperties.hitboxLingerFrames; i++)
            yield return new WaitForEndOfFrame();

        ToggleHitbox(hitBox, false, true);

        for (int i = 0; i < SwipeAttackProperties.attackLagFrames; i++)
            yield return new WaitForEndOfFrame();

        stateMachine.ChangeState(PlayerState.Actionable);
        state.PocketPlayerS = 0;
    }

    private IEnumerator getHitBySwipeAttack()
    {
        //disable opponents hitbox until their next attack
        ToggleHitbox(otherPlayerController.hitBox, isEnabled: false, alsoToggleVisual: false);

        float _percentWeight = playerDetails.percent / 100f;

        //add percent from being hit
        playerDetails.percent += SwipeAttackProperties.percentDealt;

        //hitstun length, and velocity (based on percent and charge)
        float hitstunLength = ScaleMultiplier(SwipeAttackProperties.minHitstunLength, SwipeAttackProperties.maxHitstunLength, _percentWeight);
        knockbackVelocity = ScaleMultiplier(SwipeAttackProperties.minKnockbackVelocityAdditionFromPercent, SwipeAttackProperties.maxKnockbackVelocityAdditionFromPercent, _percentWeight);

        //direction the player is attacking:
        //Vector3 attackAngleTrajectory = (otherPlayer.transform.position - otherPlayer.transform.Find("Front").position).normalized;
        //relative angle between players, rotated some depending on swipe direction
        Vector3 playerAngleTrajectory = Vector3.zero;


        playerAngleTrajectory = (otherPlayer.transform.position - transform.position).normalized;
        if (SwipeAttackProperties.usePaddleKnockbackDir)
            playerAngleTrajectory = Quaternion.AngleAxis(otherPlayerController.isSwipingRight ? 20f : -20f, Vector3.up) * playerAngleTrajectory;

        knockbackTrajectory = (playerAngleTrajectory.SetY(0f).normalized * knockbackVelocity * -1f).ApplyDirectionalInfluence(JoystickPosition, knockbackVelocity, masterLogic.DirectionalInfluenceMultiplier);

        for (int i = 0; i < Mathf.Floor(hitstunLength); i++)
        {
            MovePlayer(knockbackTrajectory);
            yield return new WaitForEndOfFrame();
        }

        stateMachine.ChangeState(PlayerState.Actionable);
    }

    private IEnumerator teleport()
    {
        for (int i = 0; i < TeleportProperties.startupFrames; i++)
            yield return new WaitForEndOfFrame();

        Vector3 startPos = transform.position;
        Vector3 target = transform.position + (moveVector.normalized * TeleportProperties.teleportDistance);

        //if we would be teleporting into a wall, set our new target to be within the game's bounds
        RaycastHit[] hits = Physics.RaycastAll(transform.position, (target - startPos).normalized, TeleportProperties.teleportDistance);
        foreach (RaycastHit hit in hits.Where(h => h.transform.tag == "Wall").ToArray())
            target = Vector3.MoveTowards(hit.point, Vector3.zero, 1f).SetY(transform.position.y);

        if (TeleportProperties.modelHiddenWhileTeleporting)
            model.GetComponent<MeshRenderer>().enabled = false;
        if (TeleportProperties.invincibleWhileTeleporting)
            ToggleHitbox(hurtBox, false, false);

        for (int i = 0; i < TeleportProperties.framesToTeleport; i++)
        {
            float weight = (float)i / (float)TeleportProperties.framesToTeleport;
            transform.position = Vector3.Lerp(startPos, target, weight);
            yield return new WaitForEndOfFrame();
        }
        transform.position = target;

        ToggleHitbox(hurtBox, true, false);
        model.GetComponent<MeshRenderer>().enabled = true;

        for (int i = 0; i < TeleportProperties.lagFrames; i++)
            yield return new WaitForEndOfFrame();

        stateMachine.ChangeState(PlayerState.Actionable);
    }

    private IEnumerator projectile()
    {
        for (int i = 0; i < ProjectileProperties.startupFrames; i++)
            yield return new WaitForEndOfFrame();

        GameObject _projectile = GameObject.Instantiate(ProjectileProperties.projectileFab);
        _projectile.name = "Projectile (Player " + playerDetails.id + ")";
        _projectile.transform.parent = transform;
        _projectile.transform.localEulerAngles = Vector3.zero;
        _projectile.transform.localPosition = ProjectileProperties.spawnOffset;
        _projectile.transform.parent = null;

        Bullet bullet = _projectile.GetComponent<Bullet>();
        bullet.speed = ProjectileProperties.projectileSpeed;
        bullet.dir = (transform.Find("Front").position - transform.position).normalized;
        bullet.playerOwner = gameObject;
        bullet.framesProjectileIsAlive = ProjectileProperties.projectileLifeFrames;

        for (int i = 0; i < ProjectileProperties.lagFrames; i++)
            yield return new WaitForEndOfFrame();

        stateMachine.ChangeState(PlayerState.Actionable);
    }

    private IEnumerator getHitByProjectile()
    {
        //add percent from being hit
        playerDetails.percent += otherPlayerController.ProjectileProperties.percentDealt;

        //hitstun length, and velocity (based on percent and charge)
        float hitstunLength = otherPlayerController.ProjectileProperties.stunFrames;
        knockbackVelocity = otherPlayerController.ProjectileProperties.stunSpeed;

        Vector3 dir = currProjectileDir.SetY(0f).normalized;
        knockbackTrajectory = (dir * knockbackVelocity).ApplyDirectionalInfluence(JoystickPosition, knockbackVelocity, masterLogic.DirectionalInfluenceMultiplier);

        for (int i = 0; i < Mathf.Floor(hitstunLength); i++)
        {
            MovePlayer(knockbackTrajectory);
            yield return new WaitForEndOfFrame();
        }

        stateMachine.ChangeState(PlayerState.Actionable);
    }

    private void DaMove()
    {
        if (masterLogic == null) return;

        stateMachine.ChangeState(PlayerState.Idle);
        GetInputs();

        //by normalizing the vector, we solve the 'diagonal is faster' problem
        moveVector = new Vector3(JoystickPosition.x, 0f, JoystickPosition.y).normalized;

        //slide along the wall if near
        RaycastHit[] hits = (Physics.RaycastAll(transform.position, moveVector, .8f));
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

        if (masterLogic.isGameStateActive(GameStateId.Battle))
        {
            if (ButtonPressed(GetButtonMappingForMove("ChargeAttack")))
            {
                stateMachine.ChangeState(PlayerState.Charge); //start attack
            }
            else if (ButtonPressed(GetButtonMappingForMove("SwipeAttackLeft")))
            {
                //isSwipingRight = false;
                //stateMachine.ChangeState(PlayerState.SwipeAttack); //start attack

                state.PocketPlayerS = 1;
            }
            else if (ButtonPressed(GetButtonMappingForMove("SwipeAttackRight")))
            {
                //isSwipingRight = true;
                //stateMachine.ChangeState(PlayerState.SwipeAttack); //start attack
            }
            else if (ButtonPressed(GetButtonMappingForMove("Teleport")) && CanTeleport())
            {
                stateMachine.ChangeState(PlayerState.Teleport);
            }
            else if (ButtonPressed(GetButtonMappingForMove("Projectile")) && CanShootProjectile())
            {
                stateMachine.ChangeState(PlayerState.Projectile);
            }
        }

        if (ButtonPressed(MasterLogic.Button.Start))
            gameStateMachine.ChangeState(GameStateId.Battle); //skip the countdown
        if (ButtonPressed(MasterLogic.Button.Select))
            masterLogic.viewDebug = !masterLogic.viewDebug; //view debug info

        if (ButtonHeld(MasterLogic.Button.Start))
        {
            startCounter++; //hold start for 55 frames to reload the scene
            if (startCounter > 55) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        }
        else { startCounter = 0; }

        if (!isPlayerStateActive(PlayerState.Teleport))
            framesWithoutTeleport++;
        if (!isPlayerStateActive(PlayerState.Projectile))
            framesWithoutProjectile++;

        BallLight.GetComponent<Light>().color = CanTeleport() && ((!isPlayerStateActive(PlayerState.Hitstun)) && (!isPlayerStateActive(PlayerState.BulletHitstun))) ? Color.cyan : Color.white;
        BallLight.GetComponent<Light>().intensity = CanTeleport() && ((!isPlayerStateActive(PlayerState.Hitstun)) && (!isPlayerStateActive(PlayerState.BulletHitstun))) ? 8f : 1.62f;

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

            bufferedButton.framesLeft -= 1;
            if (bufferedButton.framesLeft < 0)
                bufferedButton = new BufferButton() { button = MasterLogic.Button.Select, framesLeft = 0 };
        }
        else
        {
            RegisterKeyboardInputs(KeyCode.J, 2);
            RegisterKeyboardInputs(KeyCode.K, 7);
            RegisterKeyboardInputs(KeyCode.L, 6);
        }
    }

    public void RegisterKeyboardInputs(KeyCode keycode, int buttonNumber)
    {
        if (Input.GetKeyDown(keycode)) ButtonList_OnKeyDown.Add((MasterLogic.Button)buttonNumber);
        if (Input.GetKeyUp(keycode)) ButtonList_OnKeyUp.Add((MasterLogic.Button)buttonNumber);
        if (Input.GetKey(keycode)) ButtonList_OnKey.Add((MasterLogic.Button)buttonNumber);
    }

    public void RegisterControllerInputs(string keycode, int buttonNumber)
    {
        if (Input.GetKeyDown(keycode))
        {
            ButtonList_OnKeyDown.Add((MasterLogic.Button)buttonNumber);
            bufferedButton = new BufferButton() { button = (MasterLogic.Button)buttonNumber, framesLeft = masterLogic.bufferWindow + 1 };
        }
        if (Input.GetKeyUp(keycode)) ButtonList_OnKeyUp.Add((MasterLogic.Button)buttonNumber);
        if (Input.GetKey(keycode)) ButtonList_OnKey.Add((MasterLogic.Button)buttonNumber);

    }

    public bool ButtonReleased(MasterLogic.Button button)
    {
        return ButtonList_OnKeyUp.Contains(button);
    }

    public bool ButtonPressed(MasterLogic.Button button)
    {
        return (ButtonList_OnKeyDown.Contains(button) || (bufferedButton.button == button && bufferedButton.framesLeft > 0));
    }

    private void OnGUI()
    {
        if (playerDetails.id == 1)
        {
            GUILayout.Label("BUFFERED BUTTON: " + bufferedButton.button + " - " + bufferedButton.framesLeft);
            GUILayout.Label("BUFFERED BUTTON: " + bufferedButton.button + " - " + bufferedButton.framesLeft);
            GUILayout.Label("BUFFERED BUTTON: " + bufferedButton.button + " - " + bufferedButton.framesLeft);
        }
    }

    public bool ButtonHeld(MasterLogic.Button button)
    {
        return ButtonList_OnKey.Contains(button);
    }

    public bool CanTeleport()
    {
        return framesWithoutTeleport >= TeleportProperties.rechargeFrames;
    }

    public bool CanShootProjectile()
    {
        return framesWithoutProjectile >= ProjectileProperties.rechargeFrames;
    }

    private void ResetHitboxOrientation()
    {
        hitboxHolder.localPosition = Vector3.zero;
        hitBox.transform.localPosition = Vector3.zero;
        hitboxHolder.localEulerAngles = Vector3.zero;
        hitBox.transform.localEulerAngles = Vector3.zero;
    }

    public void MovePlayer(Vector3 movementVector)
    {
        if (!ChargeAttackProperties.rotationLockedDuringCharge || !isPlayerStateActive(PlayerState.Charge))
            transform.LookAt(transform.position + movementVector);
        transform.Translate(movementVector * (isPlayerStateActive(PlayerState.Charge) ? ChargeAttackProperties.speedMultiplierWhileCharging : 1f), Space.World);
        if (movementVector != Vector3.zero)
            model.transform.Rotate(new Vector3(7f, 0f, 0f) * (isPlayerStateActive(PlayerState.Charge) ? ChargeAttackProperties.speedMultiplierWhileCharging : 1f), Space.Self);
    }

    public void ReflectKnockbackTrajectory(Vector3 wallColliderNormal)
    {
        if (isPlayerStateActive(PlayerState.Hitstun) && masterLogic.isGameStateActive(GameStateId.Battle))
            knockbackTrajectory = Vector3.Reflect(knockbackTrajectory, wallColliderNormal).ApplyDirectionalInfluence(JoystickPosition, knockbackVelocity, masterLogic.DirectionalInfluenceMultiplier);
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

    public MasterLogic.Button GetButtonMappingForMove(string moveType)
    {
        switch (moveType)
        {
            case "Projectile": return playerDetails.id == 1 ? masterLogic.projectile_btn_P1 : masterLogic.projectile_btn_P2;
            case "Teleport": return playerDetails.id == 1 ? masterLogic.teleport_btn_P1 : masterLogic.teleport_btn_P2;
            case "ChargeAttack": return playerDetails.id == 1 ? masterLogic.chargeAttack_btn_P1 : masterLogic.chargeAttack_btn_P2;
            case "SwipeAttackLeft": return playerDetails.id == 1 ? masterLogic.swipeAttack_btn_L_P1 : masterLogic.swipeAttack_btn_L_P2;
            case "SwipeAttackRight": return playerDetails.id == 1 ? masterLogic.swipeAttack_btn_R_P1 : masterLogic.swipeAttack_btn_R_P2;
            default: return MasterLogic.Button.Start;
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
        if (origTrajectory != Vector3.zero)
        {
            Vector3 stickPos = new Vector3(JoystickPosition.x, 0f, JoystickPosition.y).normalized * DirectionalInfluenceMultiplier;
            Vector3 combined = stickPos + origTrajectory.normalized;
            combined = combined.normalized;
            return combined * knockbackVelocity;
        }

        return origTrajectory;
    }

    public static Vector3 SetY(this Vector3 origVector, float y)
    {
        return new Vector3(origVector.x, y, origVector.z);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class HurtboxLogic : MonoBehaviour
{
    private MasterLogic masterLogic;
    private PocketPlayerController pocketController;

    void Awake()
    {
        masterLogic = FindObjectOfType<MasterLogic>();
        pocketController = transform.parent.GetComponent<PocketPlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!masterLogic.isGameStateActive(GameStateId.Results))
        {
            if (other.tag == "Wall" && pocketController.isPlayerStateActive(PlayerState.Hitstun) && masterLogic.isGameStateActive(GameStateId.Battle))
                pocketController.knockBackTrajectory = Vector3.Reflect(pocketController.knockBackTrajectory, other.transform.position.normalized);
            else if (other.tag == "Hole" && masterLogic.isGameStateActive(GameStateId.Battle))
                masterLogic.KillPlayer(pocketController, other.gameObject);
        }
    }

}

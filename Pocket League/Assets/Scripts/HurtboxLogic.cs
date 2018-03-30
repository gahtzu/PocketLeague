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
        if (!masterLogic.isCurrentSceneState(SceneStateId.Results))
        {
            if (other.tag == "Wall" && pocketController.isCurrentCharacterState(StateId.Hitstun) && masterLogic.isCurrentSceneState(SceneStateId.Battle))
                pocketController.knockBackTrajectory = Vector3.Reflect(pocketController.knockBackTrajectory, other.transform.position.normalized);
            else if (other.tag == "Hole" && masterLogic.isCurrentSceneState(SceneStateId.Battle))
                masterLogic.KillPlayer(pocketController, other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Wall" && !masterLogic.isCurrentSceneState(SceneStateId.Countdown))
        {
            transform.parent.position = other.ClosestPointOnBounds(transform.parent.position);
            transform.parent.Translate(other.transform.position.normalized * -1f * .5f, Space.World);
        }
    }
}

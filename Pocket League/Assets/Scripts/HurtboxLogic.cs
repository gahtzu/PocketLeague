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

    private void OnTriggerEnter(Collider collider)
    {
        if (!masterLogic.isGameStateActive(GameStateId.Results))
        {
            if (collider.tag == "Wall")
                pocketController.ReflectKnockbackTrajectory(collider.transform.position.normalized);
            else if (collider.tag == "Hole" && masterLogic.isGameStateActive(GameStateId.Battle))
                masterLogic.KillPlayer(pocketController, collider.gameObject);
        }
    }
}

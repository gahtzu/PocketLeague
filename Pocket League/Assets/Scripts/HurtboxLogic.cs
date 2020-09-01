using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private void LateUpdate()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, pocketController.knockbackTrajectory.normalized, pocketController.knockbackVelocity*2f);
        foreach (RaycastHit hit in hits.Where(h => h.transform.tag == "Wall").ToArray())
            pocketController.ReflectKnockbackTrajectory(hit.collider.transform.position.normalized);

    }
    private void OnTriggerEnter(Collider collider)
    {
        if (!masterLogic.gameStateMachine.IsStateActive(GameStateId.Results))
        {
             if (collider.tag == "Hole" && masterLogic.gameStateMachine.IsStateActive(GameStateId.Battle))
                masterLogic.KillPlayer(pocketController, collider.gameObject);
        }
    }
}

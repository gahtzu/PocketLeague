using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class HurtboxLogic : MonoBehaviour
{
    private MasterLogic masterLogic;

    void Start()
    {
        masterLogic = FindObjectOfType<MasterLogic>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Wall")
        {
            PocketPlayerController pocketController = transform.parent.gameObject.GetComponent<PocketPlayerController>();
            pocketController.trajectory = Vector3.Reflect(pocketController.trajectory, other.transform.position.normalized);
        }
        else if (other.tag == "Hole")
        {
            masterLogic.LoseStock(transform.parent.GetComponent<PocketPlayerController>());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        StateId currentState = (StateId)transform.parent.gameObject.GetComponent<PocketPlayerController>().stateMachine.GetCurrentStateEnum();
        if (other.tag == "Wall")
        {
            print(other.ClosestPointOnBounds(transform.position));
            transform.parent.position = other.ClosestPointOnBounds(transform.parent.position);
            transform.parent.Translate(other.transform.position.normalized * -1f * .5f, Space.World);
        }
    }
}

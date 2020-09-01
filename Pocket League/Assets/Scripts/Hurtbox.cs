using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Hurtbox : MonoBehaviour
{
    // anyway to seperate this out?
    private PocketPlayerController pocketController;

    [HideInInspector]
    public CollisionsStateMachine collisionsStateMachine = new CollisionsStateMachine();

    void Start()
    {
        pocketController = transform.parent.GetComponent<PocketPlayerController>();
    }

    private void LateUpdate()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, pocketController.knockbackTrajectory.normalized, pocketController.knockbackVelocity * 2f);

        // TODO: use a layermask ?
        //      instead of knockback trajectory, have general ray for "where the object is currently moving" whether its from knockback or normal movement
        foreach (RaycastHit hit in hits)
        {
            switch (hit.transform.tag)
            {
                case "Wall":
                {
                    collisionsStateMachine.ChangeState(CollisionsStateMachine.Id.Wall, hit.collider.transform);
                    return;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        switch (collider.tag)
        {
            case "Hole":
            {
                collisionsStateMachine.ChangeState(CollisionsStateMachine.Id.Hole, collider.transform);
                return;
            }
        }
    }
}


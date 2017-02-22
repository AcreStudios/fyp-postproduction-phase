using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CivillianAI : AIFunctions {

    public enum Actions {
        RandomWorld, RunFromPlayer
    }

    public Actions actions;

    public float attentionSpan = 5;
    AIOverseer.TaskLocation instance;
    float timer;

    Renderer colorChanging;

    void Start() {
        animator = GetComponent<Animator>();

        colorChanging = GetComponent<Renderer>();
        AIOverseer.instance.civillianAIList.Add(this);
        gameObject.tag = "Civillian";
        gameObject.name = (AIOverseer.instance.civillianAIList.Count - 1).ToString();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        instance = new AIOverseer.TaskLocation();
    }

    void Update() {
        switch (actions) {
            case Actions.RandomWorld:
                if (!(agent.velocity.sqrMagnitude > 0)) {
                    Vector3 pos = AIOverseer.instance.RandomWorldLocation();
                    destination = ArcBasedPosition(pos - transform.position, pos, range);
                }
                break;

            case Actions.RunFromPlayer:
                destination = ArcBasedPosition(target.position - transform.position, target.position, range);
                break;
        }

        if (agent.velocity.sqrMagnitude > 0)
            animator.SetInteger("TreeState", 1);
        else
            animator.SetInteger("TreeState", 0);

        agent.destination = destination;
        destinationMarker.transform.position = destination;
    }

    public override void DamageRecieved() {
        base.DamageRecieved();
    }

    public override void FindTarget() {
        actions = Actions.RunFromPlayer;
        agent.speed = 3.5f;
        animator.speed = 1;
        base.FindTarget();
    }
}

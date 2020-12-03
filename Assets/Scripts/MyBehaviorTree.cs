using UnityEngine;
using System;
using System.Collections;
using TreeSharpPlus;

public class MyBehaviorTree : MonoBehaviour
{
    public Transform wander1;
    public Transform wander2;
    public Transform wander3;
    public GameObject participant;

    private BehaviorAgent behaviorAgent;

    void Start()
    {
        behaviorAgent = new BehaviorAgent(this.BuildTreeRoot());
        BehaviorManager.Instance.Register(behaviorAgent);
        behaviorAgent.StartBehavior();
    }
    
    void Update()
    {

    }
    
	protected Node ST_ApproachAndWait(Transform target)
	{
		Val<Vector3> position = Val.V (() => target.position);
		return new Sequence( participant.GetComponent<BehaviorMecanim>().Node_GoTo(position), new LeafWait(1000));
	}
    
    protected Node BuildTreeRoot()
    {
        Node roaming = new DecoratorLoop(
            new DecoratorForceStatus(RunStatus.Success,
                new Sequence(
                    new LeafInvoke(() => {
                        if (Time.time < 5)
                        {
                            return RunStatus.Running;
                        } else
                        {
                            return RunStatus.Success;
                        }
                    }),
                    new LeafInvoke(() => {
                        print("AWEFAWEFAWEF");
                    }),
                    this.ST_ApproachAndWait(this.wander1),
                    this.ST_ApproachAndWait(this.wander2),
                    this.ST_ApproachAndWait(this.wander3)
                )
            )
        );
        return roaming;
    }
}

using UnityEngine;
using System;
using System.Collections;
using TreeSharpPlus;
using Random = UnityEngine.Random;

public class MyBehaviorTree3 : MonoBehaviour
{
	public Transform[] meetingPoint;
	public GameObject[] person1;
	public GameObject[] person2;
    public BoxCollider[] bounds;
    public GameObject user;
    public GameObject[] doors;

    private BehaviorAgent behaviorAgent;

	void Start ()
	{
		behaviorAgent = new BehaviorAgent (this.BuildTreeRoot ());
		BehaviorManager.Instance.Register (behaviorAgent);
		behaviorAgent.StartBehavior();
    }
    
	void Update ()
	{
        if (Input.GetMouseButton(1))
        {
            Blackboard.StoryArcs.currArc = Blackboard.StoryArc.ROOM_2;
        }
	}

	protected Node GoTo(GameObject agent, Transform target)
	{
		Val<Vector3> position = Val.V(() => target.position);
		return (
            agent.GetComponent<BehaviorMecanim>().Node_GoToUpToRadius(position, 2.5f)
        );
	}

    protected Node Greet(GameObject agent1)
    {
        return (
            //agent1.GetComponent<BehaviorMecanim>().ST_TurnToFace(Val.V(() => agent2.transform.position)),
            agent1.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "WAVE"), Val.V(() => (long)2000))
        );
    }

    protected Node Speak(GameObject agent1, long ms = 2000)
    {
        return agent1.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "THINK"), Val.V(() => ms));
    }

    protected Node SpeakUI(GameObject agent1, string text)
    {
        return new SequenceParallel(
            Speak(agent1),
            new LeafInvoke(() => {
                if (DialogueUI.Available())
                {
                    if (DialogueUI.Finished(text))
                    {
                        return RunStatus.Success;
                    }
                    else
                    {
                        DialogueUI.SetText(text);
                        return RunStatus.Running;
                    }
                }
                else
                {
                    return RunStatus.Running;
                }
            }),
            new LeafWait(1000)
        );
    }

    protected Node CloseDoors()
    {
        return new LeafInvoke(() => {
            foreach (var door in doors)
            {
                door.SetActive(true);
            }
        });
    }

    protected Node OpenDoors()
    {
        return new LeafInvoke(() => {
            foreach (var door in doors)
            {
                door.SetActive(false);
            }
        });
    }

    protected Node RoomBehavior(int i)
    {
        var dialogueSubtree = new Sequence(
            SpeakUI(person1[i], "Agent 1:How are you?"),
            SpeakUI(person2[i], "Agent 2:I'm doing well. How are you?"),
            SpeakUI(person1[i], "Agent 1:I'm doing fine as well. Thanks for asking.")
        );

        return (
            //new DecoratorLoop(
            //    new LeafInvoke(() => {
            //        UserMoveScript.moving = false;
            //    })
            //),
            new Sequence(
                new LeafWait(1000),
                CloseDoors(),
                new SequenceParallel(
                    GoTo(person1[i], meetingPoint[i]),
                    GoTo(person2[i], meetingPoint[i])
                ),
                new SequenceParallel(
                    Greet(person1[i]),
                    Greet(person2[i])
                ),
                new LeafWait(2000),
                dialogueSubtree,
                new LeafInvoke(() => {
                    UserMoveScript.moving = true;
                }),
                OpenDoors()
            )
        );
    }
    
    protected Node UserInput()
    {
        return new DecoratorLoop(
            new LeafInvoke(() => {
                for (int i = 0; i < 3; i++)
                {
                    //if (PointInOABB(user.transform.position, bounds[i]))
                    if (UserMoveScript.boxes.Contains(bounds[i]))
                    {
                        Blackboard.UserInput.roomNumber = i;

                        return;
                    }
                }
                Blackboard.UserInput.roomNumber = -1;
            })
        );
    }

    protected Node CheckArc(int i)
    {
        return new Sequence(
            new LeafAssert(() => i == Blackboard.UserInput.roomNumber),
            new LeafInvoke(() => {
                Blackboard.StoryArcs.currArc = (Blackboard.StoryArc)i;
            })
        );
    }

    protected Node MaintainArcs()
    {
        return new DecoratorLoop(
            //new Selector(
            //    CheckArc(-1),
            //    CheckArc(0),
            //    CheckArc(1),
            //    CheckArc(2)
            //)
            new LeafInvoke(() => {
                switch (Blackboard.UserInput.roomNumber)
                {
                    case -1:
                        Blackboard.StoryArcs.currArc = Blackboard.StoryArc.NO_ROOM;
                        break;
                    case 0:
                        Blackboard.StoryArcs.currArc = Blackboard.StoryArc.ROOM_0;
                        break;
                    case 1:
                        Blackboard.StoryArcs.currArc = Blackboard.StoryArc.ROOM_1;
                        break;
                    case 2:
                        Blackboard.StoryArcs.currArc = Blackboard.StoryArc.ROOM_2;
                        break;
                }
            })
        );
    }

    protected Node SelectNoStory()
    {
        return new SelectorParallel(
            new DecoratorInvert(new DecoratorLoop(new Sequence(
                new LeafAssert(() => Blackboard.StoryArcs.currArc == Blackboard.StoryArc.NO_ROOM)
            ))),
            new Sequence(
                OpenDoors(),
                new DecoratorLoop(new LeafWait(1))
            )
        );
    }

    protected Node SelectStory(int i)
    {
        return new SelectorParallel(
            new DecoratorInvert( new DecoratorLoop( new Sequence(
                new LeafAssert(() => Blackboard.StoryArcs.currArc == (Blackboard.StoryArc)Enum.Parse(typeof(Blackboard.StoryArc), "ROOM_" + i))
            ))),
            new Sequence(
                RoomBehavior(i),
                new DecoratorLoop(new LeafWait(1))
            )
        );
    }
    
    protected Node Story()
    {
        return new Sequence(
            new DecoratorLoop(
                new Sequence(
                    SelectNoStory(),
                    SelectStory(0),
                    SelectStory(1),
                    SelectStory(2)
                )
            )
        );
    }

    protected Node BuildTreeRoot()
	{
        return new Sequence(
            new SequenceParallel(
                UserInput(),
                MaintainArcs(),
                Story()
            )
        );
	}

    private struct Blackboard
    {
        public struct UserInput
        {
            public static int roomNumber = -1;
        }

        public struct StoryArcs
        {
            public static StoryArc currArc = StoryArc.NO_ROOM;
        }

        public enum StoryArc
        {
            NO_ROOM = -1,
            ROOM_0 = 0,
            ROOM_1 = 1,
            ROOM_2 = 2,
        }
    }

}

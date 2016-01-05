using System;
using System.Collections.Generic;
using System.Text;

namespace FineStateMachine
{
    /// <summary>
    /// Place the labels for the Transitions in this enum.
    /// Don't change the first label, NullTransition as FSMSystem class uses it.
    /// </summary>
    public enum Transition
    {
        NullTransition = 0, // Use this transition to represent a non-existing transition in your system
        SawPlayer = 1, // Use this transition to represent an NPC that see player
        LostPlayer = 2, // Use this transition to represent an NPC that follow is way
    }

    /// <summary>
    /// Place the labels for the States in this enum.
    /// Don't change the first label, NullTransition as FSMSystem class uses it.
    /// </summary>
    public enum StateID
    {
        NullStateID = 0, // Use this ID to represent a non-existing State in your system
        ChasingPlayer = 1, // Use this ID to represent an NPC that see player State in your system
        FollowingPath = 2, // Use this ID to represent an NPC that follow is way State in your system
    }

    public class NPCControl
    {
        public String player;
        public String bot;
        private FSMSystem fsm;

        public void SetTransition(Transition t) { fsm.PerformTransition(t); }

        public NPCControl(String bot)
        {
            this.bot = bot;
        }

        public void Start()
        {
            MakeFSM();
        }

        public void FixedUpdate()
        {
            fsm.CurrentState.Reason(player, bot);
            fsm.CurrentState.Act(player, bot);
        }

        // The NPC has two states: FollowPath and ChasePlayer
        // If it's on the first state and SawPlayer transition is fired, it changes to ChasePlayer
        // If it's on ChasePlayerState and LostPlayer transition is fired, it returns to FollowPath
        private void MakeFSM()
        {
            FollowPathState follow = new FollowPathState("path1");
            follow.AddTransition(Transition.SawPlayer, StateID.ChasingPlayer);

            ChasePlayerState chase = new ChasePlayerState();
            chase.AddTransition(Transition.LostPlayer, StateID.FollowingPath);

            fsm = new FSMSystem();
            fsm.AddState(follow);
            fsm.AddState(chase);
        }
    }

    public class FollowPathState : FSMState
    {
        String path;
        public FollowPathState(String path)
        {
            this.path = path;
            stateID = StateID.FollowingPath;
        }

        public override void Reason(String player, String npc)
        {
            Console.WriteLine("If the Player passes less than 15 meters away in front of the NPC");
        }

        public override void Act(String player, String npc)
        {
            Console.WriteLine("Follow the path of waypoints");
        }

        public override void DoBeforeEntering()
        {
            Console.WriteLine("Prepare following my way");
        }

        public override void DoBeforeLeaving()
        {
            Console.WriteLine("Prepare to stop following my way");
        }

    }

    public class ChasePlayerState : FSMState
    {
        public ChasePlayerState()
        {
            stateID = StateID.ChasingPlayer;
        }

        public override void Reason(String player, String npc)
        {
            Console.WriteLine("If the player has gone 30 meters away from the NPC, fire LostPlayer transition");
        }

        public override void Act(String player, String npc)
        {
            Console.WriteLine("Chase the current player");
        }

        public override void DoBeforeEntering()
        {
            Console.WriteLine("Prepare chasing player");
        }

        public override void DoBeforeLeaving()
        {
            Console.WriteLine("Prepare to stop chasing");
        }
    }
}
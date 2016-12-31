using static sk.fockomatej.karel.Karel;

namespace KarelTest
{
    class Program
    {
        private static int DELAY = 200;

        public static void TurnRight()
        {
            int PreviousDelay = StepDelay;
            StepDelay = 0;
            for (int i = 0; i < 2; i++) TurnLeft();
            StepDelay = PreviousDelay;
            TurnLeft();
        }

        public static void ClimbStairsAndPickBeepers()
        {
            while (FrontIsBlocked())
            {
                TurnLeft();
                while (RightIsBlocked())
                {
                    Move();
                }
                TurnRight();
                Move();
                while (BeepersPresent())
                {
                    PickBeeper();
                }
            }
        }
        
        static void Main(string[] args)
        {
            TurnOn("stairs3.kw");

            StepDelay = DELAY;
            Move();
            ClimbStairsAndPickBeepers();
            while (BeepersInBag())
            {
                PutBeeper();
            }

            TurnOff();
        }
    }
}

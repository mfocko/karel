using sk.fockomatej.karel;

namespace KarelTest
{
    class KarelReloaded : Karel
    {
        public KarelReloaded(string path) : base(path) {}

        public void TurnRight()
        {
            int PreviousDelay = this.StepDelay;
            StepDelay = 0;
            for (int i = 0; i < 2; i++) TurnLeft();
            StepDelay = PreviousDelay;
            TurnLeft();
        }

        public void ClimbStairsAndPickBeepers()
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
    }

    class Program
    {
        private static int DELAY = 200;
        
        static void Main(string[] args)
        {
            KarelReloaded k = new KarelReloaded("stairs3.kw");

            k.StepDelay = DELAY;
            k.Move();
            k.ClimbStairsAndPickBeepers();
            while (k.BeepersInBag())
            {
                k.PutBeeper();
            }

            k.TurnOff();
        }
    }
}

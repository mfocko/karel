using System;
using System.Threading;

namespace sk.fockomatej.karel
{
    enum BLOCK
    {
        CLEAR = 0,
        WALL = -1
    };

    enum DIRECTION
    {
        EAST = 0,
        NORTH = 90,
        WEST = 180,
        SOUTH = 270
    };

    struct WORLD
    {
        public int Width;
        public int Height;
        public int[,] Data;
        public const int MAX_WIDTH = 30;
        public const int MAX_HEIGHT = 30;
    }

    public static class Karel
    {
        private static int X, Y;
        private static DIRECTION Direction;
        private static int Steps;
        private static int Beepers;
        private static bool IsRunning;
        private static string LastCommand;
        private static WORLD World;
        private static int _StepDelay;
        private static readonly string[] Commands =
            { "MOVE", "TURNLEFT", "TURNON", "TURNOFF", "PUTBEEPER", "PICKBEEPER" };
        private static ConsoleColor DefaultBackground;

        public static void TurnOn(string path)
        {
            string[] Kw = null;
            try
            {
                Kw = System.IO.File.ReadAllLines(path);
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.Error.Write("Error: World file '{0}' not found.\n", path);
                Environment.Exit(1);
            }
            string[] Line = Kw[0].Split(' ');
            World.Width = int.Parse(Line[0]);
            World.Height = int.Parse(Line[1]);
            X = int.Parse(Line[2]);
            Y = int.Parse(Line[3]);
            string LocalDirection = Line[4].ToUpper();
            Beepers = int.Parse(Line[5]);

            World.Width = World.Width * 2 - 1;
            World.Height = World.Height * 2 - 1;

            X = X * 2 - 2;
            Y = Y * 2 - 2;

            if (World.Width > WORLD.MAX_WIDTH || World.Height > WORLD.MAX_HEIGHT)
            {
                Console.Error.Write("The given world is greater then the max values of [{0}x{1}].\n", WORLD.MAX_WIDTH, WORLD.MAX_HEIGHT);
                Environment.Exit(1);
            }

            switch (LocalDirection)
            {
                case "S":
                    Direction = DIRECTION.SOUTH;
                    break;
                case "W":
                    Direction = DIRECTION.WEST;
                    break;
                case "E":
                    Direction = DIRECTION.EAST;
                    break;
                case "N":
                    Direction = DIRECTION.NORTH;
                    break;
                default:
                    Console.Error.Write("Error: Unknown Karel's direction.\n");
                    Environment.Exit(1);
                    break;
            }

            World.Data = new int[World.Height, World.Width];

            for (int LineNr = 1; LineNr < Kw.Length; LineNr++)
            {
                if (Kw[LineNr] == "") continue;
                Line = Kw[LineNr].Split(' ');
                string Block = Line[0].ToUpper();

                switch (Block)
                {
                    case "W":
                        {
                            int Column = int.Parse(Line[1]) * 2 - 2;
                            int Row = int.Parse(Line[2]) * 2 - 2;
                            string Orientation = Line[3].ToUpper();
                            if (Column % 2 == 1 || Row % 2 == 1)
                            {
                                Console.Error.Write("Error: Wrong position.\n");
                                Environment.Exit(1);
                            }

                            switch (Orientation)
                            {
                                case "E":
                                    Column++;
                                    break;
                                case "W":
                                    Column--;
                                    break;
                                case "N":
                                    Row++;
                                    break;
                                case "S":
                                    Row--;
                                    break;
                                default:
                                    Console.Error.Write("Error: Unknown wall orientation '{0}' on line {1} in world file.\n", Orientation, LineNr + 1);
                                    Environment.Exit(1);
                                    break;
                            }

                            World.Data[Row, Column] = (int)BLOCK.WALL;

                            if (Column % 2 == 1 && Row % 2 == 0)
                            {
                                if (Row + 1 < World.Height) World.Data[Row + 1, Column] = (int)BLOCK.WALL;
                                if (Row - 1 >= 0) World.Data[Row - 1, Column] = (int)BLOCK.WALL;
                            }
                            else
                            {
                                if (Column + 1 < World.Width) World.Data[Row, Column + 1] = (int)BLOCK.WALL;
                                if (Column - 1 >= 0) World.Data[Row, Column - 1] = (int)BLOCK.WALL;
                            }
                            break;
                        }
                    case "B":
                        {
                            int Column = int.Parse(Line[1]) * 2 - 2;
                            int Row = int.Parse(Line[2]) * 2 - 2;
                            int Count = int.Parse(Line[3]);
                            World.Data[Row, Column] = Count;
                            break;
                        }
                    default:
                        Console.Error.Write("Unknown block character {0} on line {1} in world file.\n", Block, LineNr + 1);
                        Environment.Exit(1);
                        break;
                }
            }
            StepDelay = 1000;

            Init();
            LastCommand = Commands[2];
            DrawWorld();
            Render();

            IsRunning = true;
        }

        private static void PrintBeeper(int n)
        {
            ConsoleColor DefaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{0,-2}", n);
            Console.ForegroundColor = DefaultColor;
        }

        private static void DrawWorld()
        {
            Console.SetCursorPosition(0, 4);
            Console.Write("ST.+");
            for (int Column = 0, To = World.Width * 2; Column <= To; Column++)
            {
                Console.Write("-");
            }
            Console.Write("+\n");

            for (int Row = World.Height - 1; Row >= 0; Row--)
            {
                if (Row % 2 == 0) Console.Write("{0,2} |", Row / 2 + 1);
                else Console.Write("   |");

                if (World.Data[Row, 0] == (int)BLOCK.WALL) Console.Write("-");
                else Console.Write(" ");

                for (int Column = 0; Column < World.Width; Column++)
                {
                    int Block = World.Data[Row, Column];
                    if (Column % 2 == 0 && Row % 2 == 0)
                    {
                        if (Block > 0) PrintBeeper(Block);
                        else Console.Write(". ");
                        continue;
                    }

                    int Left = (Column - 1 >= 0) ? World.Data[Row, Column - 1] : (int)BLOCK.WALL;
                    int Right = (Column + 1 < World.Width) ? World.Data[Row, Column + 1] : (int)BLOCK.WALL;
                    int Up = (Row + 1 < World.Height) ? World.Data[Row + 1, Column] : 0;
                    int Down = (Row - 1 >= 0) ? World.Data[Row - 1, Column] : 0;
                    if (Block == (int)BLOCK.WALL)
                    {
                        if (Column % 2 == 1 && Row % 2 == 0)
                        {
                            Console.Write("| ");
                            continue;
                        }

                        bool WallAbove = (Up == (int)BLOCK.WALL);
                        bool WallBelow = (Down == (int)BLOCK.WALL);
                        bool WallOnLeft = (Left == (int)BLOCK.WALL);
                        bool WallOnRight = (Right == (int)BLOCK.WALL);

                        if (WallAbove && WallBelow && !WallOnLeft && !WallOnRight)
                        {
                            Console.Write("| ");
                            continue;
                        }

                        if (WallOnLeft && !WallOnRight && !WallAbove && !WallBelow)
                        {
                            Console.Write("- ");
                            continue;
                        }

                        if (!WallAbove && !WallBelow)
                        {
                            Console.Write("--");
                            continue;
                        }

                        if (!WallOnLeft && WallOnRight && !WallAbove && !WallBelow)
                        {
                            Console.Write(" -");
                            continue;
                        }

                        if (WallOnRight && ((WallAbove || WallBelow) || (WallAbove && WallOnLeft) || (WallAbove && WallBelow) || (WallOnLeft && WallBelow)))
                        {
                            Console.Write("+-");
                            continue;
                        }

                        if (!WallOnLeft && !WallOnRight && ((!WallAbove && WallBelow) || (!WallBelow && WallAbove)))
                        {
                            Console.Write("| ");
                            continue;
                        }

                        if (WallOnLeft && !WallOnRight && (WallAbove || WallBelow))
                        {
                            Console.Write("+ ");
                            continue;
                        }

                        Console.Write("  ");
                    }
                    else
                    {
                        Console.Write("  ");
                    }
                }

                Console.Write("|\n");
            }

            Console.Write("   +");
            for (int Column = 0, To = World.Width * 2; Column <= To; Column++)
            {
                Console.Write("-");
            }
            Console.Write("+\n     ");

            for (int Column = 0; Column < World.Width; Column++)
            {
                if (Column % 2 == 0) Console.Write("{0,-2}", Column / 2 + 1);
                else Console.Write("  ");
            }
            Console.Write("  AVE.");
        }

        private static void Render()
        {
            string DirectionOut;
            Console.SetCursorPosition(0, 1);

            switch (Direction)
            {
                case DIRECTION.NORTH:
                    DirectionOut = "NORTH";
                    break;
                case DIRECTION.SOUTH:
                    DirectionOut = "SOUTH";
                    break;
                case DIRECTION.WEST:
                    DirectionOut = "WEST";
                    break;
                case DIRECTION.EAST:
                    DirectionOut = "EAST";
                    break;
                default:
                    DirectionOut = "UNKNOWN";
                    break;
            }

            Console.Write(" {0,3} {1,-10}\n", Steps, LastCommand);
            Console.Write("  CORNER    FACING  BEEP-BAG  BEEP-CORNER\n");
            Console.Write(" ( {0,2},{1,2} )  {2,5}       {3,-3}        {4,-3}\n", (X + 2) / 2, (Y + 2) / 2, DirectionOut, Beepers, World.Data[Y, X]);

            Console.SetCursorPosition(2 * X + 5, World.Height - Y + 4);
            ConsoleColor DefaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            switch (Direction)
            {
                case DIRECTION.NORTH:
                    Console.Write("^ ");
                    break;
                case DIRECTION.SOUTH:
                    Console.Write("v ");
                    break;
                case DIRECTION.EAST:
                    Console.Write("> ");
                    break;
                case DIRECTION.WEST:
                    Console.Write("< ");
                    break;
            }
            Console.ForegroundColor = DefaultColor;
            Thread.Sleep(StepDelay);
        }

        private static void Update(int dx, int dy)
        {
            int Block = World.Data[Y - 2 * dy, X - 2 * dx];

            Console.SetCursorPosition(2 * (X - 2 * dx) + 5, World.Height - (Y - 2 * dy) + 4);
            if (Block > 0) PrintBeeper(Block);
            else Console.Write(". ");
        }

        private static void ErrorShutOff(string message)
        {
            Console.SetCursorPosition(0, 0);
            ConsoleColor DefaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error Shutoff! ({0})", message);
            Console.ReadKey();
            Console.ForegroundColor = DefaultColor;
            Console.BackgroundColor = DefaultBackground;
            Environment.Exit(1);
        }

        private static void Init()
        {
            DefaultBackground = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.CursorVisible = false;
            int Width = 2 * World.Width + 12;
            if (Width < 44) Width = 44;
            Console.SetWindowSize(Width, World.Height + 8);
            Console.SetBufferSize(Width, World.Height + 8);
        }

        private static void DeInit()
        {
            Console.SetCursorPosition(0, 0);
            ConsoleColor DefaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Press any key to quit...");
            Console.ReadKey();
            Console.ForegroundColor = DefaultColor;
            Console.BackgroundColor = DefaultBackground;
            Environment.Exit(0);
        }

        private static void CheckKarelState()
        {
            if (!IsRunning) ErrorShutOff("Karel is not turned on");
        }

        public static bool BeepersInBag()
        {
            CheckKarelState();
            return Beepers > 0;
        }

        public static bool NoBeepersInBag()
        {
            return !BeepersInBag();
        }

        public static bool FrontIsClear()
        {
            CheckKarelState();

            switch (Direction)
            {
                case DIRECTION.NORTH:
                    if (Y + 1 >= World.Height || World.Data[Y + 1, X] == (int)BLOCK.WALL) return false;
                    break;
                case DIRECTION.SOUTH:
                    if (Y - 1 < 1 || World.Data[Y - 1, X] == (int)BLOCK.WALL) return false;
                    break;
                case DIRECTION.WEST:
                    if (X - 1 < 1 || World.Data[Y, X - 1] == (int)BLOCK.WALL) return false;
                    break;
                case DIRECTION.EAST:
                    if (X + 1 >= World.Width || World.Data[Y, X + 1] == (int)BLOCK.WALL) return false;
                    break;
            }

            return true;
        }

        public static bool FrontIsBlocked()
        {
            return !FrontIsClear();
        }

        public static bool LeftIsClear()
        {
            CheckKarelState();

            DIRECTION OriginalDirection = Direction;
            Direction += 90;
            if (Direction > DIRECTION.SOUTH)
            {
                Direction = DIRECTION.EAST;
            }

            bool IsClear = FrontIsClear();
            Direction = OriginalDirection;

            return IsClear;
        }

        public static bool LeftIsBlocked()
        {
            return !LeftIsClear();
        }

        public static bool RightIsClear()
        {
            CheckKarelState();

            DIRECTION OriginalDirection = Direction;
            Direction -= 90;
            if (Direction < DIRECTION.EAST)
            {
                Direction = DIRECTION.SOUTH;
            }

            bool IsClear = FrontIsClear();
            Direction = OriginalDirection;

            return IsClear;
        }

        public static bool RightIsBlocked()
        {
            return !RightIsClear();
        }

        public static bool FacingNorth()
        {
            CheckKarelState();
            return Direction == DIRECTION.NORTH;
        }

        public static bool NotFacingNorth()
        {
            return !FacingNorth();
        }

        public static bool FacingSouth()
        {
            CheckKarelState();
            return Direction == DIRECTION.SOUTH;
        }

        public static bool NotFacingSouth()
        {
            return !FacingSouth();
        }

        public static bool FacingEast()
        {
            CheckKarelState();
            return Direction == DIRECTION.EAST;
        }

        public static bool NotFacingEast()
        {
            return !FacingEast();
        }

        public static bool FacingWest()
        {
            CheckKarelState();
            return Direction == DIRECTION.WEST;
        }

        public static bool NotFacingWest()
        {
            return !FacingWest();
        }

        public static bool BeepersPresent()
        {
            CheckKarelState();
            return World.Data[Y, X] > 0;
        }

        public static bool NoBeepersPresent()
        {
            return !BeepersPresent();
        }

        public static void Move()
        {
            CheckKarelState();

            if (FrontIsClear())
            {
                switch (Direction)
                {
                    case DIRECTION.NORTH:
                        Y += 2;
                        Update(0, 1);
                        break;
                    case DIRECTION.SOUTH:
                        Y -= 2;
                        Update(0, -1);
                        break;
                    case DIRECTION.WEST:
                        X -= 2;
                        Update(-1, 0);
                        break;
                    case DIRECTION.EAST:
                        X += 2;
                        Update(1, 0);
                        break;
                }
                Steps++;
                LastCommand = Commands[0];
                Render();
            }
            else
            {
                ErrorShutOff("Can't move this way");
            }
        }

        public static void TurnLeft()
        {
            CheckKarelState();

            Direction += 90;
            if ((int)Direction > 270)
            {
                Direction = DIRECTION.EAST;
            }
            Steps++;
            LastCommand = Commands[1];

            Render();
        }

        public static void TurnOff()
        {
            LastCommand = Commands[3];
            Render();
            DeInit();

            Environment.Exit(0);
        }

        public static void PutBeeper()
        {
            CheckKarelState();

            if (Beepers > 0)
            {
                World.Data[Y, X]++;
                Beepers--;
                Steps++;
                LastCommand = Commands[4];
                Render();
            }
            else
            {
                ErrorShutOff("Karel has no beeper to put at the corner");
            }
        }

        public static void PickBeeper()
        {
            CheckKarelState();

            if (World.Data[Y, X] > 0)
            {
                World.Data[Y, X]--;
                Beepers++;
                Steps++;
                LastCommand = Commands[5];
                Render();
            }
            else
            {
                ErrorShutOff("There is no beeper at the corner");
            }
        }

        public static int StepDelay
        {
            get { return _StepDelay; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _StepDelay = value;
            }
        }
    }
}

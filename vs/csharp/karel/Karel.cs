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

    public class Karel
    {
        private int X, Y;
        private DIRECTION Direction;
        private int Steps;
        private int Beepers;
        private bool IsRunning;
        private string LastCommand;
        private WORLD World;
        private int _StepDelay;
        private static readonly string[] Commands =
            { "MOVE", "TURNLEFT", "TURNON", "TURNOFF", "PUTBEEPER", "PICKBEEPER" };
        private ConsoleColor DefaultBackground;

        public Karel(string path)
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

        private void PrintBeeper(int n)
        {
            ConsoleColor DefaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{0,-2}", n);
            Console.ForegroundColor = DefaultColor;
        }

        private void DrawWorld()
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
                    int Left = (Column - 1 >= 0) ? World.Data[Row, Column - 1] : (int)BLOCK.WALL;
                    int Right = (Column + 1 < World.Width) ? World.Data[Row, Column + 1] : (int)BLOCK.WALL;
                    int Up = (Row + 1 < World.Height) ? World.Data[Row + 1, Column] : 0;
                    int Down = (Row - 1 >= 0) ? World.Data[Row - 1, Column] : 0;

                    if (Column % 2 == 0 && Row % 2 == 0)
                    {
                        if (Block > 0) PrintBeeper(Block);
                        else Console.Write(". ");
                        continue;
                    }
                    if (Block == (int)BLOCK.WALL)
                    {
                        if (Column % 2 == 1 && Row % 2 == 0)
                        {
                            Console.Write("| ");
                            continue;
                        }

                        if ((Up == (int)BLOCK.WALL && Down == (int)BLOCK.WALL && Left != (int)BLOCK.WALL && Right != (int)BLOCK.WALL))
                        {
                            Console.Write("| ");
                            continue;
                        }

                        if (Left == (int)BLOCK.WALL && Right != (int)BLOCK.WALL && Up != (int)BLOCK.WALL && Down != (int)BLOCK.WALL)
                        {
                            Console.Write("- ");
                            continue;
                        }

                        if (Up != (int)BLOCK.WALL && Down != (int)BLOCK.WALL)
                        {
                            Console.Write("--");
                            continue;
                        }

                        if (Left != (int)BLOCK.WALL && Right == (int)BLOCK.WALL && Up != (int)BLOCK.WALL && Down != (int)BLOCK.WALL)
                        {
                            Console.Write(" -");
                            continue;
                        }

                        if (Right == (int)BLOCK.WALL && ( (Up == (int)BLOCK.WALL || Down == (int)BLOCK.WALL)
                                                          || (Up == (int)BLOCK.WALL && Left == (int)BLOCK.WALL)
                                                          || (Up == (int)BLOCK.WALL && Down == (int)BLOCK.WALL)
                                                          || (Left == (int)BLOCK.WALL && Down == (int)BLOCK.WALL) ))
                        {
                            Console.Write("+-");
                            continue;
                        }

                        if (Left != (int)BLOCK.WALL && Right != (int)BLOCK.WALL &&
                            ((Up != (int)BLOCK.WALL && Down == (int)BLOCK.WALL) || (Down != (int)BLOCK.WALL && Up == (int)BLOCK.WALL)))
                        {
                            Console.Write("| ");
                            continue;
                        }

                        if (Left == (int)BLOCK.WALL && Right != (int)BLOCK.WALL && (Up == (int)BLOCK.WALL || Down == (int)BLOCK.WALL))
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

        private void Render()
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
            Console.Write(" CORNER  FACING  BEEP-BAG  BEEP-CORNER\n");
            Console.Write(" ({0}, {1})   {2,5}     {3,-2}        {4,-2}\n", (X + 2) / 2, (Y + 2) / 2, DirectionOut, Beepers, World.Data[Y, X]);

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

        private void Update(int dx, int dy)
        {
            if (!(dx == 0 && dy == 0))
            {
                int Block = World.Data[Y - 2 * dy, X - 2 * dx];

                Console.SetCursorPosition(2 * (X - 2 * dx) + 5, World.Height - (Y - 2 * dy) + 4);
                if (Block > 0) PrintBeeper(Block);
                else Console.Write(". ");
            }
        }

        private void ErrorShutOff(string message)
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

        private void Init()
        {
            DefaultBackground = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.CursorVisible = false;
            Console.SetWindowSize(2 * World.Width + 12, World.Height + 8);
            Console.SetBufferSize(2 * World.Width + 12, World.Height + 8);
        }

        private void DeInit()
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

        private void CheckKarelState()
        {
            if (!IsRunning) ErrorShutOff("Karel is not turned on");
        }

        public bool BeepersInBag()
        {
            CheckKarelState();
            return Beepers > 0;
        }

        public bool NoBeepersInBag()
        {
            return !BeepersInBag();
        }

        public bool FrontIsClear()
        {
            CheckKarelState();

            switch (this.Direction)
            {
                case DIRECTION.NORTH:
                    if (Y + 1 >= World.Height || World.Data[Y + 1, X] == (int) BLOCK.WALL) return false;
                    break;
                case DIRECTION.SOUTH:
                    if (Y - 1 < 1 || World.Data[Y - 1, X] == (int) BLOCK.WALL) return false;
                    break;
                case DIRECTION.WEST:
                    if (X - 1 < 1 || World.Data[Y, X - 1] == (int) BLOCK.WALL) return false;
                    break;
                case DIRECTION.EAST:
                    if (X + 1 >= World.Width || World.Data[Y, X + 1] == (int) BLOCK.WALL) return false;
                    break;
            }

            return true;
        }

        public bool FrontIsBlocked()
        {
            return !FrontIsClear();
        }

        public bool LeftIsClear()
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

        public bool LeftIsBlocked()
        {
            return !LeftIsClear();
        }

        public bool RightIsClear()
        {
            CheckKarelState();

            DIRECTION OriginalDirection = this.Direction;
            Direction -= 90;
            if (Direction < DIRECTION.EAST)
            {
                Direction = DIRECTION.SOUTH;
            }

            bool IsClear = FrontIsClear();
            Direction = OriginalDirection;

            return IsClear;
        }

        public bool RightIsBlocked()
        {
            return !RightIsClear();
        }

        public bool FacingNorth()
        {
            CheckKarelState();
            return Direction == DIRECTION.NORTH;
        }

        public bool NotFacingNorth()
        {
            return !FacingNorth();
        }

        public bool FacingSouth()
        {
            CheckKarelState();
            return Direction == DIRECTION.SOUTH;
        }

        public bool NotFacingSouth()
        {
            return !FacingSouth();
        }

        public bool FacingEast()
        {
            CheckKarelState();
            return Direction == DIRECTION.EAST;
        }

        public bool NotFacingEast()
        {
            return !FacingEast();
        }

        public bool FacingWest()
        {
            CheckKarelState();
            return Direction == DIRECTION.WEST;
        }

        public bool NotFacingWest()
        {
            return !FacingWest();
        }

        public bool BeepersPresent()
        {
            CheckKarelState();
            return World.Data[Y, X] > 0;
        }

        public bool NoBeepersPresent()
        {
            return !BeepersPresent();
        }

        public void Move()
        {
            CheckKarelState();

            if (FrontIsClear())
            {
                switch(Direction)
                {
                    case DIRECTION.NORTH:
                        Y += 2;
                        Update( 0,  1);
                        break;
                    case DIRECTION.SOUTH:
                        Y -= 2;
                        Update( 0, -1);
                        break;
                    case DIRECTION.WEST:
                        X -= 2;
                        Update(-1,  0);
                        break;
                    case DIRECTION.EAST:
                        X += 2;
                        Update( 1,  0);
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

        public void TurnLeft()
        {
            CheckKarelState();

            Direction += 90;
            if ((int) Direction > 270)
            {
                Direction = DIRECTION.EAST;
            }
            Steps++;
            LastCommand = Commands[1];

            Update(0, 0);
            Render();
        }

        public void TurnOff()
        {
            LastCommand = Commands[3];
            Render();
            DeInit();

            Environment.Exit(0);
        }

        public void PutBeeper()
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

        public void PickBeeper()
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

        public int StepDelay
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

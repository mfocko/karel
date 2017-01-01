Imports System.Threading

Public Module Karel
    Enum BLOCK
        CLEAR = 0
        WALL = -1
    End Enum

    Enum _DIRECTION
        EAST = 0
        NORTH = 90
        WEST = 180
        SOUTH = 270
    End Enum

    Structure _WORLD
        Public Width As Integer
        Public Height As Integer
        Public Data As Integer(,)
        Public Const MAX As Integer = 30
    End Structure

    Private X, Y As Integer
    Private Direction As _DIRECTION
    Private Steps As Integer
    Private Beepers As Integer
    Private IsRunning As Boolean
    Private LastCommand As String
    Private World As _WORLD
    Private _StepDelay As Integer
    Private ReadOnly Commands() As String = {"MOVE", "TURNLEFT", "TURNON", "TURNOFF", "PUTBEEPER", "PICKBEEPER"}
    Private DefaultBackground As ConsoleColor

    Public Sub TurnOn(ByVal path As String)
        Dim Kw() As String = Nothing
        Try
            Kw = IO.File.ReadAllLines(path)
        Catch ex As IO.FileNotFoundException
            Console.Error.Write("Error: World file '{0}' not found." + vbLf, path)
            Environment.Exit(1)
        End Try
        Dim Line() As String = Kw(0).Split(" ")
        World.Width = Integer.Parse(Line(0))
        World.Height = Integer.Parse(Line(1))
        X = Integer.Parse(Line(2))
        Y = Integer.Parse(Line(3))
        Dim LocalDirection As String = Line(4).ToUpper()
        Beepers = Integer.Parse(Line(5))

        World.Width = World.Width * 2 - 1
        World.Height = World.Height * 2 - 1

        X = X * 2 - 2
        Y = Y * 2 - 2

        If World.Width > _WORLD.MAX OrElse World.Height > _WORLD.MAX Then
            Console.Error.Write("The given world is greater then the max values of [{0}x{1}]." + vbLf, _WORLD.MAX, _WORLD.MAX)
            Environment.Exit(1)
        End If

        Select Case LocalDirection
            Case "S"
                Direction = _DIRECTION.SOUTH
            Case "W"
                Direction = _DIRECTION.WEST
            Case "E"
                Direction = _DIRECTION.EAST
            Case "N"
                Direction = _DIRECTION.NORTH
            Case Else
                Console.Error.Write("Error: Unknown Karel's direction." + vbLf)
                Environment.Exit(1)
        End Select

        ReDim World.Data(World.Height, World.Width)
        For LineNr As Integer = 1 To Kw.Length - 1
            If Kw(LineNr) = "" Then Continue For
            Line = Kw(LineNr).Split(" ")
            Dim ReadBlock As String = Line(0).ToUpper()

            Select Case ReadBlock
                Case "W"
                    Dim Column As Integer = Integer.Parse(Line(1)) * 2 - 2
                    Dim Row As Integer = Integer.Parse(Line(2)) * 2 - 2
                    Dim Orientation As String = Line(3).ToUpper()
                    If (Column Mod 2 = 1) OrElse (Row Mod 2 = 1) Then
                        Console.Error.Write("Error: Wrong position." + vbLf)
                        Environment.Exit(1)
                    End If
                    Select Case Orientation
                        Case "E"
                            Column += 1
                        Case "W"
                            Column -= 1
                        Case "N"
                            Row += 1
                        Case "S"
                            Row -= 1
                        Case Else
                            Console.Error.Write("Error: Unknown wall orientation '{0}' on line {1} in world file." + vbLf, Orientation, LineNr + 1)
                            Environment.Exit(1)
                    End Select
                    World.Data(Row, Column) = BLOCK.WALL
                    If (Column Mod 2 = 1) AndAlso (Row Mod 2 = 0) Then
                        If Row + 1 < World.Height Then World.Data(Row + 1, Column) = BLOCK.WALL
                        If Row - 1 >= 0 Then World.Data(Row - 1, Column) = BLOCK.WALL
                    Else
                        If Column + 1 < World.Width Then World.Data(Row, Column + 1) = BLOCK.WALL
                        If Column - 1 >= 0 Then World.Data(Row, Column - 1) = BLOCK.WALL
                    End If
                Case "B"
                    Dim Column As Integer = Integer.Parse(Line(1)) * 2 - 2
                    Dim Row As Integer = Integer.Parse(Line(2)) * 2 - 2
                    Dim Count As Integer = Integer.Parse(Line(3))
                    World.Data(Row, Column) = Count
                Case Else
                    Console.Error.Write("Unknown block character {0} on line {1} in world file." + vbLf, ReadBlock, LineNr + 1)
                    Environment.Exit(1)
            End Select
        Next
        StepDelay = 1000

        Init()
        LastCommand = Commands(2)
        DrawWorld()
        Render()

        IsRunning = True
    End Sub

    Private Sub PrintBeeper(ByVal n As Integer)
        Dim DefaultColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.White
        Console.Write("{0,-2}", n)
        Console.ForegroundColor = DefaultColor
    End Sub

    Private Sub DrawWorld()
        Console.SetCursorPosition(0, 4)
        Console.Write("ST.+")
        For Column As Integer = 0 To World.Width * 2
            Console.Write("-")
        Next
        Console.Write("+" + vbLf)

        For Row As Integer = World.Height - 1 To 0 Step -1
            If Row Mod 2 = 0 Then
                Console.Write("{0,2} |", Row / 2 + 1)
            Else
                Console.Write("   |")
            End If

            If World.Data(Row, 0) = BLOCK.WALL Then
                Console.Write("-")
            Else
                Console.Write(" ")
            End If

            For Column As Integer = 0 To World.Width - 1
                Dim ActualBlock As Integer = World.Data(Row, Column)
                If (Column Mod 2 = 0) AndAlso (Row Mod 2 = 0) Then
                    If ActualBlock > 0 Then
                        PrintBeeper(ActualBlock)
                    Else
                        Console.Write(". ")
                    End If
                    Continue For
                End If

                Dim Left As Integer = If(Column - 1 >= 0, World.Data(Row, Column - 1), BLOCK.WALL)
                Dim Right As Integer = If(Column + 1 < World.Width, World.Data(Row, Column + 1), BLOCK.WALL)
                Dim Up As Integer = If(Row + 1 < World.Height, World.Data(Row + 1, Column), 0)
                Dim Down As Integer = If(Row - 1 >= 0, World.Data(Row - 1, Column), 0)
                If ActualBlock = BLOCK.WALL Then
                    If (Column Mod 2 = 1) AndAlso (Row Mod 2 = 0) Then
                        Console.Write("| ")
                        Continue For
                    End If

                    Dim WallAbove As Boolean = (Up = BLOCK.WALL)
                    Dim WallBelow As Boolean = (Down = BLOCK.WALL)
                    Dim WallOnLeft As Boolean = (Left = BLOCK.WALL)
                    Dim WallOnRight As Boolean = (Right = BLOCK.WALL)

                    If (WallAbove) AndAlso (WallBelow) AndAlso (Not WallOnLeft) AndAlso (Not WallOnRight) Then
                        Console.Write("| ")
                        Continue For
                    End If

                    If (WallOnLeft) AndAlso (Not WallOnRight) AndAlso (Not WallAbove) AndAlso (Not WallBelow) Then
                        Console.Write("- ")
                        Continue For
                    End If

                    If (Not WallAbove) AndAlso (Not WallBelow) Then
                        Console.Write("--")
                        Continue For
                    End If

                    If (Not WallOnLeft) AndAlso (WallOnRight) AndAlso (Not WallAbove) AndAlso (Not WallBelow) Then
                        Console.Write(" -")
                        Continue For
                    End If

                    If (WallOnRight) AndAlso (((WallAbove) OrElse (WallBelow)) OrElse ((WallAbove) AndAlso (WallOnLeft)) OrElse ((WallAbove) AndAlso (WallBelow)) OrElse ((WallOnLeft) AndAlso (WallBelow))) Then
                        Console.Write("+-")
                        Continue For
                    End If

                    If (Not WallOnLeft) AndAlso (Not WallOnRight) AndAlso
                        (((Not WallAbove) AndAlso (WallBelow)) OrElse ((Not WallBelow) AndAlso (WallAbove))) Then
                        Console.Write("| ")
                        Continue For
                    End If

                    If (WallOnLeft) AndAlso (Not WallOnRight) AndAlso ((WallAbove) OrElse (WallBelow)) Then
                        Console.Write("+ ")
                        Continue For
                    End If

                    Console.Write("  ")
                Else
                    Console.Write("  ")
                End If
            Next
            Console.Write("|" + vbLf)
        Next

        Console.Write("   +")
        For Column As Integer = 0 To World.Width * 2
            Console.Write("-")
        Next
        Console.Write("+" + vbLf + "     ")

        For Column As Integer = 0 To World.Width - 1
            If Column Mod 2 = 0 Then
                Console.Write("{0,-2}", Column / 2 + 1)
            Else
                Console.Write("  ")
            End If
        Next
        Console.Write("  AVE.")
    End Sub

    Public Sub Render()
        Dim DirectionOut As String = Nothing
        Console.SetCursorPosition(0, 1)

        Select Case Direction
            Case _DIRECTION.NORTH
                DirectionOut = "NORTH"
            Case _DIRECTION.SOUTH
                DirectionOut = "SOUTH"
            Case _DIRECTION.WEST
                DirectionOut = "WEST"
            Case _DIRECTION.EAST
                DirectionOut = "EAST"
            Case Else
                DirectionOut = "UNKNOWN"
        End Select

        Console.Write(" {0,3} {1,-10}" + vbLf, Steps, LastCommand)
        Console.Write("  CORNER    FACING  BEEP-BAG  BEEP-CORNER" + vbLf)
        Console.Write(" ( {0,2},{1,2} )  {2,5}       {3,-3}        {4,-3}" + vbLf, (X + 2) / 2, (Y + 2) / 2, DirectionOut, Beepers, World.Data(Y, X))

        Console.SetCursorPosition(2 * X + 5, World.Height - Y + 4)
        Dim DefaultColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.Yellow
        Select Case Direction
            Case _DIRECTION.NORTH
                Console.Write("^ ")
            Case _DIRECTION.SOUTH
                Console.Write("v ")
            Case _DIRECTION.EAST
                Console.Write("> ")
            Case _DIRECTION.WEST
                Console.Write("< ")
        End Select
        Console.ForegroundColor = DefaultColor
        Thread.Sleep(StepDelay)
    End Sub

    Private Sub Update(ByVal dx, ByVal dy)
        Dim ActualBlock As Integer = World.Data(Y - 2 * dy, X - 2 * dx)

        Console.SetCursorPosition(2 * (X - 2 * dx) + 5, World.Height - (Y - 2 * dy) + 4)
        If ActualBlock > 0 Then
            PrintBeeper(ActualBlock)
        Else
            Console.Write(". ")
        End If
    End Sub

    Private Sub ErrorShutOff(ByVal message As String)
        Console.SetCursorPosition(0, 0)
        Dim DefaultColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.Red
        Console.Write("Error Shutoff! ({0})", message)
        Console.ReadKey()
        Console.ForegroundColor = DefaultColor
        Console.BackgroundColor = DefaultBackground
        Environment.Exit(1)
    End Sub

    Private Sub Init()
        DefaultBackground = Console.BackgroundColor
        Console.BackgroundColor = ConsoleColor.Black
        Console.CursorVisible = False
        Dim Width As Integer = 2 * World.Width + 12
        If Width < 44 Then Width = 44
        Console.SetWindowSize(Width, World.Height + 8)
        Console.SetBufferSize(Width, World.Height + 8)
    End Sub

    Private Sub DeInit()
        Console.SetCursorPosition(0, 0)
        Dim DefaultColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.Write("Press any key to quit...")
        Console.ReadKey()
        Console.ForegroundColor = DefaultColor
        Console.BackgroundColor = DefaultBackground
        Environment.Exit(0)
    End Sub

    Private Sub CheckKarelState()
        If Not IsRunning Then
            ErrorShutOff("Karel is not turned on")
        End If
    End Sub

    Public Function BeepersInBag() As Boolean
        CheckKarelState()
        Return Beepers > 0
    End Function

    Public Function NoBeepersInBag() As Boolean
        Return Not BeepersInBag()
    End Function

    Public Function FrontIsClear() As Boolean
        CheckKarelState()

        Select Case Direction
            Case _DIRECTION.NORTH
                If (Y + 1 >= World.Height) OrElse (World.Data(Y + 1, X) = BLOCK.WALL) Then Return False
            Case _DIRECTION.SOUTH
                If (Y - 1 < 1) OrElse (World.Data(Y - 1, X) = BLOCK.WALL) Then Return False
            Case _DIRECTION.WEST
                If (X - 1 < 1) OrElse (World.Data(Y, X - 1) = BLOCK.WALL) Then Return False
            Case _DIRECTION.EAST
                If (X + 1 >= World.Width) OrElse (World.Data(Y, X + 1) = BLOCK.WALL) Then Return False
        End Select
        Return True
    End Function

    Public Function FrontIsBlocked() As Boolean
        Return Not FrontIsClear()
    End Function

    Public Function LeftIsClear() As Boolean
        CheckKarelState()

        Dim OriginalDirection As _DIRECTION = Direction
        Direction += 90
        If Direction > _DIRECTION.SOUTH Then
            Direction = _DIRECTION.EAST
        End If

        Dim IsClear As Boolean = FrontIsClear()
        Direction = OriginalDirection

        Return IsClear
    End Function

    Public Function LeftIsBlocked() As Boolean
        Return Not LeftIsClear()
    End Function

    Public Function RightIsClear() As Boolean
        CheckKarelState()

        Dim OriginalDirection As _DIRECTION = Direction
        Direction -= 90
        If Direction < _DIRECTION.EAST Then
            Direction = _DIRECTION.SOUTH
        End If

        Dim IsClear As Boolean = FrontIsClear()
        Direction = OriginalDirection

        Return IsClear
    End Function

    Public Function RightIsBlocked() As Boolean
        Return Not RightIsClear()
    End Function

    Public Function FacingNorth() As Boolean
        CheckKarelState()
        Return Direction = _DIRECTION.NORTH
    End Function

    Public Function NotFacingNorth() As Boolean
        Return Not FacingNorth()
    End Function

    Public Function FacingSouth() As Boolean
        CheckKarelState()
        Return Direction = _DIRECTION.SOUTH
    End Function

    Public Function NotFacingSouth() As Boolean
        Return Not FacingSouth()
    End Function

    Public Function FacingEast() As Boolean
        CheckKarelState()
        Return Direction = _DIRECTION.EAST
    End Function

    Public Function NotFacingEast() As Boolean
        Return Not FacingEast()
    End Function

    Public Function FacingWest() As Boolean
        CheckKarelState()
        Return Direction = _DIRECTION.WEST
    End Function

    Public Function NotFacingWest() As Boolean
        Return Not FacingWest()
    End Function

    Public Function BeepersPresent() As Boolean
        CheckKarelState()
        Return World.Data(Y, X) > 0
    End Function

    Public Function NoBeepersPresent() As Boolean
        Return Not BeepersPresent()
    End Function

    Public Sub Move()
        CheckKarelState()

        If FrontIsClear() Then
            Select Case Direction
                Case _DIRECTION.NORTH
                    Y += 2
                    Update(0, 1)
                Case _DIRECTION.SOUTH
                    Y -= 2
                    Update(0, -1)
                Case _DIRECTION.WEST
                    X -= 2
                    Update(-1, 0)
                Case _DIRECTION.EAST
                    X += 2
                    Update(1, 0)
            End Select
            Steps += 1
            LastCommand = Commands(0)
            Render()
        Else
            ErrorShutOff("Can't move this way")
        End If
    End Sub

    Public Sub TurnLeft()
        CheckKarelState()

        Direction += 90
        If Direction > _DIRECTION.SOUTH Then
            Direction = _DIRECTION.EAST
        End If
        Steps += 1
        LastCommand = Commands(1)

        Render()
    End Sub

    Public Sub TurnOff()
        LastCommand = Commands(3)
        Render()
        DeInit()

        Environment.Exit(0)
    End Sub

    Public Sub PutBeeper()
        CheckKarelState()

        If Beepers > 0 Then
            World.Data(Y, X) += 1
            Beepers -= 1
            Steps += 1
            LastCommand = Commands(4)
            Render()
        Else
            ErrorShutOff("Karel has no beeper to put at the corner")
        End If
    End Sub

    Public Sub PickBeeper()
        CheckKarelState()

        If World.Data(Y, X) > 0 Then
            World.Data(Y, X) -= 1
            Beepers += 1
            Steps += 1
            LastCommand = Commands(5)
            Render()
        Else
            ErrorShutOff("There is no beeper at the corner")
        End If
    End Sub

    Public Property StepDelay As Integer
        Get
            Return _StepDelay
        End Get
        Set(value As Integer)
            If value < 0 Then Throw New ArgumentException()
            _StepDelay = value
        End Set
    End Property
End Module
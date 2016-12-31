Imports sk.fockomatej.karel.Karel
Module Module1

    Public Sub TurnRight()
        Dim PreviousDelay As Integer = StepDelay
        StepDelay = 0
        For i As Integer = 1 To 2
            TurnLeft()
        Next
        StepDelay = PreviousDelay
        TurnLeft()
    End Sub

    Public Sub ClimbStairsAndPickBeepers()
        While FrontIsBlocked()
            TurnLeft()
            While RightIsBlocked()
                Move()
            End While
            TurnRight()
            Move()
            While BeepersPresent()
                PickBeeper()
            End While
        End While
    End Sub

    Private Const DELAY As Integer = 200
    Sub Main()
        TurnOn("stairs3.kw")
        StepDelay = DELAY
        Move()
        ClimbStairsAndPickBeepers()
        While BeepersInBag()
            PutBeeper()
        End While
        TurnOff()
    End Sub

End Module

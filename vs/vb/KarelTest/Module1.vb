Imports sk.fockomatej.karel
Module Module1

    Class KarelReloaded : Inherits Karel
        Public Sub New(path As String)
            MyBase.New(path)
        End Sub

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
    End Class

    Private Const DELAY As Integer = 200
    Sub Main()
        Dim k As KarelReloaded = New KarelReloaded("stairs3.kw")
        k.StepDelay = DELAY
        k.Move()
        k.ClimbStairsAndPickBeepers()
        While k.BeepersInBag()
            k.PutBeeper()
        End While
        k.TurnOff()
    End Sub

End Module

from karel import *
import sys
delay = 0.2

def TurnRight():
    before = GetStepDelay()
    SetStepDelay(0)
    for i in range(2): TurnLeft()
    SetStepDelay(before)
    TurnLeft()

def ClimbStairsAndPickBeepers():
    while FrontIsBlocked():
        TurnLeft()
        while RightIsBlocked():
            Move()
        TurnRight()
        Move()
        while BeepersPresent():
            PickBeeper()

TurnOn("stairs3.kw")
SetStepDelay(delay)
Move()
ClimbStairsAndPickBeepers()
while BeepersInBag():
    PutBeeper()

TurnOff()

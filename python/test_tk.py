from karel_tk import *
delay = 250

def turnRight():
    before = getStepDelay()
    setStepDelay(0)
    for i in range(2):
        turnLeft()
    setStepDelay(before)
    turnLeft()

def climbStairsAndPickBeepers():
    while frontIsBlocked():
        turnLeft()
        while rightIsBlocked():
            movek()
        turnRight()
        movek()
        while beepersPresent():
            pickBeeper()

turnOn("stairs3.kw")
setStepDelay(delay)
movek()
climbStairsAndPickBeepers()
while beepersInBag():
    putBeeper()

turnOff()

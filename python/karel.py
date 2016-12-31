import os
import locale
from time import sleep
import sys
import locale
locale.setlocale(locale.LC_ALL, 'en_US.UTF-8')
from curses import *

class World:
    def __init__(self, width = 0, height = 0, data = []):
        self.width = width
        self.height = height
        self.data = data

class Karel:
    def __init__(self):
        self.x = 0
        self.y = 0
        self.direction = None
        self.steps = 0
        self.beepers = 0
        self.last_command = None
        self.step_delay = 1
        self.world = World()
        self.win = None
        self.summary_mode = False
        self.is_running = None

BLOCK = {"CLEAR": 0, "WALL": -1}
DIRECTION = {"EAST": 0, "NORTH": 90, "WEST": 180, "SOUTH": 270}
COLORS = {"RED": 1, "YELLOW": 2, "WHITE": 3}

__k = Karel()

def TurnOn(filename = None):
    world_content = None
    try:
        world_file = open(filename)
        world_content = world_file.read()
        world_file.close()
    except OSError:
        print("Error: World file '{}' not found.".format(filename))
        exit(1)
    world_content = world_content.split('\n')
    world_content[0] = world_content[0].split(' ')

    __k.world.width = int(world_content[0][0])
    __k.world.height = int(world_content[0][1])
    __k.x = int(world_content[0][2])
    __k.y = int(world_content[0][3])
    __k.direction = world_content[0][4].upper()
    __k.beepers = int(world_content[0][5])

    __k.world.data = []
    __k.world.width = __k.world.width * 2 - 1
    __k.world.height = __k.world.height * 2 - 1
    __k.x = __k.x * 2 - 2
    __k.y = __k.y * 2 - 2

    for i in range(__k.world.height):
        __k.world.data.append([])
        for j in range(__k.world.width):
            __k.world.data[i].append(0)

    if __k.world.width > 30 or __k.world.height > 30:
        print("The given world is greater than the max values of [{}x{}]".format(30, 30), file=sys.stderr)
        exit(1)

    if __k.direction == "S": __k.direction = DIRECTION["SOUTH"]
    elif __k.direction == "W": __k.direction = DIRECTION["WEST"]
    elif __k.direction == "E": __k.direction = DIRECTION["EAST"]
    elif __k.direction == "N": __k.direction = DIRECTION["NORTH"]
    else:
        print("Error: Unknown Karel's direction\n", file=sys.stderr)
        exit(1)

    for i in range(1, len(world_content)):
        if world_content[i] == '': continue
        world_content[i] = world_content[i].split(' ')
        block = world_content[i][0].upper()
        if block == "W":
            column = int(world_content[i][1]) * 2 - 2
            row = int(world_content[i][2]) * 2 - 2

            if column % 2 == 1 or row % 2 == 1:
                print("Error: Wrong position", file=sys.stderr)
                exit(1)

            orientation = world_content[i][3].upper()
            if orientation == "E": column += 1
            elif orientation == "W": column -= 1
            elif orientation == "N": row += 1
            elif orientation == "S": row -= 1
            else:
                print("Error: Unknown wall orientation '" + orientation + "' on line " + str(i + 1) + " in world file.", file=sys.stderr)
                exit(1)
            __k.world.data[row][column] = BLOCK["WALL"]
            if column % 2 == 1 and row % 2 == 0:
                if row + 1 < __k.world.height:
                    __k.world.data[row + 1][column] = BLOCK["WALL"]
                if row - 1 >= 0:
                    __k.world.data[row - 1][column] = BLOCK["WALL"]
            else:
                if column + 1 < __k.world.width:
                    __k.world.data[row][column + 1] = BLOCK["WALL"]
                if column - 1 >= 0:
                    __k.world.data[row][column - 1] = BLOCK["WALL"]
        elif block == "B":
            column = int(world_content[i][1]) * 2 - 2
            row = int(world_content[i][2]) * 2 - 2
            count = int(world_content[i][3])
            __k.world.data[row][column] = count
        else:
            print("Unknown block character " + block + " on line " + str(i + 1) + " in world file.")
            exit(1)
    __Init()
    __k.last_command = "TURNON"
    __DrawWorld()
    __Render()

    __k.is_running = True


def __PrintBeeper(n):
    if __k.summary_mode: return
    if has_colors():
        __k.win.attron(color_pair(COLORS["WHITE"]))
        __k.win.attron(A_BOLD)
    __k.win.addstr(u"{:^2}".format(n))

    if has_colors():
        __k.win.attroff(color_pair(COLORS["WHITE"]))
        __k.win.attroff(A_BOLD)

def __DrawWorld():
    if __k.summary_mode: return
    __k.win.move(4, 0)
    __k.win.addstr(u"ST.+")
    column = 0
    while column <= __k.world.width * 2:
        __k.win.addstr(u"-")
        column += 1
    __k.win.addstr(u"+\n")

    row = __k.world.height - 1
    while row >= 0:
        if row % 2 == 0:
            __k.win.addstr(u"{:2} |".format(row // 2 + 1))
        else:
            __k.win.addstr(u"   |")

        if __k.world.data[row][0] == BLOCK["WALL"]:
            __k.win.addstr(u"-")
        else:
            __k.win.addstr(u" ")

        for column in range(__k.world.width):
            block = __k.world.data[row][column]
            left = BLOCK["WALL"]
            right = BLOCK["WALL"]
            up = 0
            down = 0
            if column - 1 >= 0: left = __k.world.data[row][column - 1]
            if column + 1 < __k.world.width: right = __k.world.data[row][column + 1]
            if row + 1 < __k.world.height: up = __k.world.data[row + 1][column]
            if row - 1 >= 0: down = __k.world.data[row - 1][column]

            if column % 2 == 0 and row % 2 == 0:
                if block > 0:
                    __PrintBeeper(block)
                else:
                    __k.win.addstr(u'. ')
                column += 1
                continue

            if block == BLOCK["WALL"]:
                if column % 2 == 1 and row % 2 == 0:
                    __k.win.addstr(u"| ")
                    continue

                wall_above = (up == BLOCK["WALL"])
                wall_below = (down == BLOCK["WALL"])
                wall_on_left = (left == BLOCK["WALL"])
                wall_on_right = (right == BLOCK["WALL"])

                if wall_above and wall_below and (not wall_on_left) and (not wall_on_right):
                    __k.win.addstr(u"| ")
                    continue

                if wall_on_left and (not wall_on_right) and (not wall_above) and (not wall_below):
                    __k.win.addstr(u"- ")
                    continue

                if (not wall_above) and (not wall_below):
                    __k.win.addstr(u"--")
                    continue

                if (not wall_on_left) and wall_on_right and (not wall_above) and (not wall_below):
                    __k.win.addstr(u" -")
                    continue

                if wall_on_right and ( (wall_above or wall_below) or (wall_above and wall_on_left) or (wall_above and wall_below) or (wall_on_left and wall_below) ):
                    __k.win.addstr(u"+-")
                    continue

                if (not wall_on_left) and (not wall_on_right) and (((not wall_above) and wall_below) or ((not wall_below) and wall_above)):
                    __k.win.addstr(u"| ")
                    continue

                if wall_on_left and (not wall_on_right) and (wall_above or wall_below):
                    __k.win.addstr(u"+ ")
                    continue

                __k.win.addstr(u"  ")
            else:
                __k.win.addstr(u"  ")

        __k.win.addstr(u"|\n")
        row -= 1

    __k.win.addstr(u"   +")
    column = 0
    while column <= __k.world.width * 2:
        __k.win.addstr(u"-")
        column += 1
    __k.win.addstr(u"+\n     ")

    column = 0
    while column < __k.world.width:
        if column % 2 == 0:
            __k.win.addstr(u"{:>2}".format(column // 2 + 1))
        else:
            __k.win.addstr(u"  ")
        column += 1
    __k.win.addstr(u"  AVE.\n")
    __k.win.refresh()

def __Update(dx, dy):
    if not (dx == 0 and dy == 0):
        block = __k.world.data[__k.y - 2 * dy][__k.x - 2 * dx]

        if not __k.summary_mode:
            __k.win.move(__k.world.height - (__k.y - 2 * dy) + 4, 2 * (__k.x - 2 * dx) + 5)
            if block > 0:
                __PrintBeeper(block)
            else:
                __k.win.addstr(u". ")

def __Render():
    if __k.summary_mode: return
    direction = None
    __k.win.move(1, 0)

    if __k.direction == DIRECTION["NORTH"]: direction = "NORTH"
    elif __k.direction == DIRECTION["SOUTH"]: direction = "SOUTH"
    elif __k.direction == DIRECTION["WEST"]: direction = "WEST"
    elif __k.direction == DIRECTION["EAST"]: direction = "EAST"
    else: direction = "UNKNOWN"

    __k.win.addstr(u" {:>3} {}\n".format(__k.steps, __k.last_command))
    __k.win.addstr(u" CORNER  FACING  BEEP-BAG  BEEP-CORNER\n")
    __k.win.addstr(u" ({}, {})   {:>5}     {:2}        {:2}".format((__k.x + 2) // 2, (__k.y + 2) // 2, direction, __k.beepers, __k.world.data[__k.y][__k.x]))

    __k.win.move(__k.world.height - __k.y + 4, 2 * __k.x + 5)

    if has_colors():
        __k.win.attron(color_pair(COLORS["YELLOW"]))
        __k.win.attron(A_BOLD)

    if __k.direction == DIRECTION["NORTH"]: __k.win.addstr(u"^ ")
    elif __k.direction == DIRECTION["SOUTH"]: __k.win.addstr(u"v ")
    elif __k.direction == DIRECTION["EAST"]: __k.win.addstr(u"> ")
    elif __k.direction == DIRECTION["WEST"]: __k.win.addstr(u"< ")

    if has_colors():
        __k.win.attroff(color_pair(COLORS["YELLOW"]))
        __k.win.attroff(A_BOLD)
    __k.win.refresh()
    sleep(__k.step_delay)

def __ErrorShutOff(message):
    if not __k.summary_mode:
        __k.win.move(0, 0)
        if has_colors():
            __k.win.attron(color_pair(COLORS["RED"]))
        __k.win.addstr(u"Error Shutoff! ({})".format(message))
        __k.win.refresh()
        __k.win.getch()
        endwin()
    else:
        print("Error Shutoff! ({})".format(message), file=sys.stderr)
    exit(1)

def __Init():
    if __k.summary_mode: return

    __k.win = initscr()
    if has_colors():
        start_color()
        init_pair(COLORS["RED"], COLOR_RED, COLOR_BLACK)
        init_pair(COLORS["YELLOW"], COLOR_YELLOW, COLOR_BLACK)
        init_pair(COLORS["WHITE"], COLOR_WHITE, COLOR_BLACK)
    curs_set(0)

def __DeInit():
    if __k.summary_mode: return

    __k.win.move(0, 0)
    if has_colors():
        __k.win.attron(color_pair(COLORS["YELLOW"]))
    __k.win.addstr(u"Press any key to quit...")
    __k.win.refresh()
    __k.win.getch()
    endwin()


def __CheckKarelState():
    if not __k.is_running:
        __ErrorShutOff("Karel is not turned on")

def BeepersInBag():
    __CheckKarelState()
    return __k.beepers > 0

def NoBeepersInBag():
    return not BeepersInBag()

def FrontIsClear():
    __CheckKarelState()

    if __k.direction == DIRECTION["NORTH"]:
        if __k.y + 1 >= __k.world.height or __k.world.data[__k.y + 1][__k.x] == BLOCK["WALL"]:
            return False
    elif __k.direction == DIRECTION["SOUTH"]:
        if __k.y - 1 < 1 or __k.world.data[__k.y - 1][__k.x] == BLOCK["WALL"]:
            return False
    elif __k.direction == DIRECTION["WEST"]:
        if __k.x - 1 < 1 or __k.world.data[__k.y][__k.x - 1] == BLOCK["WALL"]:
            return False
    elif __k.direction == DIRECTION["EAST"]:
        if __k.x + 1 >= __k.world.width or __k.world.data[__k.y][__k.x + 1] == BLOCK["WALL"]:
            return False
    return True

def FrontIsBlocked():
    return not FrontIsClear()

def LeftIsClear():
    __CheckKarelState()

    original_direction = __k.direction
    __k.direction += 90
    if __k.direction > 270:
        __k.direction = DIRECTION["EAST"]

    is_clear = FrontIsClear()
    __k.direction = original_direction

    return is_clear

def LeftIsBlocked():
    return not LeftIsClear()

def RightIsClear():
    __CheckKarelState()

    original_direction = __k.direction
    __k.direction -= 90
    if __k.direction < 0:
        __k.direction = DIRECTION["SOUTH"]

    is_clear = FrontIsClear()
    __k.direction = original_direction

    return is_clear

def RightIsBlocked():
    return not RightIsClear()

def FacingNorth():
    __CheckKarelState()
    return __k.direction == DIRECTION["NORTH"]

def NotFacingNorth():
    return not FacingNorth()

def FacingSouth():
    __CheckKarelState()
    return __k.direction == DIRECTION["SOUTH"]

def NotFacingSouth():
    return not FacingSouth()

def FacingEast():
    __CheckKarelState()
    return __k.direction == DIRECTION["EAST"]

def NotFacingEast():
    return not FacingEast()

def FacingWest():
    __CheckKarelState()
    return __k.direction == DIRECTION["WEST"]

def NotFacingWest():
    return not FacingWest()

def BeepersPresent():
    __CheckKarelState()
    return __k.world.data[__k.y][__k.x] > 0

def NoBeepersPresent():
    return not BeepersPresent()

def Move():
    __CheckKarelState()

    if FrontIsClear():
        if __k.direction == DIRECTION["NORTH"]:
            __k.y += 2
            __Update(0, 1)
        elif __k.direction == DIRECTION["SOUTH"]:
            __k.y -= 2
            __Update(0, -1)
        elif __k.direction == DIRECTION["WEST"]:
            __k.x -= 2
            __Update(-1, 0)
        elif __k.direction == DIRECTION["EAST"]:
            __k.x += 2
            __Update(1, 0)
        __k.steps += 1
        __k.last_command = "MOVEK"
        __Render()
    else:
        __ErrorShutOff("Can't move this way")

def TurnLeft():
    __CheckKarelState()

    __k.direction += 90
    if __k.direction > 270:
        __k.direction = DIRECTION["EAST"]
    __k.steps += 1
    __k.last_command = "TURNLEFT"
    __Update(0, 0)
    __Render()

def TurnOff():
    __k.last_command = "TURNOFF"
    __Render()
    __DeInit()
    exit(0)

def PutBeeper():
    __CheckKarelState()

    if __k.beepers > 0:
        __k.world.data[__k.y][__k.x] += 1
        __k.beepers -= 1
        __k.steps += 1
        __k.last_command = "PUTBEEPER"
        __Render()
    else:
        __ErrorShutOff("Karel has no beeper to put at the corner")


def PickBeeper():
    __CheckKarelState()

    if __k.world.data[__k.y][__k.x] > 0:
        __k.world.data[__k.y][__k.x] -= 1
        __k.beepers += 1
        __k.steps += 1
        __k.last_command = "PICKBEEPER"
        __Render()
    else:
        __ErrorShutOff("There is no beeper at the corner")

def SetStepDelay(delay):
    __k.step_delay = delay

def GetStepDelay():
    return __k.step_delay

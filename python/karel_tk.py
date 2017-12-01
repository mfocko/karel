import sys
import tkinter as tk

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
        self.win = {}
        self.summary_mode = False
        self.is_running = None

BLOCK = {"CLEAR": 0, "WALL": -1}
DIRECTION = {"EAST": 0, "NORTH": 90, "WEST": 180, "SOUTH": 270}

__k = None

def turnOn(filename=None, scale=1):
    global __k
    __k = Karel()
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
    __init(scale)
    __k.last_command = "TURNON"
    __drawWorld()
    __render()

    __k.is_running = True


def __printBeeper(n, st, ave):
    if __k.summary_mode: return
    size = __k.win['size']
    __k.win['c'].create_text((ave + 1) * size + size // 2, ((__k.world.height + 1) // 2 - st) * size + size // 2, text=n, anchor='center', fill='white', font=__k.win['font'])

def __drawWorld():
    if __k.summary_mode: return

    c = __k.win['c']
    size = __k.win['size']
    m, n = (__k.world.height + 1) // 2, (__k.world.width + 1) // 2

    # NUMBERS
    c.create_text(size // 2, size, text='ST.', fill='white', anchor='s', font=__k.win['font'])
    for i in range(m):
        c.create_text(size - 8, (i + 2) * size - size // 2, text=(m - i), anchor='e', fill='white', font=__k.win['font'])
    y = ((m + 1) * size) + 8
    c.create_text((n + 1) * size + size // 2, y, text='AVE.', fill='white', anchor='n', font=__k.win['font'])
    for i in range(n):
        c.create_text((i + 2) * size - size // 2, y, text=(i + 1), anchor='n', fill='white', font=__k.win['font'])

    # BORDER
    c.create_rectangle(size, size, size * (n + 1), size * (m + 1), fill='', outline='white')

    for row in range(__k.world.height):
        for col in range(__k.world.width):
            block = __k.world.data[row][col]
            if row % 2 == 0 and col % 2 == 0:
                if block > 0:
                    __printBeeper(block, (row) // 2, (col) // 2)
                else:
                    __printBeeper('.', (row) // 2, (col) // 2)
            elif block == BLOCK['WALL']:
                size = __k.win['size']
                x = size + col // 2 * size
                y = (__k.world.width + 1) // 2 * size - row // 2 * size
                if row % 2 == 1 and col % 2 == 0:
                    __k.win['c'].create_line(x, y, x + size, y, fill='white')
                elif row % 2 == 0:
                    x += size
                    __k.win['c'].create_line(x, y, x, y + size, fill='white')

    __k.win['c'].update()

def __update(dx, dy):
    block = __k.world.data[__k.y - 2 * dy][__k.x - 2 * dx]

    if not __k.summary_mode:
        i, j = (__k.y - 2 * dy) // 2, (__k.x - 2 * dx) // 2
        size = __k.win['size']
        x, y = (j + 1) * size, ((__k.world.height + 1) // 2 - i) * size
        __k.win['c'].create_rectangle(x + 1, y + 1, x + size - 1, y + size - 1, fill='black')
        if block > 0:
            __printBeeper(block, i, j)
        else:
            __printBeeper(".", i, j)

def __render():
    if __k.summary_mode: return
    direction = None

    if __k.direction == DIRECTION["NORTH"]: direction = "NORTH"
    elif __k.direction == DIRECTION["SOUTH"]: direction = "SOUTH"
    elif __k.direction == DIRECTION["WEST"]: direction = "WEST"
    elif __k.direction == DIRECTION["EAST"]: direction = "EAST"
    else: direction = "UNKNOWN"

    __k.win['steps']['text'] = __k.steps
    __k.win['last_cmd']['text'] = __k.last_command
    __k.win['corner']['text'] = f"({(__k.x + 2) // 2}, {(__k.y + 2) // 2})"
    __k.win['facing']['text'] = direction
    __k.win['beep_bag']['text'] = __k.beepers
    __k.win['beep_corner']['text'] = __k.world.data[__k.y][__k.x]

    i, j = (__k.y + 2) // 2, (__k.x + 2) // 2
    size = __k.win['size']
    x, y = j * size, ((__k.world.height + 1) // 2 - i + 1) * size
    __k.win['c'].create_rectangle(x + 1, y + 1, x + size - 1, y + size - 1, fill='black')

    if __k.direction == DIRECTION["NORTH"]:
        __k.win['c'].create_text(x + size // 2, y + size // 2, text="^", font=__k.win['font'] + ' bold', fill='yellow', anchor='center')
    elif __k.direction == DIRECTION["SOUTH"]:
        __k.win['c'].create_text(x + size // 2, y + size // 2, text="v", font=__k.win['font'] + ' bold', fill='yellow', anchor='center')
    elif __k.direction == DIRECTION["EAST"]:
        __k.win['c'].create_text(x + size // 2, y + size // 2, text=">", font=__k.win['font'] + ' bold', fill='yellow', anchor='center')
    elif __k.direction == DIRECTION["WEST"]:
        __k.win['c'].create_text(x + size // 2, y + size // 2, text="<", font=__k.win['font'] + ' bold', fill='yellow', anchor='center')

    __k.win['c'].update()
    __k.win['c'].after(__k.step_delay)

def __errorShutOff(message):
    if not __k.summary_mode:
        __k.win['last_cmd']['foreground'] = 'red'
        __k.win['last_cmd']['foreground'] = f"Error Shutoff! ({message})"
    else:
        print(f"Error Shutoff! ({message})", file=sys.stderr)
    exit(1)

def __init(scale):
    if __k.summary_mode: return

    __k.win['size'] = int(32 * scale)

    __k.win['steps'] = tk.Label(text=__k.steps, anchor='e')
    __k.win['steps'].grid(row=0, column=0, sticky='e')

    __k.win['last_cmd'] = tk.Label(text=__k.last_command, anchor='w')
    __k.win['last_cmd'].grid(row=0, column=1, columnspan=3, sticky='w')

    tk.Label(text="CORNER").grid(row=1, column=0)
    tk.Label(text="FACING").grid(row=1, column=1)
    tk.Label(text="BEEP-BAG").grid(row=1, column=2)
    tk.Label(text="BEEP-CORNER").grid(row=1, column=3)

    __k.win['corner'] = tk.Label()
    __k.win['corner'].grid(row=2, column=0)
    __k.win['facing'] = tk.Label()
    __k.win['facing'].grid(row=2, column=1)
    __k.win['beep_bag'] = tk.Label()
    __k.win['beep_bag'].grid(row=2, column=2)
    __k.win['beep_corner'] = tk.Label()
    __k.win['beep_corner'].grid(row=2, column=3)
    
    __k.win['c'] = tk.Canvas(width=((__k.world.width + 1) // 2 + 2) * __k.win['size'],
                             height=((__k.world.height + 1) // 2 + 2) * __k.win['size'],
                             background='black')
    __k.win['c'].grid(column=0, row=3, columnspan=4)

    __k.win['font'] = f'monospace {int(12 * scale)}'

def __deinit():
    if __k.summary_mode: return

def __checkKarelState():
    if not __k.is_running:
        __errorShutOff("Karel is not turned on")

def beepersInBag():
    __checkKarelState()
    return __k.beepers > 0

def noBeepersInBag():
    return not beepersInBag()

def frontIsClear():
    __checkKarelState()

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

def frontIsBlocked():
    return not frontIsClear()

def leftIsClear():
    __checkKarelState()

    original_direction = __k.direction
    __k.direction += 90
    if __k.direction > 270:
        __k.direction = DIRECTION["EAST"]

    is_clear = frontIsClear()
    __k.direction = original_direction

    return is_clear

def leftIsBlocked():
    return not leftIsClear()

def rightIsClear():
    __checkKarelState()

    original_direction = __k.direction
    __k.direction -= 90
    if __k.direction < 0:
        __k.direction = DIRECTION["SOUTH"]

    is_clear = frontIsClear()
    __k.direction = original_direction

    return is_clear

def rightIsBlocked():
    return not rightIsClear()

def facingNorth():
    __checkKarelState()
    return __k.direction == DIRECTION["NORTH"]

def notFacingNorth():
    return not facingNorth()

def facingSouth():
    __checkKarelState()
    return __k.direction == DIRECTION["SOUTH"]

def notFacingSouth():
    return not facingSouth()

def facingEast():
    __checkKarelState()
    return __k.direction == DIRECTION["EAST"]

def notFacingEast():
    return not facingEast()

def facingWest():
    __checkKarelState()
    return __k.direction == DIRECTION["WEST"]

def notFacingWest():
    return not facingWest()

def beepersPresent():
    __checkKarelState()
    return __k.world.data[__k.y][__k.x] > 0

def noBeepersPresent():
    return not beepersPresent()

def movek():
    __checkKarelState()

    if frontIsClear():
        if __k.direction == DIRECTION["NORTH"]:
            __k.y += 2
            __update(0, 1)
        elif __k.direction == DIRECTION["SOUTH"]:
            __k.y -= 2
            __update(0, -1)
        elif __k.direction == DIRECTION["WEST"]:
            __k.x -= 2
            __update(-1, 0)
        elif __k.direction == DIRECTION["EAST"]:
            __k.x += 2
            __update(1, 0)
        __k.steps += 1
        __k.last_command = "MOVEK"
        __render()
    else:
        __errorShutOff("Can't move this way")

def turnLeft():
    __checkKarelState()

    __k.direction += 90
    if __k.direction > 270:
        __k.direction = DIRECTION["EAST"]
    __k.steps += 1
    __k.last_command = "TURNLEFT"
    __render()

def turnOff():
    __k.last_command = "TURNOFF"
    __k.is_running = False
    __render()
    __deinit()
    exit(0)

def putBeeper():
    __checkKarelState()

    if __k.beepers > 0:
        __k.world.data[__k.y][__k.x] += 1
        __k.beepers -= 1
        __k.steps += 1
        __k.last_command = "PUTBEEPER"
        __render()
    else:
        __errorShutOff("Karel has no beeper to put at the corner")


def pickBeeper():
    __checkKarelState()

    if __k.world.data[__k.y][__k.x] > 0:
        __k.world.data[__k.y][__k.x] -= 1
        __k.beepers += 1
        __k.steps += 1
        __k.last_command = "PICKBEEPER"
        __render()
    else:
        __errorShutOff("There is no beeper at the corner")

def setStepDelay(delay):
    __k.step_delay = delay

def getStepDelay():
    return __k.step_delay

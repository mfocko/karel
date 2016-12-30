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

BLOCK = {"CLEAR": 0, "WALL": -1}
DIRECTION = {"EAST": 0, "NORTH": 90, "WEST": 180, "SOUTH": 270}
COLORS = {"RED": 1, "YELLOW": 2, "WHITE": 3}
class Karel:
    def __init__(self, filename = None):
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

        self.__x = 0
        self.__y = 0
        self.__direction = DIRECTION["EAST"]
        self.__steps = 0
        self.__beepers = 0
        self.__last_command = "none"
        self.__step_delay = 1
        self.__world = World()
        self.__win = None
        self.__summary_mode = False

        self.__world.width = int(world_content[0][0])
        self.__world.height = int(world_content[0][1])
        self.__x = int(world_content[0][2])
        self.__y = int(world_content[0][3])
        self.__direction = world_content[0][4].upper()
        self.__beepers = int(world_content[0][5])

        self.__world.data = []
        self.__world.width = self.__world.width * 2 - 1
        self.__world.height = self.__world.height * 2 - 1
        self.__x = self.__x * 2 - 2
        self.__y = self.__y * 2 - 2

        for i in range(self.__world.height):
            self.__world.data.append([])
            for j in range(self.__world.width):
                self.__world.data[i].append(0)

        if self.__world.width > 30 or self.__world.height > 30:
            print("The given world is greater than the max values of [{}x{}]".format(30, 30), file=sys.stderr)
            exit(1)

        if self.__direction == "S": self.__direction = DIRECTION["SOUTH"]
        elif self.__direction == "W": self.__direction = DIRECTION["WEST"]
        elif self.__direction == "E": self.__direction = DIRECTION["EAST"]
        elif self.__direction == "N": self.__direction = DIRECTION["NORTH"]
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
                self.__world.data[row][column] = BLOCK["WALL"]
                if column % 2 == 1 and row % 2 == 0:
                    if row + 1 < self.__world.height:
                        self.__world.data[row + 1][column] = BLOCK["WALL"]
                    if row - 1 >= 0:
                        self.__world.data[row - 1][column] = BLOCK["WALL"]
                else:
                    if column + 1 < self.__world.width:
                        self.__world.data[row][column + 1] = BLOCK["WALL"]
                    if column - 1 >= 0:
                        self.__world.data[row][column - 1] = BLOCK["WALL"]
            elif block == "B":
                column = int(world_content[i][1]) * 2 - 2
                row = int(world_content[i][2]) * 2 - 2
                count = int(world_content[i][3])
                self.__world.data[row][column] = count
            else:
                print("Unknown block character " + block + " on line " + str(i + 1) + " in world file.")
                exit(1)
        self.__init()
        self.__last_command = "TURNON"
        self.__draw_world()
        self.__render()

        self.__is_running = True


    def __print_beeper(self, n):
        if self.__summary_mode: return
        if has_colors():
            self.__win.attron(color_pair(COLORS["WHITE"]))
            self.__win.attron(A_BOLD)
        self.__win.addstr(u"{:^2}".format(n))

        if has_colors():
            self.__win.attroff(color_pair(COLORS["WHITE"]))
            self.__win.attroff(A_BOLD)

    def __draw_world(self):
        if self.__summary_mode: return
        self.__win.move(4, 0)
        self.__win.addstr(u"ST.+")
        column = 0
        while column <= self.__world.width * 2:
            self.__win.addstr(u"-")
            column += 1
        self.__win.addstr(u"+\n")

        row = self.__world.height - 1
        while row >= 0:
            if row % 2 == 0:
                self.__win.addstr(u"{:2} |".format(row // 2 + 1))
            else:
                self.__win.addstr(u"   |")

            if self.__world.data[row][0] == BLOCK["WALL"]:
                self.__win.addstr(u"-")
            else:
                self.__win.addstr(u" ")

            for column in range(self.__world.width):
                block = self.__world.data[row][column]
                left = BLOCK["WALL"]
                right = BLOCK["WALL"]
                up = 0
                down = 0
                if column - 1 >= 0: left = self.__world.data[row][column - 1]
                if column + 1 < self.__world.width: right = self.__world.data[row][column + 1]
                if row + 1 < self.__world.height: up = self.__world.data[row + 1][column]
                if row - 1 >= 0: down = self.__world.data[row - 1][column]

                if column % 2 == 0 and row % 2 == 0:
                    if block > 0:
                        self.__print_beeper(block)
                    else:
                        self.__win.addstr(u'. ')
                    column += 1
                    continue

                if block == BLOCK["WALL"]:
                    if column % 2 == 1 and row % 2 == 0:
                        self.__win.addstr(u"| ")
                        continue

                    if up == BLOCK["WALL"] and down == BLOCK["WALL"] and left != BLOCK["WALL"] and right != BLOCK["WALL"]:
                        self.__win.addstr(u"| ")
                        continue

                    if left == BLOCK["WALL"] and right != BLOCK["WALL"] and up != BLOCK["WALL"] and down != BLOCK["WALL"]:
                        self.__win.addstr(u"- ")
                        continue

                    if up != BLOCK["WALL"] and down != BLOCK["WALL"]:
                        self.__win.addstr(u"--")
                        continue

                    if left != BLOCK["WALL"] and right == BLOCK["WALL"] and up != BLOCK["WALL"] and down != BLOCK["WALL"]:
                        self.__win.addstr(u" -")
                        continue

                    if right == BLOCK["WALL"] and ( (up == BLOCK["WALL"] or down == BLOCK["WALL"]) or (up == BLOCK["WALL"] and left == BLOCK["WALL"]) or (up == BLOCK["WALL"] and down ==BLOCK["WALL"]) or (left == BLOCK["WALL"] and down ==BLOCK["WALL"]) ):
                        self.__win.addstr(u"+-")
                        continue

                    if left != BLOCK["WALL"] and right != BLOCK["WALL"] and ((up != BLOCK["WALL"] and down == BLOCK["WALL"]) or (down != BLOCK["WALL"] and up == BLOCK["WALL"])):
                        self.__win.addstr(u"| ")
                        continue

                    if left == BLOCK["WALL"] and right != BLOCK["WALL"] and (up == BLOCK["WALL"] or down == BLOCK["WALL"]):
                        self.__win.addstr(u"+ ")
                        continue

                    self.__win.addstr(u"  ")
                else:
                    self.__win.addstr(u"  ")

            self.__win.addstr(u"|\n")
            row -= 1

        self.__win.addstr(u"   +")
        column = 0
        while column <= self.__world.width * 2:
            self.__win.addstr(u"-")
            column += 1
        self.__win.addstr(u"+\n     ")

        column = 0
        while column < self.__world.width:
            if column % 2 == 0:
                self.__win.addstr(u"{:>2}".format(column // 2 + 1))
            else:
                self.__win.addstr(u"  ")
            column += 1
        self.__win.addstr(u"  AVE.\n")
        self.__win.refresh()

    def __update(self, dx, dy):
        if not (dx == 0 and dy == 0):
            block = self.__world.data[self.__y - 2 * dy][self.__x - 2 * dx]

            if not self.__summary_mode:
                self.__win.move(self.__world.height - (self.__y - 2 * dy) + 4, 2 * (self.__x - 2 * dx) + 5)
                if block > 0:
                    self.__print_beeper(block)
                else:
                    self.__win.addstr(u". ")

    def __render(self):
        if self.__summary_mode: return
        direction = None
        self.__win.move(1, 0)

        if self.__direction == DIRECTION["NORTH"]: direction = "NORTH"
        elif self.__direction == DIRECTION["SOUTH"]: direction = "SOUTH"
        elif self.__direction == DIRECTION["WEST"]: direction = "WEST"
        elif self.__direction == DIRECTION["EAST"]: direction = "EAST"
        else: direction = "UNKNOWN"

        self.__win.addstr(u" {:>3} {}\n".format(self.__steps, self.__last_command))
        self.__win.addstr(u" CORNER  FACING  BEEP-BAG  BEEP-CORNER\n")
        self.__win.addstr(u" ({}, {})   {:>5}     {:2}        {:2}".format((self.__x + 2) // 2, (self.__y + 2) // 2, direction, self.__beepers, self.__world.data[self.__y][self.__x]))

        self.__win.move(self.__world.height - self.__y + 4, 2 * self.__x + 5)

        if has_colors():
            self.__win.attron(color_pair(COLORS["YELLOW"]))
            self.__win.attron(A_BOLD)

        if self.__direction == DIRECTION["NORTH"]: self.__win.addstr(u"^ ")
        elif self.__direction == DIRECTION["SOUTH"]: self.__win.addstr(u"v ")
        elif self.__direction == DIRECTION["EAST"]: self.__win.addstr(u"> ")
        elif self.__direction == DIRECTION["WEST"]: self.__win.addstr(u"< ")

        if has_colors():
            self.__win.attroff(color_pair(COLORS["YELLOW"]))
            self.__win.attroff(A_BOLD)
        self.__win.refresh()
        sleep(self.__step_delay)

    def __error_shut_off(self, message):
        if not self.__summary_mode:
            self.__win.move(0, 0)
            if has_colors():
                self.__win.attron(color_pair(COLORS["RED"]))
            self.__win.addstr(u"Error Shutoff! ({})".format(message))
            self.__win.refresh()
            self.__win.getch()
            endwin()
        else:
            print("Error Shutoff! ({})".format(message), file=sys.stderr)
        exit(1)

    def __init(self):
        if self.__summary_mode: return

        self.__win = initscr()
        if has_colors():
            start_color()
            init_pair(COLORS["RED"], COLOR_RED, COLOR_BLACK)
            init_pair(COLORS["YELLOW"], COLOR_YELLOW, COLOR_BLACK)
            init_pair(COLORS["WHITE"], COLOR_WHITE, COLOR_BLACK)
        curs_set(0)

    def __deinit(self):
        if self.__summary_mode: return

        self.__win.move(0, 0)
        if has_colors():
            self.__win.attron(color_pair(COLORS["YELLOW"]))
        self.__win.addstr(u"Press any key to quit...")
        self.__win.refresh()
        self.__win.getch()
        endwin()


    def __check_karel_state(self):
        if not self.__is_running:
            self.__error_shut_off("Karel is not turned on")

    def beepers_in_bag(self):
        self.__check_karel_state()
        return self.__beepers > 0

    def no_beepers_in_bag(self):
        return not self.beepers_in_bag()

    def front_is_clear(self):
        self.__check_karel_state()

        if self.__direction == DIRECTION["NORTH"]:
            if self.__y + 1 >= self.__world.height or self.__world.data[self.__y + 1][self.__x] == BLOCK["WALL"]:
                return False
        elif self.__direction == DIRECTION["SOUTH"]:
            if self.__y - 1 < 1 or self.__world.data[self.__y - 1][self.__x] == BLOCK["WALL"]:
                return False
        elif self.__direction == DIRECTION["WEST"]:
            if self.__x - 1 < 1 or self.__world.data[self.__y][self.__x - 1] == BLOCK["WALL"]:
                return False
        elif self.__direction == DIRECTION["EAST"]:
            if self.__x + 1 >= self.__world.width or self.__world.data[self.__y][self.__x + 1] == BLOCK["WALL"]:
                return False
        return True

    def front_is_blocked(self):
        return not self.front_is_clear()

    def left_is_clear(self):
        self.__check_karel_state()

        original_direction = self.__direction
        self.__direction += 90
        if self.__direction > 270:
            self.__direction = DIRECTION["EAST"]

        is_clear = self.front_is_clear()
        self.__direction = original_direction

        return is_clear

    def left_is_blocked(self):
        return not self.left_is_clear()

    def right_is_clear(self):
        self.__check_karel_state()

        original_direction = self.__direction
        self.__direction -= 90
        if self.__direction < 0:
            self.__direction = DIRECTION["SOUTH"]

        is_clear = self.front_is_clear()
        self.__direction = original_direction

        return is_clear

    def right_is_blocked(self):
        return not self.right_is_clear()

    def facing_north(self):
        self.__check_karel_state()
        return self.__direction == DIRECTION["NORTH"]

    def not_facing_north(self):
        return not self.facing_north()

    def facing_south(self):
        self.__check_karel_state()
        return self.__direction == DIRECTION["SOUTH"]

    def not_facing_south(self):
        return not self.facing_south()

    def facing_east(self):
        self.__check_karel_state()
        return self.__direction == DIRECTION["EAST"]

    def not_facing_east(self):
        return not self.facing_east()

    def facing_west(self):
        self.__check_karel_state()
        return self.__direction == DIRECTION["WEST"]

    def not_facing_west(self):
        return not self.facing_west()

    def beepers_present(self):
        self.__check_karel_state()
        return self.__world.data[self.__y][self.__x] > 0

    def no_beepers_present(self):
        return not self.beepers_present()

    def movek(self):
        self.__check_karel_state()

        if self.front_is_clear():
            if self.__direction == DIRECTION["NORTH"]:
                self.__y += 2
                self.__update(0, 1)
            elif self.__direction == DIRECTION["SOUTH"]:
                self.__y -= 2
                self.__update(0, -1)
            elif self.__direction == DIRECTION["WEST"]:
                self.__x -= 2
                self.__update(-1, 0)
            elif self.__direction == DIRECTION["EAST"]:
                self.__x += 2
                self.__update(1, 0)
            self.__steps += 1
            self.__last_command = "MOVEK"
            self.__render()
        else:
            self.__error_shut_off("Can't move this way")

    def turn_left(self):
        self.__check_karel_state()

        self.__direction += 90
        if self.__direction > 270:
            self.__direction = DIRECTION["EAST"]
        self.__steps += 1
        self.__last_command = "TURNLEFT"
        self.__update(0, 0)
        self.__render()

    def turn_off(self):
        self.__last_command = "TURNOFF"
        self.__render()
        self.__deinit()
        exit(0)

    def put_beeper(self):
        self.__check_karel_state()

        if self.__beepers > 0:
            self.__world.data[self.__y][self.__x] += 1
            self.__beepers -= 1
            self.__steps += 1
            self.__last_command = "PUTBEEPER"
            self.__render()
        else:
            self.__error_shut_off("Karel has no beeper to put at the corner")


    def pick_beeper(self):
        self.__check_karel_state()

        if self.__world.data[self.__y][self.__x] > 0:
            self.__world.data[self.__y][self.__x] -= 1
            self.__beepers += 1
            self.__steps += 1
            self.__last_command = "PICKBEEPER"
            self.__render()
        else:
            self.__error_shut_off("There is no beeper at the corner")

    def set_step_delay(self, delay):
        self.__step_delay = delay

    def get_step_delay(self):
        return self.__step_delay

from karel import Karel
import sys
delay = 0.2

class LepsiKarel(Karel):
    def turn_right(self):
        before = self.get_step_delay()
        self.set_step_delay(0)
        for i in range(2): self.turn_left()
        self.set_step_delay(before)
        self.turn_left()

    def climb_stairs_and_pick_beepers(self):
        while self.front_is_blocked():
            self.turn_left()
            while self.right_is_blocked():
                self.movek()
            self.turn_right()
            self.movek()
            while self.beepers_present():
                self.pick_beeper()

k = LepsiKarel("stairs3.kw")
k.set_step_delay(delay)
k.movek()
k.climb_stairs_and_pick_beepers()
while k.beepers_in_bag():
    k.put_beeper()

k.turn_off()

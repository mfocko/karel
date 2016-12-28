from karel import Karel
import sys
delay = 0.5

class LepsiKarel(Karel):
    def turn_right(self):
        before = self.get_step_delay()
        self.set_step_delay(0)
        for i in range(2): self.turn_left()
        self.set_step_delay(before)
        self.turn_left()

k = LepsiKarel("stairs3.kw")
k.set_step_delay(delay)
k.movek()
while k.front_is_blocked():
    k.turn_left()
    while k.right_is_blocked(): k.movek()
    k.turn_right()
    k.movek()
    while k.beepers_present():
        k.pick_beeper()
while k.beepers_in_bag():
    k.put_beeper()

k.turn_off()

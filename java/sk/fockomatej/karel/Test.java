package sk.fockomatej.karel;

public class Test extends Karel {
	public static void main(String[] args) {
		launch(args);
	}

	public void turnRight() {
		int previousDelay = stepDelay();
		stepDelay(0);
		for (int i = 0; i < 2; i++) turnLeft();
		stepDelay(previousDelay);
		turnLeft();
	}

	public void climbStairsAndPickBeepers() {
		while (frontIsBlocked()) {
			turnLeft();
			while (rightIsBlocked()) {
				move();
			}
			turnRight();
			move();
			while (beepersPresent()) {
				pickBeeper();
			}
		}
	}

	@Override
	public void run() {
		turnOn("stairs3.kw");
		
		stepDelay(200);

		move();
		climbStairsAndPickBeepers();
		while (beepersInBag()) putBeeper();

		turnOff();
	}
}
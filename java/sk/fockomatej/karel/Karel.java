package sk.fockomatej.karel;

import java.util.Scanner;
import java.io.FileNotFoundException;
import java.io.File;
import javafx.application.Application;
import javafx.application.Platform;
import javafx.stage.Stage;

public abstract class Karel extends Application {

	private enum Block {
		CLEAR(0), WALL(-1);
		private int value;
		Block(int value) { this.value = value; }
		public int toInt() { return value; }
	}

	private enum Direction {
		EAST(0), NORTH(90), WEST(180), SOUTH(270);
		private int value;
		Direction(int value) { this.value = value; }
		public int toInt() { return value; }
	}

	private class World {
		public int width, height;
		public int[][] data;
		public static final int MAX = 30;
	}

	private int x, y;
	private int direction;
	private int steps;
	private int beepers;
	private boolean isRunning;
	private String lastCommand;
	private World world;
	private int _stepDelay;
	private Terminal term;
	private final String[] commands =
		{ "MOVE", "TURNLEFT", "TURNON", "TURNOFF", "PUTBEEPER", "PICKBEEPER" };

	public void turnOn(String path) {
		Scanner file = null;
		try {
			file = new Scanner(new File(path));
		} catch (FileNotFoundException ex) {
			System.err.format("Error: World file '%s' not found.\n", path);
			System.exit(1);
		}
		String[] line = file.nextLine().split(" ");
		world = new World();
		world.width = Integer.parseInt(line[0]);
		world.height = Integer.parseInt(line[1]);
		x = Integer.parseInt(line[2]);
		y = Integer.parseInt(line[3]);
		String localDirection = line[4].toUpperCase();
		beepers = Integer.parseInt(line[5]);

		world.width = world.width * 2 - 1;
		world.height = world.height * 2 - 1;

		x = x * 2 - 2;
		y = y * 2 - 2;

		if (world.width > World.MAX || world.height > World.MAX) {
			System.err.format("The given world is greater then the max values of [%dx%d].\n", World.MAX, World.MAX);
			System.exit(1);
		}

		switch (localDirection) {
			case "S":
				direction = Direction.SOUTH.toInt();
				break;
			case "W":
				direction = Direction.WEST.toInt();
				break;
			case "E":
				direction = Direction.EAST.toInt();
				break;
			case "N":
				direction = Direction.NORTH.toInt();
				break;
			default:
				System.err.println("Error: Unknown Karel's direction.");
				System.exit(1);
				break;
		}

		world.data = new int[world.height][world.width];

		while (file.hasNextLine()) {
			String nextLine = file.nextLine();
			if (nextLine.equals("")) continue;
			line = nextLine.split(" ");
			String block = line[0].toUpperCase();

			int row, column;

			switch (block) {
				case "W":
					column = Integer.parseInt(line[1]) * 2 - 2;
					row = Integer.parseInt(line[2]) * 2 - 2;
					String orientation = line[3].toUpperCase();
					if (column % 2 == 1 || row % 2 == 1) {
						System.err.println("Error: Wrong position.");
						System.exit(1);
					}

					switch (orientation) {
						case "E":
							column++;
							break;
						case "W":
							column--;
							break;
						case "N":
							row++;
							break;
						case "S":
							row--;
							break;
						default:
							System.err.format("Error: Unknown wall orientation '%s' in world file.\n", orientation);
							System.exit(1);
							break;
					}

					world.data[row][column] = Block.WALL.toInt();

					if (column % 2 == 1 && row % 2 == 0) {
						if (row + 1 < world.height) world.data[row + 1][column] = Block.WALL.toInt();
						if (row - 1 >= 0) world.data[row - 1][column] = Block.WALL.toInt();
					} else {
						if (column + 1 < world.width) world.data[row][column + 1] = Block.WALL.toInt();
						if (column - 1 >= 0) world.data[row][column - 1] = Block.WALL.toInt();
					}
					break;
				case "B":
					column = Integer.parseInt(line[1]) * 2 - 2;
					row = Integer.parseInt(line[2]) * 2 - 2;
					int count = Integer.parseInt(line[3]);
					world.data[row][column] = count;
					break;
				default:
					System.err.format("Unknown block character %s in world file.\n", block);
					System.exit(1);
					break;
			}
		}

		_stepDelay = 500;

		lastCommand = commands[2];
		drawWorld();
		render();

		isRunning = true;

		file.close();
	}

	private void printBeeper(int n) {
		term.setForeground(Terminal.Foreground.WHITE);
		term.write(String.format("%-2d", n));
		term.setForeground(Terminal.Foreground.DEFAULT);
	}

	private void drawWorld() {
		term.move(0, 4);
		term.write("ST.+");
		for (int column = 0, to = world.width * 2; column <= to; column++) {
			term.write("-");
		}
		term.write("+\n");

		for (int row = world.height - 1; row >= 0; row--) {
			if (row % 2 == 0) term.write(String.format("%2d |", row / 2 + 1));
			else term.write("   |");

			if (world.data[row][0] == Block.WALL.toInt()) term.write("-");
			else term.write(" ");

			for (int column = 0; column < world.width; column++) {
				int block = world.data[row][column];
				if (column % 2 == 0 && row % 2 == 0) {
					if (block > 0) printBeeper(block);
					else term.write(". ");
					continue;
				}

				int left = (column - 1 >= 0) ? world.data[row][column - 1] : Block.WALL.toInt();
				int right = (column + 1 < world.width) ? world.data[row][column + 1] : Block.WALL.toInt();
				int up = (row + 1 < world.height) ? world.data[row + 1][column] : 0;
				int down = (row - 1 >= 0) ? world.data[row - 1][column] : 0;
				if (block == Block.WALL.toInt()) {
					if (column % 2 == 1 && row % 2 == 0) {
						term.write("| ");
						continue;
					}

					boolean wallAbove = (up == Block.WALL.toInt());
					boolean wallBelow = (down == Block.WALL.toInt());
					boolean wallOnLeft = (left == Block.WALL.toInt());
					boolean wallOnRight = (right == Block.WALL.toInt());
					if (wallAbove && wallBelow && !wallOnLeft && !wallOnRight) {
						term.write("| ");
						continue;
					}

					if (wallOnLeft && !wallOnRight && !wallAbove && !wallBelow) {
						term.write("- ");
						continue;
					}

					if (!wallAbove && !wallBelow) {
						term.write("--");
						continue;
					}

					if (!wallOnLeft && wallOnRight && !wallAbove && !wallBelow) {
						term.write(" -");
						continue;
					}

					if (wallOnRight && ((wallAbove || wallBelow) || (wallAbove && wallOnLeft) || (wallAbove && wallBelow) || (wallOnLeft && wallBelow))) {
						term.write("+-");
						continue;
					}

					if (!wallOnLeft && !wallOnRight && ((!wallAbove && wallBelow) || (!wallBelow && wallAbove))) {
						term.write("| ");
						continue;
					}

					if (wallOnLeft && !wallOnRight && (wallAbove || wallBelow)) {
						term.write("+ ");
						continue;
					}

					term.write("  ");
				} else {
					term.write("  ");
				}
			}
			term.write("|\n");
		}

		term.write("   +");
		for (int column = 0, to = world.width * 2; column <= to; column++) {
			term.write("-");
		}
		term.write("+\n     ");

		for (int column = 0; column < world.width; column++) {
			if (column % 2 == 0) term.write(String.format("%-2d", column / 2 + 1));
			else term.write("  ");
		}
		term.write("  AVE.");
	}

	private void render() {
		String directionOut;
		term.move(0, 1);

		switch (direction) {
			case 90: // NORTH
				directionOut = "NORTH";
				break;
			case 270: // SOUTH
				directionOut = "SOUTH";
				break;
			case 180: // WEST
				directionOut = "WEST";
				break;
			case 0: // EAST
				directionOut = "EAST";
				break;
			default:
				directionOut = "UNKNOWN";
				break;
		}

		term.write(String.format(" %3d %-10s\n", steps, lastCommand));
		term.write("  CORNER    FACING  BEEP-BAG  BEEP-CORNER\n");
		term.write(String.format(" ( %2d,%2d )  %5s       %-3d        %-3d\n", (x + 2) / 2, (y + 2) / 2, directionOut, beepers, world.data[y][x]));

		term.move(2 * x + 5, world.height - y + 4);
		term.setForeground(Terminal.Foreground.YELLOW);
		switch (direction) {
			case 90: // NORTH
				term.write("^ ");
				break;
			case 270: // SOUTH
				term.write("v ");
				break;
			case 0: // EAST
				term.write("> ");
				break;
			case 180: // WEST
				term.write("< ");
				break;
		}

		term.setForeground(Terminal.Foreground.DEFAULT);
		try {
			Thread.sleep(_stepDelay);
		} catch (InterruptedException ex) {
			System.err.println(ex);
		}
	}

	private void update(int dx, int dy) {
		int block = world.data[y - 2 * dy][x - 2 * dx];

		term.move(2 * (x - 2 * dx) + 5, world.height - (y - 2 * dy) + 4);
		if (block > 0) printBeeper(block);
		else term.write(". ");
	}

	private void errorShutOff(String message) {
		term.move(0, 0);
		term.setForeground(Terminal.Foreground.RED);
		term.write(String.format("Error Shutoff! (%s)", message));
		isRunning = false;
		term.close();
	}

	private void deInit() {
		term.move(0, 0);
		term.setForeground(Terminal.Foreground.YELLOW);
		term.write("Close window to quit...");
		isRunning = false;
		term.close();
	}

	private void checkKarelState() {
		if (!isRunning) errorShutOff("Karel is not turned on");
	}

	public boolean beepersInBag() {
		checkKarelState();
		return beepers > 0;
	}

	public boolean noBeepersInBag() {
		return !beepersInBag();
	}

	public boolean frontIsClear() {
		checkKarelState();

		switch (direction) {
			case 90: // NORTH
				if (y + 1 >= world.height || world.data[y + 1][x] == Block.WALL.toInt()) return false;
				break;
			case 270: // SOUTH
				if (y - 1 < 1 || world.data[y - 1][x] == Block.WALL.toInt()) return false;
				break;
			case 180: // WEST
				if (x - 1 < 1 || world.data[y][x - 1] == Block.WALL.toInt()) return false;
				break;
			case 0: // EAST
				if (x + 1 >= world.width || world.data[y][x + 1] == Block.WALL.toInt()) return false;
				break;
		}
		return true;
	}

	public boolean frontIsBlocked() {
		return !frontIsClear();
	}

	public boolean leftIsClear() {
		checkKarelState();

		int originalDirection = direction;
		direction += 90;
		if (direction > Direction.SOUTH.toInt()) {
			direction = Direction.EAST.toInt();
		}

		boolean isClear = frontIsClear();
		direction = originalDirection;

		return isClear;
	}

	public boolean leftIsBlocked() {
		return !leftIsClear();
	}

	public boolean rightIsClear() {
		checkKarelState();

		int originalDirection = direction;
		direction -= 90;
		if (direction < Direction.EAST.toInt()) {
			direction = Direction.SOUTH.toInt();
		}

		boolean isClear = frontIsClear();
		direction = originalDirection;

		return isClear;
	}

	public boolean rightIsBlocked() {
		return !rightIsClear();
	}

	public boolean facingNorth() {
		checkKarelState();
		return direction == Direction.NORTH.toInt();
	}

	public boolean notFacingNorth() {
		return !facingNorth();
	}

	public boolean facingSouth() {
		checkKarelState();
		return direction == Direction.SOUTH.toInt();
	}

	public boolean notFacingSouth() {
		return !facingSouth();
	}

	public boolean facingEast() {
		checkKarelState();
		return direction == Direction.EAST.toInt();
	}

	public boolean notFacingEast() {
		return !facingEast();
	}

	public boolean facingWest() {
		checkKarelState();
		return direction == Direction.WEST.toInt();
	}

	public boolean notFacingWest() {
		return !facingWest();
	}

	public boolean beepersPresent() {
		checkKarelState();
		return world.data[y][x] > 0;
	}

	public boolean noBeepersPresent() {
		return !beepersPresent();
	}

	public void move() {
		checkKarelState();

		if (frontIsClear()) {
			switch (direction) {
				case 90: // NORTH
					y += 2;
					update(0, 1);
					break;
				case 270: // SOUTH
					y -= 2;
					update(0, -1);
					break;
				case 180: // WEST
					x -= 2;
					update(-1, 0);
					break;
				case 0: // EAST
					x += 2;
					update(1, 0);
					break;
			}
			steps++;
			lastCommand = commands[0];
			render();
		} else {
			errorShutOff("Can't move this way");
		}
	}

	public void turnLeft() {
		checkKarelState();

		direction += 90;
		if (direction > Direction.SOUTH.toInt()) {
			direction = Direction.EAST.toInt();
		}
		steps++;
		lastCommand = commands[1];

		render();
	}

	public void turnOff() {
		lastCommand = commands[3];
		render();
		deInit();
	}

	public void putBeeper() {
		checkKarelState();

		if (beepers > 0) {
			world.data[y][x]++;
			beepers--;
			steps++;
			lastCommand = commands[4];
			render();
		} else {
			errorShutOff("Karel has no beeper to put at the corner");
		}
	}

	public void pickBeeper() {
		checkKarelState();

		if (world.data[y][x] > 0) {
			world.data[y][x]--;
			beepers++;
			steps++;
			lastCommand = commands[5];
			render();
		} else {
			errorShutOff("There is no beeper at the corner");
		}
	}

	public void stepDelay(int value) {
		if (value < 0) throw new IllegalArgumentException();
		_stepDelay = value;
	}

	public int stepDelay() {
		return _stepDelay;
	}

	public void close() {
		if (!isRunning) {
			Platform.exit();
		}
	}

	public static void main(String[] args) {
		launch(args);
	}

	@Override
	public void start(Stage stage) {
		stage.setResizable(false);
		term = new Terminal(this);
		new Thread( () -> { term.start(stage); } ).run();
		new Thread( () -> { run(); } ).start();
	}

	public abstract void run();
}
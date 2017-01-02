package sk.fockomatej.karel;

import javafx.application.Application;
import javafx.stage.Stage;
import javafx.scene.Scene;
import javafx.scene.canvas.Canvas;
import javafx.scene.canvas.GraphicsContext;
import javafx.scene.layout.StackPane;
import javafx.scene.paint.Color;
import javafx.scene.text.Font;
import javafx.scene.text.Text;
import javafx.event.ActionEvent;

public class Terminal extends Application {
	public enum Foreground {
		DEFAULT, WHITE, YELLOW, RED
	}

	private Canvas canvas;
	private GraphicsContext gr;
	private double height, width;
	private double x, y;
	private int row, column;
	private double fontHeight, fontWidth;
	private static final String fontFamily = "monospace";
	private Karel k;
	private boolean running;

	public Terminal(Karel obj) {
		super();
		k = obj;
		fontHeight = 16;
		Text text = new Text(" ");
		text.setFont(Font.font(fontFamily, fontHeight));
		fontWidth = text.getBoundsInLocal().getWidth();
		width = 70 * fontWidth;
		height = 37 * fontHeight;
		x = 0;
		y = fontHeight;
		row = column = 0;
		running = true;
	}

	@Override
	public void start(Stage stage) {
		canvas = new Canvas(width, height);
		gr = canvas.getGraphicsContext2D();
		initialize();
		StackPane pane = new StackPane(canvas);
		Scene scene = new Scene(pane, width, height);
		scene.setOnMouseClicked( (event) -> { k.close(); } );
		scene.setOnKeyPressed( (event) -> { k.close(); } );
		stage.setTitle("Karel");
		stage.setScene(scene);
		stage.show();
	}

	private void initialize() {
		gr.setFill(Color.BLACK);
		gr.setStroke(Color.GAINSBORO);
		gr.setFont(Font.font(fontFamily, fontHeight));
		gr.fillRect(0, 0, width, height);
	}

	public void setForeground(Foreground color) {
		if (!running) return;
		switch (color) {
			case DEFAULT:
				gr.setStroke(Color.GAINSBORO);
				break;
			case WHITE:
				gr.setStroke(Color.WHITE);
				break;
			case YELLOW:
				gr.setStroke(Color.YELLOW);
				break;
			case RED:
				gr.setStroke(Color.RED);
				break;
		}
	}

	public void write(String text) {
		if (!running) return;
		if (text.length() == 0) return;
		int newLine = text.indexOf('\n');
		if (newLine == -1) {
			gr.fillRect(x, y - fontHeight + 3, text.length() * fontWidth, fontHeight);
			gr.strokeText(text, x, y);
			x += fontWidth * text.length();
			column += text.length();
		} else if (newLine == text.length() - 1) {
			String substring = text.substring(0, newLine);
			gr.fillRect(x, y - fontHeight + 3, substring.length() * fontWidth, fontHeight);
			gr.strokeText(substring, x, y);
			x = column = 0;
			y += fontHeight;
			row++;
		} else if (newLine == 0) {
			x = column = 0;
			y += fontHeight;
			row++;
			String substring = text.substring(1, text.length());
			gr.fillRect(x, y - fontHeight + 3, substring.length() * fontWidth, fontHeight);
			gr.strokeText(substring, x, y);
		} else {
			String substring = text.substring(0, newLine);
			gr.fillRect(x, y - fontHeight + 3, substring.length() * fontWidth, fontHeight);
			gr.strokeText(substring, x, y);
			x = column = 0;
			y += fontHeight;
			row++;
			write(text.substring(newLine + 1, text.length()));
		}
	}

	public void move(int column, int row) {
		if (!running) return;
		this.column = column;
		this.row = row;
		y = fontHeight * (row + 1);
		x = fontWidth * column;
	}

	public void close() {
		running = false;
	}
}
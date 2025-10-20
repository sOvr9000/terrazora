
using TMPro;

public class ConsoleMessage {
	public readonly string text;
	public readonly int tick;

	public TextMeshProUGUI tmp { get; private set; }
	public int displayLife { get; private set; }

	public ConsoleMessage(TextMeshProUGUI tmp, string text) {
		this.tmp = tmp;
		this.text = text;

		displayLife = Constants.CONSOLE_MESSAGE_DISPLAY_LIFE;
	}

	public void Update() {
		if (displayLife > 0) {
			displayLife--;
		}
	}

}

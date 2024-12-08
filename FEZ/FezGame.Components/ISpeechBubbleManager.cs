using Microsoft.Xna.Framework;

namespace FezGame.Components;

public interface ISpeechBubbleManager
{
	Vector3 Origin { set; }

	SpeechFont Font { set; }

	bool Hidden { get; }

	void ChangeText(string toText);

	void Hide();

	void ForceDrawOrder(int drawOrder);

	void RevertDrawOrder();
}

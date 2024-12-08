using System;
using System.Globalization;
using FezEngine.Tools;
using FezGame.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Structure;

internal interface MenuItem
{
	string Text { get; set; }

	Func<string> SuffixText { get; set; }

	MenuLevel Parent { get; set; }

	bool Hovered { get; set; }

	Action Selected { get; set; }

	Vector2 Size { get; set; }

	bool Selectable { get; set; }

	bool IsSlider { get; set; }

	bool UpperCase { get; set; }

	bool Centered { get; set; }

	bool Disabled { get; set; }

	bool InError { get; set; }

	bool IsGamerCard { get; set; }

	bool Hidden { get; set; }

	TimeSpan SinceHovered { get; set; }

	Rectangle HoverArea { get; set; }

	bool LocalizeSliderValue { get; set; }

	string LocalizationTagFormat { get; set; }

	float ActivityRatio { get; }

	void OnSelected();

	void ClampTimer();

	void Slide(int direction);
}
internal class MenuItem<T> : MenuItem
{
	private static readonly TimeSpan HoverGrowDuration = TimeSpan.FromSeconds(0.1);

	private string text;

	public Func<T> SliderValueGetter;

	public Action<T, int> SliderValueSetter;

	public string Text
	{
		get
		{
			string text = ((this.text == null) ? "" : StaticText.GetString(this.text)) + ((SuffixText == null) ? "" : SuffixText());
			if (!UpperCase)
			{
				return text;
			}
			return text.ToUpper(CultureInfo.InvariantCulture);
		}
		set
		{
			text = value;
		}
	}

	public Func<string> SuffixText { get; set; }

	public MenuLevel Parent { get; set; }

	public bool Hovered { get; set; }

	public bool UpperCase { get; set; }

	public Action Selected { get; set; }

	public Vector2 Size { get; set; }

	public bool Selectable { get; set; }

	public bool IsSlider { get; set; }

	public bool LocalizeSliderValue { get; set; }

	public string LocalizationTagFormat { get; set; }

	public bool Centered { get; set; }

	public bool Disabled { get; set; }

	public bool IsGamerCard { get; set; }

	public TimeSpan SinceHovered { get; set; }

	public bool Hidden { get; set; }

	public Rectangle HoverArea { get; set; }

	public bool InError { get; set; }

	public float ActivityRatio => FezMath.Saturate((float)SinceHovered.Ticks / (float)HoverGrowDuration.Ticks);

	public MenuItem()
	{
		Selectable = true;
		Centered = true;
	}

	public void Slide(int direction)
	{
		if (IsSlider)
		{
			SliderValueSetter(SliderValueGetter(), Math.Sign(direction));
		}
	}

	public void ClampTimer()
	{
		if (SinceHovered.Ticks < 0)
		{
			SinceHovered = TimeSpan.Zero;
		}
		if (SinceHovered > HoverGrowDuration)
		{
			SinceHovered = HoverGrowDuration;
		}
	}

	public void OnSelected()
	{
		Selected();
	}

	public override string ToString()
	{
		string text = Text;
		if (IsSlider)
		{
			text = string.Format(Text, LocalizeSliderValue ? StaticText.GetString(string.Format(LocalizationTagFormat, SliderValueGetter())) : ((object)SliderValueGetter()));
		}
		if (UpperCase)
		{
			text = text.ToUpperInvariant();
		}
		return text;
	}
}

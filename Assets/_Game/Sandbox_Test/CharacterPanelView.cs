using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TTH.Combat.Attributes;

public class CharacterPanelView : UIView
{
	[SerializeField] private PlayerRuntime playerRuntime;
	[SerializeField] private Slider hpBar;
	[SerializeField] private Slider mpBar;
	[SerializeField] private TMP_Text hpText;
	[SerializeField] private TMP_Text mpText;
	[SerializeField] private TMP_Text atkText;
	[SerializeField] private TMP_Text defText;
	[SerializeField] private TMP_Text dexText;
	[SerializeField] private TMP_Text spdText;
	[SerializeField] private TMP_Text vitText;
	[SerializeField] private TMP_Text wisText;

	private bool subscribed;

	private void Awake()
	{
		if (playerRuntime == null)
			playerRuntime = FindFirstObjectByType<PlayerRuntime>();
	}

	public override void OnShown()
	{
		Subscribe();
		Refresh();
	}

	public override void OnHide()
	{
		Unsubscribe();
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	private void Subscribe()
	{
		if (subscribed || playerRuntime == null || playerRuntime.Resources == null)
			return;

		playerRuntime.Resources.OnHPChanged += HandleResourceChanged;
		playerRuntime.Resources.OnMPChanged += HandleResourceChanged;
		if (playerRuntime.Attributes != null)
			playerRuntime.Attributes.OnDirty += HandleAttributeDirty;
		subscribed = true;
	}

	private void Unsubscribe()
	{
		if (!subscribed || playerRuntime == null)
			return;

		if (playerRuntime.Resources != null)
		{
			playerRuntime.Resources.OnHPChanged -= HandleResourceChanged;
			playerRuntime.Resources.OnMPChanged -= HandleResourceChanged;
		}

		if (playerRuntime.Attributes != null)
			playerRuntime.Attributes.OnDirty -= HandleAttributeDirty;
		subscribed = false;
	}

	private void HandleResourceChanged(float before, float after)
	{
		Refresh();
	}

	private void HandleAttributeDirty(AttributeId attribute)
	{
		Refresh();
	}

	private void Refresh()
	{
		if (playerRuntime == null || playerRuntime.Attributes == null || playerRuntime.Resources == null)
			return;

		var attributes = playerRuntime.Attributes;
		var resources = playerRuntime.Resources;
		var maxHP = Mathf.Max(0f, attributes.Get(AttributeId.HP));
		var maxMP = Mathf.Max(0f, attributes.Get(AttributeId.MP));

		SetBar(hpBar, resources.CurrentHP, maxHP);
		SetBar(mpBar, resources.CurrentMP, maxMP);
		SetText(hpText, $"HP {resources.CurrentHP:0}/{maxHP:0}");
		SetText(mpText, $"MP {resources.CurrentMP:0}/{maxMP:0}");
		SetText(atkText, $"ATK {attributes.Get(AttributeId.ATK):0}");
		SetText(defText, $"DEF {attributes.Get(AttributeId.DEF):0}");
		SetText(dexText, $"DEX {attributes.Get(AttributeId.DEX):0}");
		SetText(spdText, $"SPD {attributes.Get(AttributeId.SPD):0}");
		SetText(vitText, $"VIT {attributes.Get(AttributeId.VIT):0}");
		SetText(wisText, $"WIS {attributes.Get(AttributeId.WIS):0}");
	}

	private static void SetBar(Slider bar, float current, float max)
	{
		if (bar == null)
			return;

		bar.minValue = 0f;
		bar.maxValue = Mathf.Max(1f, max);
		bar.SetValueWithoutNotify(Mathf.Clamp(current, 0f, bar.maxValue));
	}

	private static void SetText(TMP_Text text, string value)
	{
		if (text != null)
			text.text = value;
	}

}

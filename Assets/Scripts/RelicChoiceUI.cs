using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>UI controller for the roguelite relic choice panel.</summary>
public class RelicChoiceUI : MonoBehaviour
{
    [System.Serializable]
    public class RelicCard
    {
        public Button button;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public TMP_Text stackText;
        public Image icon;
    }

    public GameObject panelRoot;
    public TMP_Text titleText;
    public RelicCard[] cards = new RelicCard[3];

    private readonly List<RelicData> currentChoices = new List<RelicData>();

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        WireButtons();
    }

    private void WireButtons()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null || cards[i].button == null) continue;
            int captured = i;
            cards[i].button.onClick.RemoveAllListeners();
            cards[i].button.onClick.AddListener(() => Choose(captured));
        }
    }

    public void Show(List<RelicData> choices)
    {
        currentChoices.Clear();
        if (choices != null) currentChoices.AddRange(choices);

        WireButtons();
        if (titleText != null) titleText.text = "CHOOSE A RELIC";

        for (int i = 0; i < cards.Length; i++)
        {
            RelicCard card = cards[i];
            if (card == null) continue;
            bool active = i < currentChoices.Count && currentChoices[i] != null;
            if (card.button != null) card.button.gameObject.SetActive(active);
            if (!active) continue;

            RelicData relic = currentChoices[i];
            if (card.nameText != null) card.nameText.text = relic.relicName;
            if (card.descriptionText != null) card.descriptionText.text = relic.description;
            if (card.stackText != null)
            {
                int nextStack = (RelicManager.Instance != null ? RelicManager.Instance.GetStacks(relic) : 0) + 1;
                card.stackText.text = relic.maxStacks > 1 ? $"STACK {nextStack}/{relic.maxStacks}" : "UNIQUE";
            }
            if (card.icon != null)
            {
                card.icon.sprite = relic.icon;
                card.icon.enabled = relic.icon != null;
            }
        }

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        currentChoices.Clear();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Choose(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;
        RelicManager.Instance?.ChooseRelic(currentChoices[index]);
    }
}

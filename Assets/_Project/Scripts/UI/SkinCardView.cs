using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
public class SkinCardView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text buttonLabel;

    public Button Button => button;
    public KnifeSkin Skin { get; private set; }

    public void Bind(KnifeSkin skin, bool owned, bool equipped, bool canBuy, string lockedLabel)
    {
        Skin = skin;
        icon.sprite = skin.Sprite;
        icon.color = owned ? skin.Tint : skin.Tint * new Color(0.35f, 0.35f, 0.35f, 1f);
        nameText.text = skin.DisplayName;

        if (equipped)
        {
            buttonLabel.text = "EQUIPPED";
            button.interactable = false;
        }
        else if (owned)
        {
            buttonLabel.text = "EQUIP";
            button.interactable = true;
        }
        else if (skin.Price > 0)
        {
            buttonLabel.text = $"{skin.Price} COINS";
            button.interactable = canBuy;
        }
        else
        {
            buttonLabel.text = lockedLabel;
            button.interactable = false;
        }
    }
}

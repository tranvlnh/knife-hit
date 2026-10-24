using TMPro;
using UnityEngine;

// ReSharper disable once CheckNamespace
// Mua / trang bị skin; mọi quy tắc nằm ở PlayerProgress, màn này chỉ hiển thị và gọi hành động.
public class KnifeInventoryScreen : MonoBehaviour
{
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private TMP_Text coinsText;
    [Tooltip("Số thẻ bằng số skin tối đa")]
    [SerializeField] private SkinCardView[] cards = { };

    private void Awake()
    {
        if (!progress || !coinsText || cards.Length == 0)
        {
            Debug.LogError("Assign PlayerProgress, the coins text and the skin cards.", this);
            enabled = false;
            return;
        }

        foreach (var card in cards)
        {
            var captured = card;
            card.Button.onClick.AddListener(() => OnCardClicked(captured.Skin));
        }
    }

    public void Refresh()
    {
        coinsText.text = $"COINS {progress.Coins}";
        var skins = progress.Campaign.Skins;
        for (var i = 0; i < cards.Length; i++)
        {
            cards[i].gameObject.SetActive(i < skins.Count);
            if (i >= skins.Count)
            {
                continue;
            }

            var skin = skins[i];
            var unlockLevel = progress.Campaign.FindUnlockLevel(skin);
            var lockedLabel = unlockLevel ? unlockLevel.IsBoss ? "BOSS REWARD" : $"CLEAR {unlockLevel.DisplayName}" : "LOCKED";
            cards[i].Bind(skin, progress.IsOwned(skin), progress.EquippedSkin == skin, progress.CanBuy(skin), lockedLabel);
        }
    }

    private void OnCardClicked(KnifeSkin skin)
    {
        if (!skin)
        {
            return;
        }

        if (progress.IsOwned(skin) || progress.TryBuy(skin))
        {
            progress.Equip(skin);
        }
    }
}

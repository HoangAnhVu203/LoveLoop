using UnityEngine;
using UnityEngine.UI;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance { get; private set; }

    Text moneyText;
    public long currentMoney = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void BindMoneyText(Text txt)
    {
        moneyText = txt;
        UpdateUI();
    }

    public void AddMoney(long amount)
    {
        currentMoney += amount;
        if (currentMoney < 0) currentMoney = 0;
        UpdateUI();
    }

    public bool TrySpend(long amount)
    {
        if (amount <= 0) return true;
        if (currentMoney < amount) return false;

        currentMoney -= amount;
        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "$ " + MoneyFormatter.Format(currentMoney);
    }

    public void SetMoney(long value)
    {
        currentMoney = value;
        if (currentMoney < 0) currentMoney = 0;
        ForceRefreshUI();
    }

    public void ForceRefreshUI()
    {

        if (moneyText != null)
            moneyText.text = "$ " + MoneyFormatter.Format(currentMoney);
    }

}

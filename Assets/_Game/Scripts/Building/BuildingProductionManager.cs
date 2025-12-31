using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingProductionManager : MonoBehaviour
{
    public static BuildingProductionManager Instance { get; private set; }

    readonly List<BuildingOnRoad> _active = new();

    void Awake() => Instance = this;

    public void SetActiveBuildings(List<BuildingOnRoad> buildings, bool resetTimerWhenActivate)
    {
        // tắt list cũ
        for (int i = 0; i < _active.Count; i++)
            if (_active[i] != null) _active[i].SetActiveOnRoad(false);

        _active.Clear();

        if (buildings == null) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b == null || b.data == null) continue;

            b.SetActiveOnRoad(true);

            // “chỉ chạy khi đang ở road” + claim bằng nút:
            // vào road thì set mốc bắt đầu, không trả bù road trước
            if (resetTimerWhenActivate || b.lastClaimUnix <= 0)
                b.lastClaimUnix = now;

            _active.Add(b);
        }
    }

    // UI gọi: bấm nút Collect => nhận tất cả quà đủ chu kỳ
    public bool TryClaimAllOnCurrentRoad(out ClaimResult result)
    {
        result = default;
        if (_active.Count == 0) return false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        int addMoney = 0, addRose = 0, addHeart = 0;
        float addBoostSeconds = 0f;

        bool claimedAny = false;

        for (int i = 0; i < _active.Count; i++)
        {
            var b = _active[i];
            if (b == null || !b.IsActive || b.data == null) continue;

            int interval = Mathf.Max(1, Mathf.RoundToInt(b.data.intervalSeconds));
            long last = b.lastClaimUnix;
            if (last <= 0) { b.lastClaimUnix = now; continue; }

            long delta = now - last;
            if (delta < interval) continue;

            long cycles = delta / interval;
            if (cycles <= 0) continue;

            int perCycle = CalcAmount(b);
            long total = cycles * perCycle;

            switch (b.data.rewardType)
            {
                case RewardType.Money:  addMoney += (int)total; break;
                case RewardType.Flower: addRose  += (int)total; break;
                case RewardType.Heart:  addHeart += (int)total; break;
                case RewardType.Boost5s:
                    addBoostSeconds += 5f * (int)total;
                    break;
            }

            // cập nhật mốc claim theo số chu kỳ đã nhận
            b.lastClaimUnix = last + cycles * interval;
            claimedAny = true;
        }

        if (!claimedAny) return false;

        // Apply reward 1 lần (gọn)
        if (addMoney > 0) PlayerMoney.Instance?.AddMoney(addMoney);
        if (addRose  > 0) RoseWallet.Instance?.AddRose(addRose);
        if (addHeart > 0) HeartManager.Instance?.AddHeart();
        if (addBoostSeconds > 0) BoostManager.Instance?.AddBoostSeconds(addBoostSeconds);

        GameSaveManager.Instance?.RequestSave();

        result = new ClaimResult(addMoney, addRose, addHeart, addBoostSeconds);
        return true;
    }

    int CalcAmount(BuildingOnRoad b)
    {
        if (b == null || b.data == null) return 0;
        return b.data.CalcRewardAmount(b.level);
    }


    public readonly struct ClaimResult
    {
        public readonly int money, rose, heart;
        public readonly float boostSeconds;

        public ClaimResult(int money, int rose, int heart, float boostSeconds)
        {
            this.money = money;
            this.rose = rose;
            this.heart = heart;
            this.boostSeconds = boostSeconds;
        }
    }

    public bool HasAnyClaimable()
    {
        if (_active.Count == 0) return false;

        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (int i = 0; i < _active.Count; i++)
        {
            var b = _active[i];
            if (b == null || !b.IsActive || b.data == null) continue;

            int interval = Mathf.Max(1, Mathf.RoundToInt(b.data.intervalSeconds));
            long last = b.lastClaimUnix;
            if (last <= 0) continue;

            if (now - last >= interval) return true;
        }

        return false;
    }

}

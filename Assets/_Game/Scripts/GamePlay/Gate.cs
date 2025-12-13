using System.Collections;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public int totalMoney;

    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponent<HeartStats>();
        if (stats == null) return;

        totalMoney += stats.moneyValue;

        if (stats.gateHitVFX != null)
        {
            Instantiate(stats.gateHitVFX, other.transform.position, Quaternion.identity);

            Destroy(stats.gateHitVFX, 2f);
        }

    }

    
}

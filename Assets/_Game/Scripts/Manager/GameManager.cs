using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.OpenUI<PanelGamePlay>();
    }
}

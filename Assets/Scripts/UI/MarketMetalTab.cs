using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional component on a metal tab button. If missing, MarketUI falls back to the object name
/// (TabAll, TabIron, TabGold, …).
/// </summary>
public class MarketMetalTab : MonoBehaviour
{
    [Tooltip("If true, this tab shows every treasure.")]
    public bool showAll;

    [Tooltip("Ignored when Show All is on.")]
    public MetalType metal;
}

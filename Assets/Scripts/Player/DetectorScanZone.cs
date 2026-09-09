using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DetectorScanZone : MonoBehaviour
{
    private readonly HashSet<BuriedItem> overlappingItems = new HashSet<BuriedItem>();

    public BuriedItem CurrentUndugItem
    {
        get
        {
            foreach (BuriedItem item in overlappingItems)
            {
                if (item == null || item.IsDug || !item.IsDetectable)
                {
                    continue;
                }

                if (PassesDepthFilter(item))
                {
                    return item;
                }
            }

            return null;
        }
    }

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Collider scanCollider = GetComponent<Collider>();
        scanCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        BuriedItem item = other.GetComponentInParent<BuriedItem>();
        if (item != null)
        {
            overlappingItems.Add(item);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BuriedItem item = other.GetComponentInParent<BuriedItem>();
        if (item != null)
        {
            overlappingItems.Remove(item);
        }
    }

    public void Clear()
    {
        overlappingItems.Clear();
    }

    /// <summary>
    /// Lost Items ignore depth. Treasures only beep if their depth is in the active scan band.
    /// </summary>
    public static bool PassesDepthFilter(BuriedItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (item.RequiresQuest || !item.IsTreasure)
        {
            return true;
        }

        DetectorProgress progress = DetectorProgress.Instance;
        if (progress == null)
        {
            // No upgrade component: starter detector (depth 1 only).
            return item.TreasureDepth == DepthLevel.One;
        }

        return progress.CanDetectDepth(item.TreasureDepth);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class LoopCalUtil
{
    public static void GetChildSizes(RectTransform cellTrans, int axis, bool controlSize, bool childForceExpand,
        out float min, out float preferred, out float flexible)
    {
        if (!controlSize)
        {
            min = cellTrans.sizeDelta[axis];
            preferred = min;
            flexible = 0;
        }
        else
        {
            min = LayoutUtility.GetMinSize(cellTrans, axis);
            preferred = LayoutUtility.GetPreferredSize(cellTrans, axis);
            flexible = LayoutUtility.GetFlexibleSize(cellTrans, axis);
        }

        if (childForceExpand)
            flexible = Mathf.Max(flexible, 1);
    }

    public static void RefreshViewAndRebuild(int axis, GameObject go, LoopCell cell)
    {
        if (axis == 1)
        {
            var rectTrans = go.transform as RectTransform;
            //rectTrans.sizeDelta = cell.Sizes;
            rectTrans.sizeDelta = cell.GetRect().size;
        }

        cell.RefreshCell(go);
        var rect = go.transform as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class LoopHorizontalOrVerticalLayout : HorizontalOrVerticalLayoutGroup, ILoopLayout
{
    private ContentSizeFitter _contentSizeFitter;

    private ContentSizeFitter contentSizeFitter =>
        _contentSizeFitter ? _contentSizeFitter : (_contentSizeFitter = GetComponent<ContentSizeFitter>());

    private LoopScrollSource _source;

    public LoopScrollSource Source
    {
        get => _source;
        set
        {
            _source = value;
            SetDirty();
        }
    }

    public LoopObjectPool ObjectPool { get; set; }

    private Action _onLayoutCalculateCompleteEvent;

    public void AddCalculateCompleteEvent(Action action)
    {
        _onLayoutCalculateCompleteEvent = action;
    }

    private new void SetDirty()
    {
        enabled = true;
        contentSizeFitter.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        enabled = false;
        contentSizeFitter.enabled = false;
        _onLayoutCalculateCompleteEvent?.Invoke();
    }

    protected override void OnTransformChildrenChanged()
    {
    }

    protected override void OnDidApplyAnimationProperties()
    {
    }

    protected override void OnRectTransformDimensionsChange()
    {
    }

    protected override void OnEnable()
    {
    }

    protected override void OnDisable()
    {
    }

    protected new void CalcAlongAxis(int axis, bool isVertical)
    {
        if (ObjectPool == null || _source == null)
        {
            return;
        }

        float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
        bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
        bool childForceExpandSize = (axis == 0 ? childForceExpandWidth : childForceExpandHeight);

        float totalMin = combinedPadding;
        float totalPreferred = combinedPadding;
        float totalFlexible = 0;

        bool alongOtherAxis = (isVertical ^ (axis == 1));
        for (int i = 0; i < _source.Count; i++)
        {
            var go = ObjectPool.GetObject(_source[i].GetTmpl());
            LoopCalUtil.RefreshViewAndRebuild(axis, go, _source[i]);

            float min, preferred, flexible;
            LoopCalUtil.GetChildSizes(go.transform as RectTransform, axis, controlSize, childForceExpandSize, out min,
                out preferred, out flexible);

            if (alongOtherAxis)
            {
                totalMin = Mathf.Max(min + combinedPadding, totalMin);
                totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                totalFlexible = Mathf.Max(flexible, totalFlexible);
            }
            else
            {
                totalMin += min + spacing;
                totalPreferred += preferred + spacing;

                // Increment flexible size with element's flexible size.
                totalFlexible += flexible;
            }

            ObjectPool.ReturnObject(go);
        }

        if (!alongOtherAxis && rectChildren.Count > 0)
        {
            totalMin -= spacing;
            totalPreferred -= spacing;
        }

        totalPreferred = Mathf.Max(totalMin, totalPreferred);
        SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
    }

    protected new void SetChildrenAlongAxis(int axis, bool isVertical)
    {
        if (ObjectPool == null || _source == null)
        {
            return;
        }

        float size = rectTransform.rect.size[axis];
        bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
        bool childForceExpandSize = (axis == 0 ? childForceExpandWidth : childForceExpandHeight);
        float alignmentOnAxis = GetAlignmentOnAxis(axis);

        bool alongOtherAxis = (isVertical ^ (axis == 1));
        if (alongOtherAxis)
        {
            float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
            for (int i = 0; i < _source.Count; i++)
            {
                var go = ObjectPool.GetObject(_source[i].GetTmpl());
                var child = go.transform as RectTransform;
                float min, preferred, flexible;
                LoopCalUtil.RefreshViewAndRebuild(axis, go, _source[i]);
                LoopCalUtil.GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred,
                    out flexible);

                float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                float startOffset = GetStartOffset(axis, requiredSpace);
                if (controlSize)
                {
                    _source[i].RefreshCellSizeData(axis, startOffset, requiredSpace);
                }
                else
                {
                    float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                    _source[i].RefreshCellSizeData(axis, startOffset + offsetInCell, child.sizeDelta[axis]);
                }

                ObjectPool.ReturnObject(go);
            }
        }
        else
        {
            float pos = (axis == 0 ? padding.left : padding.top);
            if (GetTotalFlexibleSize(axis) == 0 && GetTotalPreferredSize(axis) < size)
                pos = GetStartOffset(axis,
                    GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));

            float minMaxLerp = 0;
            if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) /
                                           (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

            float itemFlexibleMultiplier = 0;
            if (size > GetTotalPreferredSize(axis))
            {
                if (GetTotalFlexibleSize(axis) > 0)
                    itemFlexibleMultiplier = (size - GetTotalPreferredSize(axis)) / GetTotalFlexibleSize(axis);
            }

            for (int i = 0; i < _source.Count; i++)
            {
                var go = ObjectPool.GetObject( _source[i].GetTmpl());
                var child = go.transform as RectTransform;
                float min, preferred, flexible;
                LoopCalUtil.RefreshViewAndRebuild(axis, go, _source[i]);
                LoopCalUtil.GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred,
                    out flexible);

                float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                childSize += flexible * itemFlexibleMultiplier;
                if (controlSize)
                {
                    _source[i].RefreshCellSizeData(axis, pos, childSize);
                }
                else
                {
                    float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                    _source[i].RefreshCellSizeData(axis, pos + offsetInCell, child.sizeDelta[axis]);
                }

                pos += childSize + spacing;
                ObjectPool.ReturnObject( go);
            }
        }
    }

    public void SetChildAlongAxis(RectTransform child, LoopCell cell, int axis)
    {
        SetChildAlongAxis(child, axis, cell.GetOffset(axis), cell.GetSize(axis));
    }
}
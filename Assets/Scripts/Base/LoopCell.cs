using UnityEngine;

public struct LoopCell
{
    private LoopScrollSource _source;
    private int _index;
    private object _userData;
    private Rect _rect;

    public LoopCell(LoopScrollSource source, int index, object userData, Rect rect)
    {
        _source = source;
        _index = index;
        _userData = userData;
        _rect = rect;
    }

    public void RefreshCellSizeData(int axis, float startOffset, float size)
    {
        if (axis == 0)
        {
            _rect = new Rect()
            {
                x = startOffset,
                y = _rect.y,
                width = size,
                height = _rect.height,
            };
        }
        else
        {
            _rect = new Rect()
            {
                x = _rect.x,
                y = startOffset,
                width = _rect.width,
                height = size,
            };
        }

        _source[_index] = this;
    }

    public Rect GetRect()
    {
        return _rect;
    }

    public float GetOffset(int axis)
    {
        return axis == 0 ? _rect.x : _rect.y;
    }

    public float GetSize(int axis)
    {
        return axis == 0 ? _rect.width : _rect.height;
    }

    public GameObject GetTmpl()
    {
        if (_source is LoopSourceByUserData sourceByUserData)
        {
            return sourceByUserData.GetTmpl(_userData);
        }
        else if (_source is LoopSourceByIndex sourceByIndex)
        {
            return sourceByIndex.GetTmpl(_index);
        }

        return null;
    }

    public void RefreshCell(GameObject obj)
    {
        if (_source is LoopSourceByUserData sourceByUserData)
        {
            sourceByUserData.RefreshCell(obj, _userData);
        }
        else if (_source is LoopSourceByIndex sourceByIndex)
        {
            sourceByIndex.RefreshCell(obj, _index);
        }
    }
}
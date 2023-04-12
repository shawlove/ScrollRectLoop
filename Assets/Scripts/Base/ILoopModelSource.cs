using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class LoopScrollSource
{
    public abstract int Count { get; }

    public abstract LoopCell this[int index] { get; set; }
}

public abstract class LoopSourceByUserData : LoopScrollSource
{
    public abstract GameObject GetTmpl(object userData);
    public abstract void RefreshCell(GameObject obj, object userData);
}

public class LoopSourceByUserData<TUserData> : LoopSourceByUserData
    where TUserData : class
{
    private IList<TUserData> _source;
    private readonly Func<TUserData, GameObject> _getTmpl;
    private readonly Action<GameObject, TUserData> _refresh;

    private readonly List<LoopCell> _loopCells = new List<LoopCell>();

    public LoopSourceByUserData(
        Func<TUserData, GameObject> getTmpl,
        Action<GameObject, TUserData> refresh
    )
    {
        _getTmpl = getTmpl;
        _refresh = refresh;
    }

    public override int Count => _source == null ? 0 : _source.Count;

    public override LoopCell this[int index]
    {
        get => _loopCells[index];
        set => _loopCells[index] = value;
    }

    public void Set(IList<TUserData> source)
    {
        _source = source;
        _loopCells.Clear();
        for (int i = 0; i < source.Count; i++)
        {
            _loopCells.Add(new LoopCell(this, i, _source[i], Rect.zero));
        }
    }

    public override GameObject GetTmpl(object userData)
    {
        return _getTmpl?.Invoke(userData as TUserData);
    }

    public override void RefreshCell(GameObject obj, object userData)
    {
        _refresh?.Invoke(obj, userData as TUserData);
    }
}


public class LoopSourceByIndex : LoopScrollSource
{
    private int _count;
    private readonly Func<int, GameObject> _getTmplByIndex;
    private readonly Action<GameObject, int> _refreshByIndex;

    private readonly List<LoopCell> _loopCells = new List<LoopCell>();

    public LoopSourceByIndex(
        Func<int, GameObject> getTmplByIndex,
        Action<GameObject, int> refreshByIndex
    )
    {
        _getTmplByIndex = getTmplByIndex;
        _refreshByIndex = refreshByIndex;
    }


    public void Set(int count)
    {
        _count = count;
        _loopCells.Clear();
        for (int i = 0; i < count; i++)
        {
            _loopCells.Add(new LoopCell(this, i, null, Rect.zero));
        }
    }

    public GameObject GetTmpl(int index)
    {
        return _getTmplByIndex?.Invoke(index);
    }

    public void RefreshCell(GameObject obj, int index)
    {
        _refreshByIndex?.Invoke(obj, index);
    }

    public override int Count => _count;

    public override LoopCell this[int index]
    {
        get => _loopCells[index];
        set => _loopCells[index] = value;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectLoop : MonoBehaviour, IBeginDragHandler
{
    public GameObject tmpl;
    private ScrollRect _scroll;

    public ScrollRect ScrollRectComp
    {
        get => _scroll ? _scroll : (_scroll = GetComponent<ScrollRect>());
    }

    private RectTransform rectTransform => transform as RectTransform;

    private RectTransform _contentRectTransform;

    private RectTransform contentRectTransform
    {
        get => _contentRectTransform ? _contentRectTransform : (_contentRectTransform = ScrollRectComp.content);
    }

    private HashSet<int> _showIndexes = new HashSet<int>(); //应该显示的index
    private Dictionary<GameObject, int> _currentRenderCell = new(); // 正在显示的index
    private HashSet<int> _rendingIndexes = new HashSet<int>();
    private LoopObjectPool _objectPool = new LoopObjectPool();

    private ILoopLayout _loopLayout;

    private LoopScrollSource _source;

    private void ReturnObject(GameObject go)
    {
        _objectPool.ReturnObject(go);
    }

    private void Awake()
    {
        _loopLayout = contentRectTransform.GetComponent<ILoopLayout>();
        _loopLayout.ObjectPool = _objectPool;
        ScrollRectComp.onValueChanged.AddListener(OnScrollValueChange);
        _loopLayout.AddCalculateCompleteEvent(() => { RenderCells(true); });
        _objectPool.SetLoopContent(contentRectTransform);
        _objectPool.SetDefaultTmpl(tmpl);
    }

    public void InitUserDataCallback<TUserData>(
        Action<GameObject> init,
        Action<GameObject, TUserData> refresh,
        Func<TUserData, GameObject> getTmpl = null
    )
        where TUserData : class
    {
        _source = new LoopSourceByUserData<TUserData>(getTmpl, refresh);
        _objectPool.SetInitCallback(init);
    }

    public void InitIndexCallback(
        Action<GameObject> init,
        Action<GameObject, int> refreshByIndex,
        Func<int, GameObject> getTmplByIndex = null
    )
    {
        _source = new LoopSourceByIndex(getTmplByIndex, refreshByIndex);
        _objectPool.SetInitCallback(init);
    }

    public void RefreshViewByUserData<T>(IList<T> source) where T : class
    {
        if (_source == null)
        {
            Debug.LogError("ScrollRectLoop未初始化");
            return;
        }

        if (_source is LoopSourceByUserData<T> sourceByUserData)
        {
            sourceByUserData.Set(source);
        }
        else
        {
            Debug.LogError("与初始化类型不同");
            return;
        }

        _loopLayout.Source = _source;
    }

    public void RefreshViewByCount(int count)
    {
        if (_source == null)
        {
            Debug.LogError("ScrollRectLoop未初始化");
            return;
        }

        if (_source is LoopSourceByIndex sourceByIndex)
        {
            sourceByIndex.Set(count);
        }
        else
        {
            Debug.LogError("与初始化类型不同");
            return;
        }

        _loopLayout.Source = _source;
    }

    private void OnScrollValueChange(Vector2 normalization)
    {
        RenderCells();
    }

    private void RenderCells(bool isRemoveAll = false)
    {
        var normalization = ScrollRectComp.normalizedPosition;
        GetVisualCell(normalization);
        RemoveCells(isRemoveAll);

        foreach (int index in _showIndexes)
        {
            if (!_rendingIndexes.Contains(index))
            {
                var baseLoopModel = _source[index];
                var cell = _objectPool.GetObject(baseLoopModel.GetTmpl());
                _currentRenderCell.Add(cell, index);
                baseLoopModel.RefreshCell(cell);
                _loopLayout.SetChildAlongAxis(cell.transform as RectTransform, baseLoopModel, 0);
                _loopLayout.SetChildAlongAxis(cell.transform as RectTransform, baseLoopModel, 1);
            }
        }
    }

    private void RemoveCells(bool isRemoveAll = false)
    {
        _rendingIndexes.Clear();
        if (isRemoveAll)
        {
            for (int i = contentRectTransform.childCount - 1; i >= 0; i--)
            {
                ReturnObject(contentRectTransform.GetChild(i).gameObject);
            }

//            _showIndexes.Clear();
            _currentRenderCell.Clear();
            return;
        }

        var tmpRemove = new List<GameObject>();
        foreach (var pair in _currentRenderCell)
        {
            if (!_showIndexes.Contains(pair.Value))
            {
                tmpRemove.Add(pair.Key);
                ReturnObject(pair.Key);
            }
            else
            {
                _rendingIndexes.Add(pair.Value);
            }
        }

        foreach (var cell in tmpRemove)
        {
            _currentRenderCell.Remove(cell);
        }
    }

    private void GetVisualCell(Vector2 normalization)
    {
        _showIndexes.Clear();
        if (_source == null)
        {
            return;
        }

        var sizeDelta = contentRectTransform.sizeDelta;

        var scrollSizeDelta = rectTransform.sizeDelta;

        float scrollXFrom = normalization.x * (sizeDelta[0] - scrollSizeDelta[0]);
        float scrollYFrom = (1 - normalization.y) * (sizeDelta[1] - scrollSizeDelta[1]);
        float scrollXTo = scrollXFrom + scrollSizeDelta[0];
        float scrollYTo = scrollYFrom + scrollSizeDelta[1];

        var contentRect = new Rect(scrollXFrom, scrollYFrom, scrollXTo - scrollXFrom, scrollYTo - scrollYFrom);
        for (int i = 0; i < _source.Count; i++)
        {
            var loopModel = _source[i];
            var cellRect = loopModel.GetRect();
            if (contentRect.Overlaps(cellRect))
            {
                _showIndexes.Add(i);
            }
        }
    }

    private Coroutine _smoothMoveCo;

    public void ScrollToIndex(int index, bool smoothMove = false, float duration = 0.1f)
    {
        var model = _source[index];
        var rect = model.GetRect();
        var sizeDelta = contentRectTransform.sizeDelta;
        var scrollSizeDelta = rectTransform.sizeDelta;
        var normalizationDelta = sizeDelta - scrollSizeDelta;
        var xNormalization = rect.xMin / normalizationDelta[0];
        var yNormalization = rect.yMin / normalizationDelta[1];
        if (smoothMove)
        {
            var targetNormalizationPos = new Vector2(xNormalization, 1 - yNormalization);
            _smoothMoveCo = StartCoroutine(SmoothMove(targetNormalizationPos, duration));
        }
        else
        {
            ScrollRectComp.normalizedPosition = new Vector2(xNormalization, 1 - yNormalization);
        }
    }

    private IEnumerator SmoothMove(Vector2 targetPos, float duration)
    {
        var currentPos = ScrollRectComp.normalizedPosition;
        var time = 0f;
        while (true)
        {
            var t = time / duration;
            ScrollRectComp.normalizedPosition = Vector2.Lerp(currentPos, targetPos, t);
            time += Time.deltaTime;
            yield return null;
            if (time >= duration)
            {
                ScrollRectComp.normalizedPosition = targetPos;
                break;
            }
        }
    }


    private void OnDestroy()
    {
        _objectPool.ClearPool();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_smoothMoveCo != null)
        {
            StopCoroutine(_smoothMoveCo);
        }
    }
}
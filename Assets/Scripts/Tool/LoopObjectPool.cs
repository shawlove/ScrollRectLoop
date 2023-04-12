using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class LoopObjectPool
{
    //key : tmpl
    private Dictionary<GameObject, Queue<GameObject>> _poolDic = new Dictionary<GameObject, Queue<GameObject>>();

    //key : hashcode
    private Dictionary<int, GameObject> _tmplDic = new Dictionary<int, GameObject>();

    private RectTransform _contentTrans;
    private RectTransform _objectPoolContainer;

    private GameObject _defaultTmpl;
    private Action<GameObject> _initCallback;

    public void SetDefaultTmpl(GameObject tmpl)
    {
        _defaultTmpl = tmpl;
    }

    public void SetInitCallback(Action<GameObject> init)
    {
        _initCallback = init;
    }

    public void SetLoopContent(RectTransform contentTrans)
    {
        _contentTrans = contentTrans;
        if (_objectPoolContainer == null)
        {
            var go = new GameObject("ObjectPoolContainer");
            _objectPoolContainer = go.AddComponent<RectTransform>();
            _objectPoolContainer.SetParent(_contentTrans.parent);
            _objectPoolContainer.localPosition = Vector3.zero;
            _objectPoolContainer.localScale = Vector3.one;
        }
    }

    public GameObject GetObject(GameObject tmpl)
    {
        if (tmpl == null)
        {
            tmpl = _defaultTmpl;
        }

        if (!_poolDic.TryGetValue(tmpl, out var queue))
        {
            queue = new Queue<GameObject>();
            _poolDic.Add(tmpl, queue);
        }

        var go = queue.Count != 0 ? queue.Dequeue() : CreateObject(tmpl);
        go.SetActive(true);
        go.transform.SetParent(_contentTrans);
        go.transform.localScale = Vector3.one;
        go.transform.position = Vector3.zero;
        return go;
    }

    private GameObject CreateObject(GameObject tmpl)
    {
        var go = Object.Instantiate(tmpl, _contentTrans, true);
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;
        _tmplDic[go.GetHashCode()] = tmpl;
        _initCallback?.Invoke(go);
        return go;
    }

    public void ReturnObject(GameObject go)
    {
        if (_tmplDic.TryGetValue(go.GetHashCode(), out var tmpl) == false)
        {
            Debug.LogError("ScrollRectLoop回收失败 未找到tmpl");
            return;
        }

        if (_poolDic.TryGetValue(tmpl, out var queue))
        {
            go.transform.SetParent(_objectPoolContainer);
            go.SetActive(false);
            queue.Enqueue(go);
        }
        else
        {
            Debug.LogError("回收失败");
        }
    }

    public void ClearPool()
    {
        _contentTrans = null;

        Object.Destroy(_objectPoolContainer);

        _tmplDic.Clear();
        _poolDic.Clear();
    }

    //这个方法是给一些对cell的rect有要求的用的，可以直接获取到所有register的cell原型
    public IEnumerable<GameObject> GetAllPrefabs()
    {
        return _poolDic.Keys;
    }
}
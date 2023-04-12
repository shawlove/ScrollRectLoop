using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILoopLayout
{
    LoopObjectPool ObjectPool { set; }
    LoopScrollSource Source { set; }
    void AddCalculateCompleteEvent(Action action);
    void SetChildAlongAxis(RectTransform child, LoopCell cell, int axis);
}

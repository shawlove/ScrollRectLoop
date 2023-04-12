using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestCell : MonoBehaviour
{
    private Text _testTxt;

    private void Awake()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        _testTxt = transform.Find("Text").GetComponent<Text>();
    }

    public void BuildData(TestModel testModel)
    {
        _testTxt.text = testModel.content;
    }
}
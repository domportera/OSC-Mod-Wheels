﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class FaderControlUi : MonoBehaviour, ISortingMember
{
    private RangeController _rangeController;
    [SerializeField] private Slider _slider;
    [SerializeField] private EventTrigger _eventTrigger;
    [SerializeField] private Text _label;
    [SerializeField] private Button _sortLeftButton;
    [SerializeField] private Button _sortRightButton;

    public void Initialize(FaderData controlData)
    {
        _rangeController = new RangeController(controlData.GetSettings());
        var rectTransform = GetComponent<RectTransform>();
        var initialSizeDelta = rectTransform.sizeDelta;

        var displayName = controlData.GetName();
        _label.text = displayName;
        name = displayName + " Fader";
        InitializeFaderInteraction();
        InitializeSorting();
        
        rectTransform.sizeDelta = new Vector2(initialSizeDelta.y * controlData.GetWidth(), initialSizeDelta.y);
    }

    // Update is called once per frame
    private void Update()
    {
        _rangeController.Update(Time.deltaTime);
        _slider.SetValueWithoutNotify(_rangeController.SmoothValue);
    }

    private void InitializeFaderInteraction()
    {
        _slider.maxValue = RangeController.MaxControllerValue;
        _slider.minValue = RangeController.MinControllerValue;
        _slider.onValueChanged.AddListener(f => _rangeController.SetValue(f));

        var startEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        startEntry.callback.AddListener(_ => StartSliding());
        _eventTrigger.triggers.Add(startEntry);

        var endEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerUp
        };
        endEntry.callback.AddListener(_ => EndSliding());
        _eventTrigger.triggers.Add(endEntry);
    }


    private void StartSliding()
    {

    }

    private void EndSliding()
    {
        _rangeController.Release();
    }

    #region Sorting
    public void InitializeSorting()
    {
        _sortLeftButton.onClick.AddListener(SortLeft);
        _sortRightButton.onClick.AddListener(SortRight);
        SetSortButtonVisibility(false);
    }

    public void SetSortButtonVisibility(bool visible)
    {
        _sortLeftButton.gameObject.SetActive(visible);
        _sortRightButton.gameObject.SetActive(visible);

        var sliderImages = _slider.GetComponentsInChildren<Image>();

        foreach (var i in sliderImages)
        {
            i.enabled = !visible;
        }
    }

    public void SortLeft()
    {
        SortPosition(false);
    }

    public void SortRight()
    {
        SortPosition(true);
    }

    public void SortPosition(bool right)
    {
        transform.SetSiblingIndex(right ? transform.GetSiblingIndex() + 1 : transform.GetSiblingIndex() - 1);
    }
    #endregion Sorting
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.UI.Dropdown;

public class UIManager : MonoBehaviour
{
    [SerializeField] ControlsManager controlMan = null;
    [SerializeField] GameObject optionsPanel = null;
    [SerializeField] GameObject sliderOptionsButtonLayoutPrefab = null;
    [SerializeField] GameObject faderOptionsPrefab = null;
    [SerializeField] GameObject faderOptionsActivationPrefab = null; //the prefab for the button that opens up fader options
    [SerializeField] GameObject sliderButtonVerticalLayoutParent = null;
    [SerializeField] Button optionsButton = null;
    [SerializeField] Button closeOptionsButton = null;
    [SerializeField] Button newControllerButton = null;

    [Space(10)]
    [SerializeField] Slider faderWidthSlider = null;
    [SerializeField] Button faderPositionEnableButton = null;
    [SerializeField] Button faderPositionExitButton = null;
    [SerializeField] HorizontalLayoutGroup faderLayoutGroup = null;

    [SerializeField] Button setupButton;
    [SerializeField] Button closeSetupButton;
    [SerializeField] GameObject setupPanel;

    [Header("Credits")]
    [SerializeField] Button creditsButton;
    [SerializeField] Button closeCreditsButton;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] List<CreditsButton> creditsButtons = new List<CreditsButton>();
    [SerializeField] List<DonationButton> donationButtons = new List<DonationButton>();

    [Serializable]
    struct CreditsButton
    {
        public Button button;
        public string link;
    }

    [Serializable]
    struct DonationButton
    {
        public Button button;
        public string address;
    }

    const int sliderButtonLayoutCapacity = 5;

    List<ControllerUIGroup> controllerUIs = new List<ControllerUIGroup>();
    List<LayoutGroupButtonCount> layoutCounts = new List<LayoutGroupButtonCount>();

    const int DEFAULT_FADER_WIDTH = 200;
    int faderWidth = DEFAULT_FADER_WIDTH;

    const string FADER_WIDTH_PLAYER_PREF = "Fader Width";
    bool positionMode = false;


    // Start is called before the first frame update
    void Awake()
    {
        //options
        optionsButton.onClick.AddListener(ToggleOptionsMenu);
        closeOptionsButton.onClick.AddListener(ToggleOptionsMenu);
        faderPositionExitButton.onClick.AddListener(ToggleEditFaderPositionMode);
        //prevent accidentally leaving stuff on in the scene
        optionsPanel.SetActive(false);


        //if not loaded, use default //when faderwidth is loaded, it will need to change the size of faders. faders should be playerprefs
        if (PlayerPrefs.HasKey(FADER_WIDTH_PLAYER_PREF))
        {
            faderWidth = PlayerPrefs.GetInt(FADER_WIDTH_PLAYER_PREF);
            SetFaderWidths();
            RefreshFaderLayoutGroup();
        }

        faderWidthSlider.SetValueWithoutNotify(faderWidth);
        faderWidthSlider.onValueChanged.AddListener(SetFaderWidthBySlider);
        faderPositionEnableButton.onClick.AddListener(ToggleEditFaderPositionMode);
        newControllerButton.onClick.AddListener(controlMan.NewController);

        UnityAction toggleSetup = () => setupPanel.SetActive(!setupPanel.activeSelf);
        setupButton.onClick.AddListener(toggleSetup);
        closeSetupButton.onClick.AddListener(toggleSetup);

        UnityAction toggleCredits = () => creditsPanel.SetActive(!creditsPanel.activeSelf);
        creditsButton.onClick.AddListener(toggleCredits);
        closeCreditsButton.onClick.AddListener(toggleCredits);

        InitializeCreditsButtons();
        InitializeDonationButtons();
    }

    //used by options button in scene
    public void ToggleOptionsMenu()
    {
        optionsPanel.SetActive(!optionsPanel.activeInHierarchy);
    }


    public void SpawnControllerOptions(ControllerSettings _config, GameObject _control)
    {
        //check if any other controller buttons exist for this, then destroy all its contents
        //make sure to destroy faderOptions as well
        ControllerUIGroup dupe = GetButtonGroupByConfig(_config);
        if (dupe != null)
        {
            DestroyControllerGroup(dupe);
        }

        ControllerUIGroup buttonGroup = new ControllerUIGroup(_config, faderOptionsPrefab, faderOptionsActivationPrefab, _control);

        buttonGroup.faderOptions.transform.SetParent(optionsPanel.transform, false);
        controllerUIs.Add(buttonGroup);
        SetFaderWidth(buttonGroup);
        SortOptionsButtons();
        RefreshFaderLayoutGroup();
    }

    void InitializeCreditsButtons()
    {
        foreach(CreditsButton b in creditsButtons)
        {
            b.button.onClick.AddListener(() => Application.OpenURL(b.link));
        }
    }

    void InitializeDonationButtons()
    {
        foreach(DonationButton b in donationButtons)
        {
            b.button.onClick.AddListener(() =>
            {
                UniClipboard.SetText(b.address);
                Utilities.instance.ConfirmationWindow($"Copied {b.address} to clipboard!");
            });
        }
    }

    void AddOptionsButtonToLayout(GameObject _button)
    {
        for(int i = 0; i < layoutCounts.Count; i++)
        {
            if(layoutCounts[i].count < sliderButtonLayoutCapacity)
            {
                _button.transform.SetParent(layoutCounts[i].layoutGroup.transform);
                _button.transform.SetSiblingIndex(layoutCounts[i].count);
                layoutCounts[i].count++;
                return;
            }
        }

        //all layouts full, create a new one

        AddNewLayoutCount();
        AddOptionsButtonToLayout(_button);
    }

    public void DestroyControllerGroup(ControllerSettings _config)
    {
        DestroyControllerGroup(GetButtonGroupByConfig(_config));
    }

    void DestroyControllerGroup(ControllerUIGroup _buttonGroup)
    {
        //destroy all params
        _buttonGroup.SelfDestruct();

        //remove from list
        controllerUIs.Remove(_buttonGroup);

        //check for empty layouts, and destroy it
        DestroyEmptyLayouts();

        SortOptionsButtons();
    }

    void DestroyEmptyLayouts()
    {
        for(int i = 0; i < layoutCounts.Count; i++)
        {
            if(layoutCounts[i].count == 0)
            {
                //destroy
                Destroy(layoutCounts[i].layoutGroup);
                layoutCounts.RemoveAt(i);
                i--;
            }
        }
    }

    public void DestroyControllerObjects(ControllerSettings _config)
    {
        ControllerUIGroup buttonGroup = GetButtonGroupByConfig(_config);
        DestroyControllerGroup(buttonGroup);
    }

    void RemoveFromLayout(ControllerUIGroup _buttonGroup)
    {
        LayoutGroupButtonCount layout = GetLayoutGroupFromObject(_buttonGroup.faderMenuButton.gameObject);

        if (layout != null)
        {
            layout.count--;
        }
        else
        {
            Debug.LogError("Null layout! button didn't find its parent.");
        }
    }

    void AddNewLayoutCount()
    {
        LayoutGroupButtonCount lay = new LayoutGroupButtonCount();
        lay.layoutGroup = Instantiate(sliderOptionsButtonLayoutPrefab);
        lay.layoutGroup.transform.SetParent(sliderButtonVerticalLayoutParent.transform);
        layoutCounts.Add(lay);
    }

    bool GetControllerEnabled(FaderOptions _faderOptions)
    {
        ControllerUIGroup group = GetButtonGroupByFaderOptions(_faderOptions);

        if(group.activationToggle.isOn)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    ControllerUIGroup GetButtonGroupByConfig(ControllerSettings _config)
    {
        foreach (ControllerUIGroup cbg in controllerUIs)
        {
            if (cbg.controllerConfig == _config)
            {
                return cbg;
            }
        }
        return null;
    }

    ControllerUIGroup GetButtonGroupByFaderOptions(FaderOptions _faderOptions)
    {
        foreach (ControllerUIGroup cbg in controllerUIs)
        {
            if(cbg.faderOptions == _faderOptions)
            {
                return cbg;
            }
        }

        Debug.LogError("Didn't find a match for button group! Returning empty.");
        return null;
    }

    LayoutGroupButtonCount GetLayoutGroupFromObject(GameObject _button)
    {
        foreach(LayoutGroupButtonCount lay in layoutCounts)
        {
            Transform[] children = lay.layoutGroup.GetComponentsInChildren<Transform>();

            foreach(Transform child in children)
            {
                if(child.gameObject == _button)
                {
                    return lay;
                }
            }
        }

        return null;
    }

    void SortOptionsButtons()
    {
        foreach(LayoutGroupButtonCount layCount in layoutCounts)
        {
            layCount.count = 0;
        }

        //get all the unnamed ones out of sorting list
        List<ControllerUIGroup> unnamedControls = new List<ControllerUIGroup>();
        for (int i = 0; i < controllerUIs.Count; i++)
        {
            if(controllerUIs[i].controllerConfig.name == ControlsManager.NEW_CONTROLLER_NAME)
            {
                unnamedControls.Add(controllerUIs[i]);
                controllerUIs.Remove(controllerUIs[i]);
                i--;
            }
        }

        //sort buttons
        controllerUIs.Sort((s1, s2) => s1.controllerConfig.GetPosition().CompareTo(s2.controllerConfig.GetPosition()));

        //add unnamed ones to the end
        foreach (ControllerUIGroup c in unnamedControls)
        {
            controllerUIs.Add(c);
        }

        //place buttons
        foreach (ControllerUIGroup c in controllerUIs)
        {
            AddOptionsButtonToLayout(c.faderMenuButton.gameObject);
        }
    }

    void SetFaderWidths()
    {
        //set to fader slider value - this should be set with loaded value, then this can be called.
        foreach(ControllerUIGroup c in controllerUIs)
        {
            SetFaderWidth(c);
        }

        RefreshFaderLayoutGroup();
    }

    void RefreshFaderLayoutGroup()
    {
        faderLayoutGroup.enabled = false;
        faderLayoutGroup.enabled = true;
    }

    void SetFaderWidth(ControllerUIGroup _group)
    {
        _group.SetFaderWidth(faderWidth);
    }

    public void SetFaderWidthBySlider(float _width)
    {
        faderWidth = (int)_width;
        SetFaderWidths();
    }

    void ToggleEditFaderPositionMode()
    {
        positionMode = !positionMode;

        foreach(ControllerUIGroup u in controllerUIs)
        {
            u.SetPostionMode(positionMode);
            u.SetPosition();
        }

        //toggle options buttons
        optionsButton.gameObject.SetActive(!positionMode);
        optionsPanel.SetActive(!positionMode);
        faderPositionExitButton.gameObject.SetActive(positionMode);

        if(!positionMode)
        {
            Utilities.instance.ConfirmationWindow("Don't forget to save!");
        }
    }

    class LayoutGroupButtonCount
    {
        public GameObject layoutGroup;
        public int count = 0;
    }

    class ControllerUIGroup
    {
        //should be public get private set
        public FaderOptions faderOptions;
        public Button faderMenuButton;
        public ControllerSettings controllerConfig;
        public Toggle activationToggle;
        public GameObject controlObject;
        FaderControl control;
        RectTransform controlObjectTransform;

        public ControllerUIGroup(ControllerSettings _config, GameObject _faderOptionsPrefab, GameObject _optionsActivateButtonPrefab, GameObject _controlObject)
        {
            faderOptions = Instantiate(_faderOptionsPrefab).GetComponent<FaderOptions>();
            faderOptions.gameObject.name = _config.name + " Options Panel";
            faderOptions.controllerConfig = _config;

            controllerConfig = _config;

            faderMenuButton = Instantiate(_optionsActivateButtonPrefab).GetComponent<Button>();
            faderMenuButton.gameObject.name = _config.name + " Options";
            faderMenuButton.GetComponentInChildren<Text>().text = _config.name + " Options"; // change visible button title
            faderMenuButton.onClick.AddListener(DisplayFaderOptions);

            activationToggle = faderMenuButton.GetComponentInChildren<Toggle>();
            activationToggle.onValueChanged.AddListener(ToggleControlVisibility);
            controlObject = _controlObject;
            controlObjectTransform = _controlObject.GetComponent<RectTransform>();

            control = _controlObject.GetComponent<FaderControl>();
        }

        public void SetFaderWidth(float _width)
        {
            controlObjectTransform.sizeDelta = new Vector2(_width, controlObjectTransform.sizeDelta.y);
        }

        void DisplayFaderOptions()
        {
            faderOptions.gameObject.SetActive(true);
        }

        public void ToggleControlVisibility(bool _b)
        {
            controlObject.SetActive(_b);

            if(!_b)
            {
                controlObjectTransform.SetAsLastSibling();
            }
        }

        public void SetPosition()
        {
            controllerConfig.SetPosition(controlObjectTransform.GetSiblingIndex());
        }

        public void SetPostionMode(bool _b)
        {
            control.SetSortButtonVisibility(_b);
        }

        public void SelfDestruct()
        {
            Destroy(faderOptions.gameObject);
            Destroy(faderMenuButton.gameObject);
            Destroy(controlObject);
        }
    }
}

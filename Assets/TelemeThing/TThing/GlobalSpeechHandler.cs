using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using System.Threading;

public class DroneMenuHandler : MonoBehaviour
{
    private static GameObject _droneMenu = null;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        _droneMenu = GameObject.Find("DroneMenu");
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Update()
    { }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="trueToShow"></param>
    //*************************************************************************
    public static void ShowMenu(bool trueToShow)
    {
        var dml = Utils.FindObjectsInScene("DroneMenu");

        if (dml.Count == 0)
            return;

        dml[0]?.SetActive(trueToShow);
    }
}

public class DeclinationSliderHandler : MonoBehaviour
{
    private static GameObject _declinationSlider = null;
    private static bool _interacting = false;
    private static float _sliderVal= 0f;
    private static DeclinationSliderHandler _singleton = null;
    private static Thread AdjustLoopThread = null;
    private static Microsoft.MixedReality.Toolkit.UI.PinchSlider _slider = null;

    private static float _midVal = .5f;

    //Microsoft.MixedReality.Toolkit.UI.PinchSlider _declinationSlider = null;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        _declinationSlider = GameObject.Find("DeclinationSlider");

        //var hh = _declinationSlider as Microsoft.MixedReality.Toolkit.UI.PinchSlider;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Update()
    {
        if (_interacting)
        {
            //MixedRealityPlayspace.Rotation = Quaternion.Euler(Vector3.up * (float)pose.Orient.True);

            var increment = _sliderVal * .1;

            MixedRealityPlayspace.Rotation *= Quaternion.Euler(Vector3.up * (float)increment);
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="trueToShow"></param>
    //*************************************************************************
    public static void ShowMenu(bool trueToShow)
    {
        var dml = Utils.FindObjectsInScene("DeclinationSlider");

        if (dml.Count == 0)
            return;

        dml[0]?.SetActive(trueToShow);
    }

    /*private static IEnumerator AdjustLoop()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            var increment = _sliderVal * .1;
            MixedRealityPlayspace.Rotation *= Quaternion.Euler(Vector3.up * (float)increment);
        }
    }*/

    private static void AdjustLoop()
    {
        while (_interacting)
        {
            //var increment = _sliderVal * .1f;
            //MixedRealityPlayspace.Rotation *= Quaternion.Euler(Vector3.up * (float)increment);

            ThingsManager.ManualDeclinationChange = (_sliderVal - _midVal) * .1f; ;

            Thread.Sleep(20);
        }

        //set the slider back to middle after user lets go
        if(null != _slider)
            _slider.SliderValue = _midVal;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="trueToShow"></param>
    //*************************************************************************
    public static void InteractionStarted(bool trueIfStarted)
    {
        _interacting = trueIfStarted;

        if (_interacting)
        {
            //Coroutine coroutine = MonoBehaviour.StartCoroutine("AdjustLoop");

            if (null != AdjustLoopThread)
                AdjustLoopThread.Abort();

            AdjustLoopThread = new Thread(new ThreadStart(AdjustLoop));
            AdjustLoopThread.Start();
        }            
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="newValue"></param>
    //*************************************************************************
    public static void ValueUpdated(float newValue, 
        Microsoft.MixedReality.Toolkit.UI.PinchSlider slider)
    {
        _sliderVal = newValue;
        _slider = slider;
    }
}

public class GlobalSpeechHandler : MonoBehaviour, IMixedRealitySpeechHandler, IMixedRealitySourceStateHandler
{
    #region Housekeeping
    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {}

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Update()
    {}

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void OnEnable()
    {
        // Instruct Input System that we would like to receive all input events of type
        // IMixedRealitySourceStateHandler and IMixedRealityHandJointHandler
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void OnDisable()
    {
        // This component is being destroyed
        // Instruct the Input System to disregard us for input event handling
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityHandJointHandler>(this);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    //*************************************************************************
    public void OnSourceDetected(SourceStateEventData eventData)
    {
        /*var hand = eventData.Controller as IMixedRealityHand;

        // Only react to articulated hand input sources
        if (hand != null)
        {
            Debug.Log("Source detected: " + hand.ControllerHandedness);
        }*/

        Debug.Log("Source detected: " + eventData.ToString());
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    //*************************************************************************
    public void OnSourceLost(SourceStateEventData eventData)
    {
        /*var hand = eventData.Controller as IMixedRealityHand;

        // Only react to articulated hand input sources
        if (hand != null)
        {
            Debug.Log("Source lost: " + hand.ControllerHandedness);
        }*/
        
        Debug.Log("Source lost: " + eventData.ToString());
    }
    #endregion //Housekeeping

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    //*************************************************************************
    public void OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        Debug.Log("--- OnSpeechKeywordRecognized: " + eventData.Command.Keyword);

        if (eventData.Command.Keyword == "Show Drone Menu")
        {
            DroneMenuHandler.ShowMenu(true);
            return;
        }
        if (eventData.Command.Keyword == "Hide Drone Menu")
        {
            DroneMenuHandler.ShowMenu(false);
            return;
        }
        if (eventData.Command.Keyword == "Find Drone")
        {
            transform.localScale *= 0.5f;
            return;
        }
        if (eventData.Command.Keyword == "smaller")
        {
            transform.localScale *= 0.5f;
            return;
        }
        else if (eventData.Command.Keyword == "bigger")
        {
            transform.localScale *= 2.0f;
            return;
        }
    }

}

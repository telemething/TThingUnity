using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

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
        _droneMenu = GameObject.Find("DroneMenu");
        _droneMenu?.SetActive(trueToShow);
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

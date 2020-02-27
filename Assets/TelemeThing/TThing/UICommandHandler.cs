using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using TThingComLib;
using TThingComLib.Messages;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.EventSystems;

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class UICommandHandler : MonoBehaviour
{
    private TThingComLib.TThingCom _tthingCom = new TThingCom();
    private Thing _self;
    private string _myFromName = "*";

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        Connect(); //*** TODO * Should we always connect at startup, should we retry?
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    // Update is called once per frame
    //*************************************************************************
    void Update()
    {
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void Connect()
    {
        _tthingCom.Connect();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void FindSelf()
    {
        _self = ThingsManager.Self;
        if (null != _self)
            _myFromName = _self.Id;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //*************************************************************************
    private string FindTargetName()
    {
        return "aDrone"; //*** TODO * Find actual target name
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="to"></param>
    /// <param name="commandId"></param>
    //*************************************************************************
    private void SendTtCommand(string to, TThingComLib.Messages.CommandIdEnum commandId)
    {
        try
        {
            FindSelf();

            Debug.Log("--- UICommandHandler:SendTtCommand() : " + commandId.ToString());

            _tthingCom.Send(TThingComLib.Messages.Message.CreateCommand(
                _myFromName, to, commandId));
        }
        catch (Exception e)
        {
            Debug.Log("--- UICommandHandler:SendCommand_RunMission() Exception" + e.Message);
            Console.WriteLine("---UICommandHandler:SendCommand_RunMission() Exception" + e.Message);
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_RunMission()
    {
        SendTtCommand(FindTargetName(), CommandIdEnum.StartMission);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_StopDrone()
    {
        SendTtCommand(FindTargetName(), CommandIdEnum.StopDrone);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_ReturnHome()
    {
        SendTtCommand(FindTargetName(), CommandIdEnum.ReturnHome);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_LandDrone()
    {
        SendTtCommand(FindTargetName(), CommandIdEnum.LandDrone);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_HideDroneMenu()
    {
        DroneMenuHandler.ShowMenu(false);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_ShowDroneMenu()
    {
        DroneMenuHandler.ShowMenu(true);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_HideCalibrateMenu()
    {
        DeclinationSliderHandler.ShowMenu(false);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void SendCommand_ShowCalibrateMenu()
    {
        DeclinationSliderHandler.ShowMenu(true);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void CalibrateSlider_Interaction(bool started)
    {
        DeclinationSliderHandler.InteractionStarted(started);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    //*************************************************************************
    public void CalibrateSlider_Updated(SliderEventData eventData)
    {
        DeclinationSliderHandler.ValueUpdated(eventData.NewValue, eventData.Slider);
    }
}




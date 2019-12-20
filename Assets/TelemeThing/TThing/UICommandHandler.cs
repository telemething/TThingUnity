using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using TThingComLib;
using TThingComLib.Messages;
using UnityEngine;

public class UICommandHandler : MonoBehaviour
{
    private TThingComLib.TThingCom _tthingCom = new TThingCom();

    // Start is called before the first frame update
    void Start()
    {
        Connect(); //*** TODO * Should we always connect at startup, should we retry?
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Connect()
    {
        _tthingCom.Connect();
    }

    public void SendCommand_RunMission()
    {
        try
        {
            Debug.Log("--- UICommandHandler:SendCommand_RunMission()");

            //_tthingCom.Send(new StartMission());

            _tthingCom.Send(TThingComLib.Messages.Message.CreateCommand(
                "from","to", TThingComLib.Messages.CommandIdEnum.StartMission));
        }
        catch (Exception e)
        {
            Debug.Log("--- UICommandHandler:SendCommand_RunMission() Exception" + e.Message);
            Console.WriteLine("---UICommandHandler:SendCommand_RunMission() Exception" + e.Message);
        }
    }

    public void SendCommand_StopDrone()
    {
        Debug.Log("--- UICommandHandler:SendCommand_StopDronen()");
    }

    public void SendCommand_ReturnHome()
    {
        Debug.Log("--- UICommandHandler:SendCommand_ReturnHomen()");
    }

    public void SendCommand_LandDrone()
    {
        Debug.Log("--- UICommandHandler:SendCommand_LandDrone()");
    }

    public void SendCommand_HideDroneMenu()
    {
        Debug.Log("--- UICommandHandler:SendCommand_HideDroneMenu()");
    }
}

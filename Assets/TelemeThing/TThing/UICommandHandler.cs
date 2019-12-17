using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class UICommandHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendCommand_RunMission()
    {
        Debug.Log("--- UICommandHandler:SendCommand_RunMission()");
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

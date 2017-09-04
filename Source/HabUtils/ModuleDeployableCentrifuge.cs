using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;


namespace HabUtils
{
  public class ModuleDeployableCentrifuge: ModuleDeployableHabitat
  {
    // Is the centrifuge rotation enabled or not?
    [KSPField(isPersistant = true)]
    public bool Rotating = false;

    // The radius for gravity generation purposes
    [KSPField(isPersistant = false)]
    public float Radius = 0.0f;

    // The CurrentSpinRate
    [KSPField(isPersistant = false)]
    public float CurrentSpinRate = 0.0f;

    // Speed of the centrifuge rotation in deg/s
    [KSPField(isPersistant = false)]
    public float SpinRate = 10.0f;

    // Rate at which the SpinRate accelerates (deg/s/s)
    [KSPField(isPersistant = false)]
    public float SpinAccelerationRate = 1.0f;

    // Transform to rotate for the centrifuge
    [KSPField(isPersistant = false)]
    public string SpinTransformName = "";

    // The CurrentSpinRate
    [KSPField(isPersistant = false)]
    public float CurrentCounterweightSpinRate = 0.0f;

    // Rate at which the counterweight spins in deg/s
    [KSPField(isPersistant = false)]
    public float CounterweightSpinRate = 20.0f;

    // Rate at which the counterweight accelerates (deg/s/s)
    [KSPField(isPersistant = false)]
    public float CounterweightSpinAccelerationRate = 1.0f;

    // Transform to rotate for the counterweight
    [KSPField(isPersistant = false)]
    public string CounterweightTransformName = "";

    // Name of the start action
    [KSPField(isPersistant = false)]
    public string StartSpinActionName = "";

    // Name of the stop action
    [KSPField(isPersistant = false)]
    public string StopSpinActionName = "";

    // Name of the toggle action
    [KSPField(isPersistant = false)]
    public string ToggleSpinActionName = "";


    /// GUI Fields
    // Current status of deploy
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Spin Gravity")]
    public string GravityStatus = "0g";

    // GUI Events
    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Start Spin", active = true)]
    public void EventStartSpin()
    {
        StartSpin();
    }
    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Stop Spin", active = false)]
    public void EventStopSpin()
    {
        StopSpin();
    }

    /// GUI Actions
    // ACTIONS
    [KSPAction("Start Spin")]
    public void StartAction(KSPActionParam param) { EventStartSpin(); }

    [KSPAction("Stop Spin")]
    public void StopAction(KSPActionParam param) { EventStopSpin(); }

    [KSPAction("Toggle Spin")]
    public void ToggleSpinAction(KSPActionParam param)
    {
        if (Rotating)
          EventStartSpin();
        else
          EventStopSpin();
    }

    // private
    private float rotationRateGoal = 0f;
    private Transform spinTransform;
    private Transform counterweightTransform;

    // Calculates the period in s given the spin rate
    protected float Period(float spinRate)
    {
      return 360f/spinRate;
    }

    // Calculates the gravity in g
    protected float CalculateGravity(float radius, float period)
    {
      return (radius * Mathf.Pow((2f* Mathf.PI)/period, 2f)) / 9.81f;
    }

    public override void Start()
    {
      base.Start();
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        spinTransform = part.FindModelTransform(SpinTransformName);
        counterweightTransform = part.FindModelTransform(CounterweightTransformName);

        if (Rotating)
        {
          rotationRateGoal = 1.0f;
          CurrentSpinRate = SpinRate;
          CurrentCounterweightSpinRate = CounterweightSpinRate;
        } else
        {
          rotationRateGoal = 0.0f;
          CurrentSpinRate = 0f;
          CurrentCounterweightSpinRate = 0f;
        }
      }
    }

    public override void Update()
    {
      base.Update();
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        HandleUI();

      }
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        DoSpin();
      }
    }

    protected override void SetupUI()
    {
      base.SetupUI();

      Fields["GravityStatus"].guiName = Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_Field_GravityStatus_Name");
      Events["EventStartSpin"].guiName = Localizer.Format(StartSpinActionName);
      Events["EventStopSpin"].guiName = Localizer.Format(StopSpinActionName);

      Actions["StartAction"].guiName = Localizer.Format(StartSpinActionName);
      Actions["StopAction"].guiName = Localizer.Format(StopSpinActionName);
      Actions["ToggleSpinAction"].guiName = Localizer.Format(ToggleSpinActionName);
    }


    // Fired when retraction starts
    protected override void StartRetract()
    {
      base.StartRetract();
      StopSpin();
    }

    // Fired when deployment starts
    protected override void StartDeploy()
    {
      base.StartDeploy();
      StartSpin();
    }

    // Fired when retraction is complete
    protected override void FinishRetract()
    {
      base.FinishRetract();
    }

    // Fired when deployment is complete
    protected override void FinishDeploy()
    {
      base.FinishDeploy();
    }

    // Updates the UI values
    protected override void HandleUI()
    {
        base.HandleUI();
        GravityStatus = Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_Field_GravityStatus_Normal", CalculateGravity(Radius, Period(Mathf.Abs(CurrentSpinRate))).ToString("F2"));

        if (base.deployState == DeployState.Retracted || base.deployState == DeployState.Retracting || base.deployState == DeployState.Deploying)
        {
            Events["EventStartSpin"].active = false;
            Events["EventStopSpin"].active = false;
        }
        else
        {
            if (Events["EventStartSpin"].active == Rotating || Events["EventStopSpin"].active != Rotating)
            {
                Events["EventStopSpin"].active = Rotating;
                Events["EventStartSpin"].active = !Rotating;
            }
        }
    }

    // Does the spinning of the centrifuge
    void DoSpin()
    {
        CurrentSpinRate = Mathf.MoveTowards(CurrentSpinRate, rotationRateGoal*SpinRate, TimeWarp.fixedDeltaTime*SpinAccelerationRate);
        CurrentCounterweightSpinRate = Mathf.MoveTowards(CurrentCounterweightSpinRate, rotationRateGoal*CounterweightSpinRate, TimeWarp.fixedDeltaTime*CounterweightSpinAccelerationRate);

        spinTransform.Rotate(Vector3.forward * TimeWarp.fixedDeltaTime * CurrentSpinRate);
        counterweightTransform.Rotate(Vector3.forward * TimeWarp.fixedDeltaTime * CurrentCounterweightSpinRate);
    }

    /// Starts the spin
    void StartSpin()
    {
        if (base.deployState == DeployState.Retracted || base.deployState == DeployState.Retracting || base.deployState == DeployState.Deploying)
            return;

        Utils.Log("[ModuleDeployableCentrifuge]: Initiating Spin");
        rotationRateGoal = 1.0f;
        Rotating = true;
    }

    /// Starts slowing down the spin
    void StopSpin()
    {
      Utils.Log("[ModuleDeployableCentrifuge]: Stopping Spin");
      rotationRateGoal = 0.0f;
      Rotating = false;
    }
  }
}
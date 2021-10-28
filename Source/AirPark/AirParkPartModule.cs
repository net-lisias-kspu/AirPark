/*
	This file is part of Airpark /L Unleashed
	© 2018-21 Lisias T : http://lisias.net <support@lisias.net>
	© 2016-2018 Gomker
	© 2015 Smelly

	Airpark /L Unleashed is licensed as follows:

	* GPL 2.0 : https://www.gnu.org/licenses/gpl-2.0.txt

	Airpark /L Unleashed is distributed in the hope that it will be useful, but
	WITHOUT ANY WARRANTY; without even the implied warranty ofMERCHANTABILITY
	or FITNESS FOR A PARTICULAR PURPOSE.

	You should have received a copy of the GNU General Public License 2.0
	Airpark /L Unleashed . If not, see <https://www.gnu.org/licenses/>.

*/
using System;
using KSPe.Annotations;
using UnityEngine;

namespace AirPark
{
    // State controller for the toobar button
	public class ParkedState:KSPe.UI.Toolbar.State.Status<bool> { protected ParkedState(bool v):base(v) { }  public static implicit operator ParkedState(bool v) => new ParkedState(v);   public static implicit operator bool(ParkedState s) => s.v; }

    public class AirPark : PartModule
    {
		public static AirPark Instance
		{
			get
			{
				foreach (Part p in FlightGlobals.ActiveVessel.Parts) if (p.Modules.Contains<AirPark>())
					return p.Modules.GetModule<AirPark>();
				return null; // This is going to get interesting...
			}
		}

        private static readonly Vector3 ZERO = new Vector3(0f, 0f, 0f);

        #region Fields / Globals

        [KSPField(isPersistant = true, guiActive = true, guiName = "AirParked")]
        public bool Parked;
        internal ParkedState ParkedState => this.Parked;

    // static KSPFiels are pretty hackish, but it works...
        [KSPField(isPersistant = true, guiActive = true, guiName = "Auto UnPark")]
        public static bool autoPark;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Allow Suborbital Parking")]
        public static bool isSuborbitalParkAllowed;
    // hackish

        //Velocity and Postion
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3 ParkPosition;

        [KSPField(isPersistant = true, guiActive = false)]
        Vector3 ParkVelocity = ZERO;

        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3 ParkAcceleration = ZERO;

        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3 ParkAngularVelocity = ZERO;

        //Vessel State
        [KSPField(isPersistant = true, guiActive = false)]
        Vessel.Situations previousState;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool isActive = false;

        #region Debug Fields

        [KSPField(isPersistant = true)]
        public bool partDebug = true;

        [KSPField(guiActive = true, isPersistant = false, guiName = "Current Situation")]
        public string vesselSituation;

        #endregion DebugFields

        #endregion

        #region Toggles

        [KSPEvent(guiActive = true, guiName = "Toggle Park")]
        public void TogglePark_Event()
        {
            TogglePark();
        }

        [KSPEvent(guiActive = true, guiName = "Parking ON")]
        public void SetParkingOn_Event()
        {
            if (!this.Parked) this.TogglePark();
        }

        [KSPEvent(guiActive = true, guiName = "Parking OFF")]
        public void SetParkingOff_Event()
        {
            if (this.Parked) this.TogglePark();
        }

        [KSPAction("Toggle Park on/off")]
        public void TogglePart_AG(KSPActionParam param)
        {
            TogglePark();
        }

        [KSPAction("Set Parking to ON")]
        public void SetParkingOn_AG(KSPActionParam param)
        {
            if (!this.Parked) this.TogglePark();
        }

        [KSPAction("Set Parking to OFF")]
        public void SetParkingOff_AG(KSPActionParam param)
        {
            if (this.Parked) this.TogglePark();
        }

        public void TogglePark()
        {
            //if (!FlightGlobals.ActiveVessel || !(vessel.id == FlightGlobals.ActiveVessel.id)) { return; }
            //if (vessel == null || vessel == FlightGlobals.ActiveVessel) { return; }
            if (vessel == null || !vessel.isActiveVessel) { return; }

            // cannot Park in orbit or sub-orbit
            if (this.isParkingAllowed)
            {
                if (!Parked)
                {
                    isActive = true;
                    this.RefreshParkData();
                    ParkVessel();
                }
                else
                {
                    RestoreVesselState();
                }
            }
            Log.dbg("Parking for vessel {0} is now {1}", this.part.vessel.vesselName, this.Parked);
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Auto UnPark")] //auto park on will awake the vessel and set Parked = false if closer than 1.5 KM and inactive
        public void ToggleAutoPark()
        {
            autoPark = !autoPark;
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Suborbital Park")] // Allows suborbital parking or not
        public void ToggleSubOrbitalPark()
        {
            isSuborbitalParkAllowed = !isSuborbitalParkAllowed;
        }
        #endregion

        #region GameEvents
        public override void OnStart(StartState state)
        {
            this.enabled = HighLogic.LoadedSceneIsFlight && null != this.vessel;
            if (state != StartState.Editor && vessel != null)
            {
                part.force_activate();
                if (this.Parked) setVesselPosition();
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (this.Parked) this.RefreshParkData();
            base.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (this.Parked) this.setVesselStill();
        }

        [UsedImplicitly]
        private void Update()
        {
            if (null == this.vessel) return;

            #region can't Park if we're orbiting (unless parking in suborbital is allowed)
            if (!this.isParkingAllowed)
            {
                this.Parked = false;
                if (AirParkToolbar.toolbarGuiEnabled) // Prevents the pesky message from being displayed without the GUI
                    ScreenMessages.PostScreenMessage("Cannot Park While Sub-Orbital or Orbital", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            #endregion
        }

        [UsedImplicitly]
        private void FixedUpdate()
        {
            vesselSituation = vessel.situation.ToString();

            #region If we are the Inactive Vessel and AutoPark is set
            if (autoPark && !vessel.isActiveVessel)
            {
                //ParkPosition = vessel.GetWorldPos3D();
                // if we're less than 1.5km from the active vessel and Parked, then wake up
                if ((vessel.GetWorldPos3D() - FlightGlobals.ActiveVessel.GetWorldPos3D()).magnitude < 1500.0f & Parked)
                {
                    Log.dbg("AutoPark kicking in. {0} is near 1.5km from active vessel.", this.vessel.vesselName);
                    vessel.GoOffRails();
                    RestoreVesselState();
                }
                // if we're farther than 2km, auto Park if needed
                if (!this.Parked && (vessel.GetWorldPos3D() - FlightGlobals.ActiveVessel.GetWorldPos3D()).magnitude > 2000.0f)
                {
                    Log.dbg("AutoPark kicking in. {0} is far than 2km from active vessel.", this.vessel.vesselName);
                    ParkVessel();
                }
            }
            #endregion

            #region if we're not Parked, and not active and flying, then go off rails
            // I dont think it matters if we are flying when I remember previous state - gomker
            //if (!vessel.isActiveVessel & Parked==false & vessel.situation == Vessel.Situations.FLYING)
            //{
            //    vessel.GoOffRails();
            //    RestoreVesselState();
            //}
            #endregion

            if (this.Parked) this.setVesselStill();
        }

        #endregion

        #region vessel states
        private void RememberPreviousState()
        {
            if (!Parked & vessel.situation != Vessel.Situations.LANDED) //Keep from Vessel Situation from Sticking to Landed permanently
            {
                previousState = vessel.situation;
            }
        }

        private void RestoreVesselState()
        {
            if (!this.isActive) return;          // we only want to restore the state if you have parked somewhere intentionally

            vessel.situation = previousState;
            if (vessel.situation != Vessel.Situations.LANDED) { vessel.Landed = false; }
            if (Parked) { Parked = false; }

            setVesselStill();

            //Restore Velocity and Accleration
            this.vessel.IgnoreGForces(240);
            this.vessel.SetWorldVelocity(this.ParkVelocity);
            this.vessel.acceleration = this.ParkAcceleration;
            this.vessel.angularVelocity = this.ParkAngularVelocity;
            this.vessel.orbitDriver.pos = this.ParkPosition;
        }

        private void ParkVessel()
        {
            RememberPreviousState();
            setVesselStill();
            Parked = true;
        }

        private void setVesselStill()
        {
            this.vessel.IgnoreGForces(240);
            this.vessel.SetWorldVelocity(ZERO);
            this.vessel.acceleration = ZERO;
            this.vessel.angularVelocity = ZERO;
            this.vessel.geeForce = 0.0;
            this.setVesselPosition();
            vessel.situation = Vessel.Situations.LANDED;
            vessel.Landed = true;
        }
        #endregion

        #region Position
        // Inspired by Hyperedit landing functions 
        // https://github.com/Ezriilc/HyperEdit

        private Vector3d GetVesselPosition()
        {
            PQS pqs = vessel.mainBody.pqsController;
            if (null == pqs) return ZERO;

            double alt = pqs.GetSurfaceHeight(vessel.mainBody.GetRelSurfaceNVector(0, 0)) - vessel.mainBody.Radius;
            alt = Math.Max(alt, 0); // Underwater!

            return vessel.mainBody.GetRelSurfacePosition(0, 0, alt);
        }

        private void RefreshParkData()
        {
            //this.ParkPosition = this.GetVesselPosition();
            //this.ParkPosition = this.vessel.GetWorldPos3D();
            this.ParkPosition = this.vessel.transform.position;
            //this.ParkPosition = this.vessel.orbitDriver.pos;

            //we only want to remember the initial velocity, not subseqent updates by onFixedUpdate()
            this.ParkVelocity = this.vessel.GetSrfVelocity();
            this.ParkAcceleration = this.vessel.acceleration;
            this.ParkAngularVelocity = this.vessel.angularVelocity;
        }

        private void setVesselPosition()
        {
            this.vessel.IgnoreGForces(240);
            this.vessel.orbitDriver.pos = this.ParkPosition;
        }

        private bool isParkingAllowed => (this.vessel.situation != Vessel.Situations.ORBITING && (isSuborbitalParkAllowed || this.vessel.situation != Vessel.Situations.SUB_ORBITAL));

        #endregion
    }
}

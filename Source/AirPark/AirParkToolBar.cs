/*
	This file is part of Airpark /L
	© 2018-21 Lisias T : http://lisias.net <support@lisias.net>
	© 2016-2018 Gomker
	© 2015 Smelly

	Airpark /L is licensed as follows:

	* GPL 2.0 : https://www.gnu.org/licenses/gpl-2.0.txt

	Airpark /L is distributed in the hope that it will be useful, but
	WITHOUT ANY WARRANTY; without even the implied warranty ofMERCHANTABILITY
	or FITNESS FOR A PARTICULAR PURPOSE.

	You should have received a copy of the GNU General Public License 2.0
	Airpark /L. If not, see <https://www.gnu.org/licenses/>.

*/
using UnityEngine;
using KSP.UI.Screens;
using KSPe.Annotations;

using Toolbar = KSPe.UI.Toolbar;
using GUI = KSPe.UI.GUI;
using GUILayout = KSPe.UI.GUILayout;
using System.Collections.Generic;

namespace AirPark
{
    using Asset = KSPe.IO.Asset<Startup>;

	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class ToolbarController : MonoBehaviour
	{
		internal static KSPe.UI.Toolbar.Toolbar Instance => KSPe.UI.Toolbar.Controller.Instance.Get<ToolbarController>();

		[UsedImplicitly]
		private void Start()
		{
			KSPe.UI.Toolbar.Controller.Instance.Register<ToolbarController>(Version.FriendlyName);
		}
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
    class AirParkToolbar : MonoBehaviour
    {
       
        public static bool hasAddedButton => null != button;
        public static bool toolbarGuiEnabled = false;

        private static AirPark airParkInstance = null;
        private static AirPark AirParkInstance => airParkInstance ?? (airParkInstance = AirPark.Instance);

        Rect toolbarRect;
        float toolbarWidth = 280;
        float toolbarHeight = 0;
        float toolbarMargin = 6;
        float toolbarLineHeight = 20;
        float contentWidth;
        Vector2 toolbarPosition;
        Rect svRectScreenSpace;
        
        void VesselChange(Vessel v)
        {
            airParkInstance = null;
            if (!v.isActiveVessel) return;
        }

        [UsedImplicitly]
        private void Start()
        {
            toolbarPosition = new Vector2(Screen.width - toolbarWidth - 80, 50);
            toolbarRect = new Rect(toolbarPosition.x, toolbarPosition.y + 100, toolbarWidth, toolbarHeight);
            contentWidth = toolbarWidth - (2 * toolbarMargin);

            AddToolbarButton();

            GameEvents.onVesselChange.Add(this.VesselChange);
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(this.VesselChange);
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            if (toolbarGuiEnabled) //&& AirParkPM.instance)
            {
                GUI.Window(999666, toolbarRect, ToolbarWindow, "AirPark", HighLogic.Skin.window);
            }
            parkingState.State = AirParkInstance.isActive && AirPark.Parked;
        }

        void ToolbarWindow(int windowID)
        {
            float line = 0;
            line += 1.25f;

            if (!FlightGlobals.ActiveVessel) { return; }

            if (AirPark.Instance)
            {
                if (!AirPark.Parked)
                {
                    if (GUI.Button(LineRect(ref line, 1.5f), "Park Vessel", HighLogic.Skin.button))
                    {
                        if (FlightGlobals.ActiveVessel && !FlightGlobals.ActiveVessel.Landed)
                            AirPark.Instance.TogglePark();
                    }

                }
                else
                {
                    if (GUI.Button(LineRect(ref line, 2), "Un-Park", HighLogic.Skin.button))
                    {
                        AirPark.Instance.TogglePark();
                    }
                }

                line += 0.2f;
                { 
                    Rect spawnVesselRect = LineRect(ref line);
                    svRectScreenSpace = new Rect(spawnVesselRect);
                    svRectScreenSpace.x += toolbarRect.x;
                    svRectScreenSpace.y += toolbarRect.y;
                    if (!AirPark.autoPark)
                    {
                        if (GUI.Button(spawnVesselRect, "Auto-Park OFF", HighLogic.Skin.button))
                            AirPark.Instance.ToggleAutoPark();
                    }
                    else
                    {
                        if (GUI.Button(spawnVesselRect, "Auto-Park ON", HighLogic.Skin.button))
                            AirPark.Instance.ToggleAutoPark();
                    }
                }

                {
                    line += 0.2f;
                    Rect spawnVesselRect = LineRect(ref line);
                    svRectScreenSpace = new Rect(spawnVesselRect);
                    svRectScreenSpace.x += toolbarRect.x;
                    svRectScreenSpace.y += toolbarRect.y;
                    if (!AirPark.isSuborbitalParkAllowed)
                    {
                        if (GUI.Button(spawnVesselRect, "Sub Orbital Park OFF", HighLogic.Skin.button))
                            AirPark.Instance.ToggleSubOrbitalPark();
                    }
                    else
                    {
                        if (GUI.Button(spawnVesselRect, "Sub Orbital Park ON", HighLogic.Skin.button))
                            AirPark.Instance.ToggleSubOrbitalPark();
                    }
                }
            }
            else
            {
                GUIStyle centerLabelStyle = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.UpperCenter };
                GUI.Label(LineRect(ref line), "No AirPark Module Found", centerLabelStyle);
            }

            toolbarRect.height = (line * toolbarLineHeight) + (toolbarMargin * 2);
        }

        Rect LineRect(ref float currentLine, float heightFactor = 1)
        {
            Rect rect = new Rect(toolbarMargin, toolbarMargin + (currentLine * toolbarLineHeight), contentWidth, toolbarLineHeight * heightFactor);
            currentLine += heightFactor + 0.1f;
            return rect;
        }

        void LineLabel(string label, ref float line)
        {
            GUI.Label(LineRect(ref line), label, HighLogic.Skin.label);
        }

        private static Toolbar.States parkingState = null;
        private static Toolbar.Button button = null;
        void AddToolbarButton()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!hasAddedButton)
                {
                    button = Toolbar.Button.Create(this
                            , ApplicationLauncher.AppScenes.FLIGHT
                            , UI.icon.button.off_36
                            , UI.icon.button.off_24
                        )
                    ;

                    parkingState = button.States.Create<bool>(
                        new Dictionary<object, Toolbar.States.Data> {
                            { false, Toolbar.States.Data.Create(UI.icon.button.off_36, UI.icon.button.off_24) }
                            ,{ true, Toolbar.States.Data.Create(UI.icon.button.on_36, UI.icon.button.on_24) }
                        })
                    ;

                    button.Toolbar
                        .Add(Toolbar.Button.ToolbarEvents.Kind.Active,
                            new Toolbar.Button.Event(this.ShowToolbarGUI, this.HideToolbarGUI)
                        );
                    ;

                    ToolbarController.Instance.Add(button);
                }
            }
        }

        public void ShowToolbarGUI()
        {
            AirParkToolbar.toolbarGuiEnabled = true;
        }

        public void HideToolbarGUI()
        {
            AirParkToolbar.toolbarGuiEnabled = false;
        }

        public static bool MouseIsInRect(Rect rect)
        {
            return rect.Contains(MouseGUIPos());
        }

        public static Vector2 MouseGUIPos()
        {
            return new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0);
        }
    }
}

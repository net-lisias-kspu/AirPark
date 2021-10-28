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
using UnityEngine;

namespace AirPark
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    internal class Startup : MonoBehaviour
    {
        private void Start()
        {
            Log.force("Version {0}", Version.Text);

            try
            {
                //KSPe.Util.Compatibility.Check<Startup>(typeof(Version), typeof(Configuration));
                KSPe.Util.Installation.Check<Startup>(typeof(Version));
            }
            catch (KSPe.Util.InstallmentException e)
            {
                Log.error(e.ToShortMessage());
                KSPe.Common.Dialogs.ShowStopperAlertBox.Show(e);
            }
        }
    }
}

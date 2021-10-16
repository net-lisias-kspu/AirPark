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
namespace AirPark
{
	using Asset = KSPe.IO.Asset<Startup>;
	internal static class UI
	{
		internal static class icon
		{
			internal static class button
			{ 
				internal static readonly Texture2D on_36 = Asset.Texture2D.LoadFromFile("Icon", "AirParkON");
				internal static readonly Texture2D off_36 = Asset.Texture2D.LoadFromFile("Icon", "AirPark");
				internal static readonly Texture2D on_24 = Asset.Texture2D.LoadFromFile("Icon", "AirParkON_24");
				internal static readonly Texture2D off_24 = Asset.Texture2D.LoadFromFile("Icon", "AirPark_24");
			}
		}
	}
}

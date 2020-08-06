using System;
using System.Linq;

namespace Winstreak.Request.Checker
{
	public class ResponseData
	{
		private string HtmlContent { get; }
		public string Name { get; private set; }
		public BedwarsData? SoloDataInfo { get; private set; }
		public BedwarsData? DoubleDataInfo { get; private set; }
		public BedwarsData? ThreesDataInfo { get; private set; }
		public BedwarsData? FoursDataInfo { get; private set; }
		public BedwarsData? TotalDataInfo { get; private set; }

		public ResponseData(string name, string htmlContent)
		{
			HtmlContent = htmlContent;
			Name = name;
		}

		/**
         * Parses the raw HTML data. After this method is executed, all getters will be available for use.
         * @return This object.
         */
		public ResponseData Parse()
		{
			if (HtmlContent.Contains("Player does not exist!"))
			{
				SoloDataInfo = null;
				DoubleDataInfo = null;
				ThreesDataInfo = null;
				FoursDataInfo = null;
				TotalDataInfo = null;
				return this;
			}

			// only get bedwars data
			string bedwarsData = HtmlContent
				.Split(new string[] { "Bed Wars </a>", "Bed Wars  </a>"}, StringSplitOptions.RemoveEmptyEntries)[1]
				.Split("Build Battle")[0]
				.Split("</thead>")[1]
				.Split("</div>")[0];

			// clean up data
			bedwarsData = bedwarsData
				.Replace("<td style=\"border-right: 1px solid #f3f3f3\">", "")
				.Replace("<th scope=\"row\" style=\"border-right: 1px solid #f3f3f3\">", "");
			// get data for solos
			string soloData = bedwarsData
				.Split("Solo")[1]
				.Split("Doubles")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			string[] soloDataArr = soloData
				.Replace(",", "")
				.Split(new string[] {"</td>", "<td>"}, StringSplitOptions.RemoveEmptyEntries);
			soloDataArr = soloDataArr
				.Where(x => x != string.Empty)
				.ToArray();
			try
			{
				SoloDataInfo = new BedwarsData(
					int.Parse(soloDataArr[0]),
					int.Parse(soloDataArr[1]),
					int.Parse(soloDataArr[3]),
					int.Parse(soloDataArr[4]),
					int.Parse(soloDataArr[6]),
					int.Parse(soloDataArr[7]),
					int.Parse(soloDataArr[9])
				);
			}
			catch (Exception)
			{
				SoloDataInfo = null;
			}

			// get data for doubles
			string doubleData = bedwarsData
				.Split("Doubles")[1]
				.Split("3v3v3v3")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			string[] doubleDataArr = doubleData
				.Replace(",", "")
				.Split(new string[] {"</td>", "<td>"}, StringSplitOptions.RemoveEmptyEntries);
			doubleDataArr = doubleDataArr
				.Where(x => x != string.Empty)
				.ToArray();

			try
			{
				DoubleDataInfo = new BedwarsData(
					int.Parse(doubleDataArr[0]),
					int.Parse(doubleDataArr[1]),
					int.Parse(doubleDataArr[3]),
					int.Parse(doubleDataArr[4]),
					int.Parse(doubleDataArr[6]),
					int.Parse(doubleDataArr[7]),
					int.Parse(doubleDataArr[9])
				);
			}
			catch (Exception)
			{
				DoubleDataInfo = null;
			}

			// get data for 3v3v3v3
			string threeData = bedwarsData
				.Split("3v3v3v3")[1]
				.Split("4v4v4v4")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			string[] threeDataArr = threeData
				.Replace(",", "")
				.Split(new string[] {"</td>", "<td>"}, StringSplitOptions.RemoveEmptyEntries);
			threeDataArr = threeDataArr
				.Where(x => x != string.Empty)
				.ToArray();
			try
			{
				ThreesDataInfo = new BedwarsData(
					int.Parse(threeDataArr[0]),
					int.Parse(threeDataArr[1]),
					int.Parse(threeDataArr[3]),
					int.Parse(threeDataArr[4]),
					int.Parse(threeDataArr[6]),
					int.Parse(threeDataArr[7]),
					int.Parse(threeDataArr[9])
				);
			}
			catch (Exception)
			{
				ThreesDataInfo = null;
			}

			// get data for 4v4v4v4
			string fourData = bedwarsData
				.Split("4v4v4v4")[1]
				.Split("4v4")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			string[] fourDataArr = fourData
				.Replace(",", "")
				.Split(new string[] {"</td>", "<td>"}, StringSplitOptions.RemoveEmptyEntries);
			fourDataArr = fourDataArr
				.Where(x => x != string.Empty)
				.ToArray();
			try
			{
				FoursDataInfo = new BedwarsData(
					int.Parse(fourDataArr[0]),
					int.Parse(fourDataArr[1]),
					int.Parse(fourDataArr[3]),
					int.Parse(fourDataArr[4]),
					int.Parse(fourDataArr[6]),
					int.Parse(fourDataArr[7]),
					int.Parse(fourDataArr[9])
				);
			}
			catch (Exception)
			{
				FoursDataInfo = null;
			}


			if (SoloDataInfo is { } oneData && DoubleDataInfo is { } twoData && ThreesDataInfo is { } threesData &&
			    FoursDataInfo is { } foursData)
			{
				TotalDataInfo = new BedwarsData(
					oneData.Kills + twoData.Kills + threesData.Kills + foursData.Kills,
					oneData.Deaths + twoData.Deaths + threesData.Deaths + foursData.Deaths,
					oneData.FinalKills + twoData.FinalKills + threesData.FinalKills + foursData.FinalKills,
					oneData.FinalDeaths + twoData.FinalDeaths +
					threesData.FinalDeaths + foursData.FinalDeaths,
					oneData.Wins + twoData.Wins + threesData.Wins + foursData.Wins,
					oneData.Losses + twoData.Losses + threesData.Losses + foursData.Losses,
					oneData.BrokenBeds + twoData.BrokenBeds + threesData.BrokenBeds + foursData.BrokenBeds
				);
			}
			else
			{
				TotalDataInfo = null;
			}

			return this;
		}
	}
}
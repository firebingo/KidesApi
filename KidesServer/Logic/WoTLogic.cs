using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using KidesServer.Models;
using System.Net.Http.Headers;

namespace KidesServer.Logic
{
	public static class WoTLogic
	{
		private static string appId = AppConfig.config.wotAppId;
		private static Dictionary<string, string> userInfoUrls = new Dictionary<string, string>()
		{
			{ "na", "https://api.worldoftanks.com/wot/account/list/" },
			{ "eu", "https://api.worldoftanks.eu/wot/account/list/" },
			{ "ru", "https://api.worldoftanks.ru/wot/account/list/" },
			{ "kr", "https://api.worldoftanks.kr/wot/account/list/" },
			{ "asia", "https://api.worldoftanks.asia/wot/account/list/" },
		};
		private static Dictionary<string, string> userDataUrls = new Dictionary<string, string>()
		{
			{ "na", "https://api.worldoftanks.com/wot/account/info/" },
			{ "eu", "https://api.worldoftanks.eu/wot/account/info/" },
			{ "ru", "https://api.worldoftanks.ru/wot/account/info/" },
			{ "kr", "https://api.worldoftanks.kr/wot/account/info/" },
			{ "asia", "https://api.worldoftanks.asia/wot/account/info/" },
		};
		//public static string userInfoUrl = "https://api.worldoftanks.com/wot/account/list/";
		//public static string userDataUrl = "https://api.worldoftanks.com/wot/account/info/";

		public static async Task<WotBasicUser> callInfoAPI(string searchString, string region)
		{
			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(userInfoUrls[region]);

			// Add an Accept header for JSON format.
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			// List data response.
			HttpResponseMessage response = client.GetAsync($"?application_id={appId}&search={searchString}").Result;  // Blocking call!
			if (response.IsSuccessStatusCode)
			{
				// Parse the response body. Blocking!
				try
				{
					var dataObjects = response.Content.ReadAsAsync<WotBasicUser>().Result;
					if (dataObjects != null)
					{
						return dataObjects;
					}
					else
					{
						return null;
					}
				}
				catch (Exception e)
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		public static async Task<WotUserInfo> callDataAPI(string accoundId, string accessToken, string region)
		{
			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(userDataUrls[region]);

			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			HttpResponseMessage response = client.GetAsync($"?application_id={appId}&account_id={accoundId}{(accessToken != null ? $"&access_token={accessToken}" : "")}").Result;
			if (response.IsSuccessStatusCode)
			{
				try
				{
					var dataObjects =  response.Content.ReadAsAsync<WotUserInfo>().Result;
					if (dataObjects != null)
						return dataObjects;
					else
						return null;
				}
				catch (Exception e)
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}
	}
}
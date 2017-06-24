using KidesServer.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace KidesServer
{
	public static class AppConfig
	{
		public static string folderLocation = String.Empty;
		private static ConfigModel _config;

		static AppConfig()
		{
			try
			{
				folderLocation = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
			}
			catch(Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static ConfigModel config
		{
			get
			{
				try
				{
					if (_config == null)
						_config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText($"{folderLocation}\\Config.json"));
					return _config;
				}
				catch (Exception e)
				{
					ErrorLog.writeLog(e.Message);
					return null;
				}
			}
		}
	}
}
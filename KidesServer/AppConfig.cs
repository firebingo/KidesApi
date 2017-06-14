using KidesServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace KidesServer
{
	public static class AppConfig
	{
		public static string folderLocation = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
		private static ConfigModel _config;
		public static ConfigModel config
		{
			get
			{
				if(_config == null)
					_config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText($"{folderLocation}\\Config.json"));
				return _config;
			}
		}
	}
}
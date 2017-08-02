
namespace KidesServer.Models
{
	public class BaseResult
	{
		public bool success;
		public string message;
	}

	public class ConfigModel
	{
		public string wotAppId;
		public string iisLogLocation;
		public string baseMusicUrl;
		public DBConfigModel DBConfig;
		public string botId;
	}

	public class DBConfigModel
	{
		public string userName;
		public string password;
		public string address;
		public string port;
		public string schemaName;
		private string _connectionString;
		public string connectionString
		{
			get
			{
				if(_connectionString == null)
					_connectionString = $"server={address};uid={userName};pwd={password};database={schemaName};charset=utf8mb4;Allow User Variables=True";
				return _connectionString;
			}
		}
	}
}
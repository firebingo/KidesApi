using MySql.Data.MySqlClient;
using System;
using System.Data;
using KidesServer.Models;

namespace KidesServer.DataBase
{
	public static class DataLayerShortcut
	{
		public static bool schemaExists { get; private set; } = true;

		public static BaseResult testConnection()
		{
			var result = new BaseResult();
			try
			{
				var connection = new MySql.Data.MySqlClient.MySqlConnection(AppConfig.config.DBConfig.connectionString);
				connection.Open();
				connection.Close();
				connection.Dispose();
			}
			catch (MySqlException e)
			{
				if (e.InnerException.Message.ToUpperInvariant().Contains("UNKNOWN DATABASE"))
					schemaExists = false;

				result.success = false;
				result.message = e.Message;
				return result;
			}
			result.success = true;
			return result;
		}

		public static void ExecuteReader<T>(Action<IDataReader, T> workFunction, T otherdata, string query, params MySqlParameter[] parameters)
		{
			var connection = new MySqlConnection(AppConfig.config.DBConfig.connectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				MySqlDataReader reader = null;
				if (parameters != null)
					DataHelper.addParams(ref cmd, parameters);

				reader = cmd.ExecuteReader();

				while (reader.Read())
					workFunction(reader, otherdata);

				reader.Close();
				cmd.Dispose();
			}
			connection.Close();
			connection.Dispose();
		}

		public static string ExecuteNonQuery(string query, params MySqlParameter[] parameters)
		{
			var connection = new MySqlConnection(AppConfig.config.DBConfig.connectionString);
			try
			{
				connection.Open();
				if (connection.State == ConnectionState.Open)
				{
					MySqlCommand cmd = new MySqlCommand(query, connection);
					cmd.CommandType = CommandType.Text;
					if (parameters != null)
						DataHelper.addParams(ref cmd, parameters);

					cmd.ExecuteNonQuery();
					cmd.Dispose();
				}
				connection.Close();
				connection.Dispose();
				return "";
			}
			catch (Exception e)
			{
				connection.Close();
				connection.Dispose();
				return $"Exception: {e.Message}, Query: {query}";
			}
		}

		public static void ExecuteSpecialNonQuery(string query, string connection, params MySqlParameter[] parameters)
		{
			var conn = new MySqlConnection(connection);
			conn.Open();
			MySqlCommand cmd = new MySqlCommand(query, conn);
			cmd.CommandType = CommandType.Text;
			if (parameters != null)
				DataHelper.addParams(ref cmd, parameters);

			cmd.ExecuteNonQuery();
			cmd.Dispose();
			conn.Close();
			conn.Dispose();
		}

		public static int? ExecuteScalar(string query, params MySqlParameter[] parameters)
		{
			int? result = null;
			var connection = new MySqlConnection(AppConfig.config.DBConfig.connectionString);
			connection.Open();
			if (connection.State == ConnectionState.Open)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.CommandType = CommandType.Text;
				if (parameters != null)
					DataHelper.addParams(ref cmd, parameters);

				object scalar = cmd.ExecuteScalar();
				try
				{
					result = Convert.ToInt32(scalar);
				}
				catch
				{
					cmd.Dispose();
					connection.Close();
					connection.Dispose();
					return result;
				}
				cmd.Dispose();
			}
			connection.Close();
			connection.Dispose();
			return result;
		}
	}
}

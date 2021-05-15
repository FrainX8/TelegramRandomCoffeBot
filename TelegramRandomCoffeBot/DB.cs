using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramRandomCoffeBot.DB
{
    static class DB
    {
        public static string ConnectionString { get; } = "Server=127.0.0.1;Port= 5432;Username=hackathon;Password=dbuser;Database=hackathon";
        public static NpgsqlConnection Connection { get; private set; }

        static DB()
        {
            Connection = new NpgsqlConnection(ConnectionString);
        }

        private static NpgsqlCommand GetCommand(string commandText)
        {
            var con = new NpgsqlConnection(ConnectionString);
            con.Open();

            var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = commandText;
            return cmd;
        }

        public static void ExecuteNonQuery(string commandText)
        {
            var cmd = GetCommand(commandText);
            cmd.ExecuteNonQuery();
            Connection.Close();
        }

        public static NpgsqlDataReader GetReader(string commandText)
        {
            var cmd = GetCommand(commandText);
            return cmd.ExecuteReader();
        }

        public static string GetScalar(string commandText)
        {
            var cmd = GetCommand(commandText);
            string result = cmd.ExecuteScalar().ToString();
            Connection.Close();
            return result;
        }
    }
}

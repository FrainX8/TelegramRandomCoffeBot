using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramRandomCoffeBot.DB
{
    static class DBTools
    {
        public static void CheckConnection()
        {
            var version = DB.GetScalar("Select version()");
            Console.WriteLine($"PostgreSQL version: {version}");
            DB.Connection.Close();
        }
    }
}

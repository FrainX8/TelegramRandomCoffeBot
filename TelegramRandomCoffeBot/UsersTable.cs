using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramRandomCoffeBot.DB
{
    public static class UsersTable
    {
        public static bool IsUserRegistered(int id)
        {
            var result = DB.GetScalar($"select exists(select 1 from users where user_id={id})");
            return bool.Parse(result);
        }

        public static void AddUser(UserData user)
        {
            DB.ExecuteNonQuery($"INSERT INTO users(user_id, otdel_id, name, age, chat_id) VALUES ({user.ID}, {user.OtdelID}, '{user.Name}', {user.Age}, {user.ChatID})");
        }

        public static UserData GetRandomUser(int userNotToInclude)
        {
            var reader = DB.GetReader($"SELECT user_id FROM users WHERE user_id != {userNotToInclude}");

            var idList = new List<int>();

            while(reader.Read())
            {
                idList.Add(reader.GetInt32(0));
            }

            DB.Connection.Close();

            var random = new Random();
            
            var randomID = idList[random.Next(idList.Count)];

            reader = DB.GetReader($"Select * FROM users where user_id = {randomID}");

            reader.Read();

            var selectedUser = new UserData()
            {
                ID = randomID,
                Name = reader.GetString(2),
                Age = reader.GetInt32(3),
                OtdelID = reader.GetInt32(1),
                ChatID = reader.GetInt32(4)
            };

            DB.Connection.Close();

            return selectedUser;
        }
    }
}

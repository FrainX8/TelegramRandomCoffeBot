using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramRandomCoffeBot.DB
{
    class OtdelsTable
    {
        public static OtdelData[] GetAllOtdels()
        {
            var reader = DB.GetReader("Select * FROM otdels");
            var result = new List<OtdelData>();
            while(reader.Read())
            {
                result.Add(new OtdelData() { ID = reader.GetInt32(0), Name = reader.GetString(1)});
            }

            return result.ToArray();
        }
    }
}

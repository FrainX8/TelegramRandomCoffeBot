using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramRandomCoffeBot.DB;

namespace TelegramRandomCoffeBot
{
    public static class Program
    {
        public static async Task Main()
        {
            DBTools.CheckConnection();
            var bot = new CoffeeBot();
            await bot.StartBot();
        }

        
    }
}

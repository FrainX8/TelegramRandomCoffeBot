using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    class CoffeeBot
    {
        private TelegramBotClient Bot;
        private UserData userData;
        private UserData foundedUserToTalk;
        private Meetings meeting;
        private Func<Message, Task> onGetAnswer;
        private Func<CallbackQueryEventArgs, Task> onQueryAnswer;
        public async Task StartBotExample()
        {
            Bot = new TelegramBotClient(Configuration.BotToken);
            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            Bot.OnMessage += BotOnMessageReceivedEx;
            Bot.OnMessageEdited += BotOnMessageReceivedEx;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceivedEx;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();
            Bot.StopReceiving();
        }

        public async Task StartBot()
        {
            Bot = new TelegramBotClient(Configuration.BotToken);

            onQueryAnswer = OnOtdelQueryReceived;
            onGetAnswer = OnDefautAnswer;

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnReceiveError += BotOnReceiveError;
            

            Bot.StartReceiving(Array.Empty<UpdateType>());

            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();

            Console.WriteLine($"Stopped listening for @{me.Username}");

            Bot.StopReceiving();
        }
        private async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await onQueryAnswer(callbackQueryEventArgs);
        }
        private async void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message;
            if (message == null || message.Type != MessageType.Text)
                return;

            await onGetAnswer(message);
        }

        #region Answers
        private async Task OnDefautAnswer(Message message)
        {
            var text = message.Text.Split(' ').First();
            if (text == "/SignUp")
            {
                await RegisterUser(message);
            }
            else if(text == "/FindSomeone")
            {
                await FindSomeone(message);
            }
            else
            {
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Использование: \n" +
                    "Зарегистрироваться - /SignUp\n" +
                    "Найти собеседника - /FindSomeone\n" +
                    "Просмотреть встречи - /ShowMeetings");
            }
        }

        private async Task FindSomeone(Message message)
        {
            foundedUserToTalk = UsersTable.GetRandomUser(message.From.Id);
            await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Как насчет {foundedUserToTalk.Name}? Данный пользователь работает в {foundedUserToTalk.OtdelID} отделе, возраст {foundedUserToTalk.Age}.\n" +
                    $"/yes\n" +
                    $"/no\n");
            onGetAnswer = OnFoundedUserAnswer;
        }

        private async Task OnFoundedUserAnswer(Message message)
        {
             
            await OnYesNoAnswer(message, async () => 
            {
                await SelectTimeAndPlaceForTalk(message);
            }, async () => 
            {
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Найти нового собеседника?\n" +
                    $"/yes\n" +
                    $"/no\n");
                onGetAnswer = OnRejectFoundedUser;

            }
            );


        }

        private async Task SelectTimeAndPlaceForTalk(Message message)
        {
            await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Выберете место");
            onGetAnswer = OnPlacePicked;

        }

        private async Task OnPlacePicked(Message message)
        {
            meeting = new Meetings() { Place = message.Text, FirstUserID = message.From.Id, SecondUserID = foundedUserToTalk.ID};
            await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Выберете время в формате 24-05 16:00");
            onGetAnswer = OnTimePicked;

        }

        private async Task OnTimePicked(Message message)
        {
            if (DateTime.TryParseExact(message.Text, "dd-MM HH:mm", new CultureInfo("ru-Ru"), DateTimeStyles.AdjustToUniversal, out DateTime time))
            {
                meeting.MeetingTime = time;
                meeting.MeetingStateID = MeetingState.created;
                await WriteMeetingToAnotherUser();
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Запрос на встречу отправлен!");
                onGetAnswer = OnDefautAnswer;
            }
            else
            {
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Неправильно выбрано время!");
                onGetAnswer = OnDefautAnswer;
            }
        }

        private async Task WriteMeetingToAnotherUser()
        {

            await Bot.SendTextMessageAsync(
                    chatId: foundedUserToTalk.ChatID,
                    text: $"Вам отправлен запрос на встречу от {meeting.FirstUserID}! Он хочет встретится с вами на {meeting.Place}, в {meeting.MeetingTime}.\n" +
                    $"/yes\n" +
                    $"/no\n");
                    //$"Изменить время /changeTime" +
                    //$"Изменить место /changePlace");
            onGetAnswer = OnAnswerAboutMeeting;
        }

        private async Task OnAnswerAboutMeeting(Message message)
        {
            if (message.Text == "/yes")
            {
                meeting.MeetingStateID = MeetingState.scheduled;
                onGetAnswer = OnDefautAnswer;
                await Bot.SendTextMessageAsync(
                    chatId: userData.ChatID,
                    text: $"Встреча с {foundedUserToTalk.Name} подтверждена!");
            }
            else if (message.Text == "/no")
            {
                meeting.MeetingStateID = MeetingState.canceled;
                onGetAnswer = OnDefautAnswer;

                await Bot.SendTextMessageAsync(
                    chatId: userData.ChatID,
                    text: $"Встреча с {foundedUserToTalk.Name} отклонена.");

            }
            //else if(message.Text == "/changeTime")
            //{

            //}
            //else if (message.Text == "/changePlace")
            //{

            //}
        }

        private async Task OnRejectFoundedUser(Message message)
        {
            OnYesNoAnswer(message, async () =>
            {
                await FindSomeone(message);
            },
            async () =>
            {
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Поиск закончен.\n");
                onGetAnswer = OnDefautAnswer;
            }
            );
        }

        private async Task OnYesNoAnswer(Message message, Func<Task> doOnYes, Func<Task> doOnNo)
        {
            if (message.Text == "/yes")
            {
                await (doOnYes());
            }
            else if (message.Text == "/no")
            {
                await doOnNo();
            }
        }


        private async Task OnNameAnswer(Message message)
        {
            userData.Name = message.Text;

            await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Введите ваш возраст");

            onGetAnswer = OnAgeAnswer;
        }

        private async Task OnAgeAnswer(Message message)
        {
            userData.Age = Int32.Parse(message.Text);

            var otdels = OtdelsTable.GetAllOtdels();
            var buttons = new InlineKeyboardButton[otdels.Length][];
            for (int i = 0; i < buttons.GetLength(0); i++)
            {
                buttons[i] = new InlineKeyboardButton[1];
                buttons[i][0] = InlineKeyboardButton.WithCallbackData(otdels[i].Name, otdels[i].ID.ToString());
            }

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);
            
            await Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Выберите отдел",
                replyMarkup: inlineKeyboard
            );

            onGetAnswer = OnOtdelAnswer;
            onQueryAnswer = OnOtdelQueryReceived;
        }

        private async Task OnOtdelQueryReceived(CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            if(userData != null)
            {
                userData.OtdelID = Int32.Parse(callbackQuery.Data);

                UsersTable.AddUser(userData);

                await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Вы зарегистрированы!"
                );
            }

            onGetAnswer = OnDefautAnswer;

        }



        private async Task OnOtdelAnswer(Message message)
        {
            await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Выберите ответ!");
        }

        private async Task OnFreeTimeAnswer(Message message)
        {
            userData.FreeTime = message.Text;

            await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Вы зарегистрированы!");

            onGetAnswer = OnDefautAnswer;

        }
        #endregion

        private async Task RegisterUser(Message usersMessage)
        {
            if (CheckIfUserRegistered(usersMessage.From))
            {
                await Bot.SendTextMessageAsync(
                    chatId: usersMessage.Chat.Id,
                    text: "Вы уже зарегистрированы!");
            }
            else
            {
                userData = new UserData();
                userData.ID = usersMessage.From.Id;
                userData.ChatID = usersMessage.Chat.Id;
                await Bot.SendTextMessageAsync(
                    chatId: usersMessage.Chat.Id,
                    text: "Введите ваше имя");
                onGetAnswer = OnNameAnswer;
            }
        }

        

        private bool CheckIfUserRegistered(User user)
        {
            var id = user.Id;
            return UsersTable.IsUserRegistered(id);
        }

        #region Examples
        private async void BotOnMessageReceivedEx(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text)
                return;

            switch (message.Text.Split(' ').First())
            {
                // Send inline keyboard
                case "/inline":
                    await SendInlineKeyboard(message);
                    break;

                // send custom keyboard
                case "/keyboard":
                    await SendReplyKeyboard(message);
                    break;

                // send a photo
                case "/photo":
                    await SendDocument(message);
                    break;

                // request location or contact
                case "/request":
                    await RequestContactAndLocation(message);
                    break;

                default:
                    await Usage(message);
                    break;
            }

            // Send inline keyboard
            // You can process responses in BotOnCallbackQueryReceived handler
            async Task SendInlineKeyboard(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                // Simulate longer running task
                await Task.Delay(500);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: inlineKeyboard
                );
            }

            async Task SendReplyKeyboard(Message message)
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new KeyboardButton[][]
                    {
                        new KeyboardButton[] { "5.1", "1.2" },
                        new KeyboardButton[] { "6.1", "2.2" },
                    },
                    resizeKeyboard: true
                );

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup

                );
            }

            async Task SendDocument(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                const string filePath = @"Files/tux.png";
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: new InputOnlineFile(fileStream, fileName),
                    caption: "Nice Picture"
                );
            }

            async Task RequestContactAndLocation(Message message)
            {
                var RequestReplyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Who or Where are you?",
                    replyMarkup: RequestReplyKeyboard
                );
            }

            async Task Usage(Message message)
            {
                const string usage = "Usage:\n" +
                                        "/inline   - send inline keyboard\n" +
                                        "/keyboard - send custom keyboard\n" +
                                        "/photo    - send a photo\n" +
                                        "/request  - request location or contact";
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
        }

        // Process Inline Keyboard callback data
        private async void BotOnCallbackQueryReceivedEx(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}"
            );

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Received {callbackQuery.Data}"
            );
        }

        #region Inline Mode

        private async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await Bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        #endregion
        #endregion

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}

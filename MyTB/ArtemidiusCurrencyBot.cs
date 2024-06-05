using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;

namespace MyTB
{
    public class ArtemidiusCurrencyBot
    {
        private readonly TelegramBotClient botClient = new TelegramBotClient("7456852539:AAErm-uIpImJTrrn6R5uyF0oj_nfEAs58es");
        private readonly CancellationToken cancellationToken = new CancellationToken();
        private readonly ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        private readonly HttpClient httpClient = new HttpClient();

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Bot {botMe.Username} started working");
            Console.ReadKey();
        }

        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Error in telegram bot API:\n {apiRequestException.ErrorCode}\n {apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
        }

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Натисніть на команду /keybord для " +
                    "відкриття меню або на /information для додаткової інформації про бота");
                return;
            }
            
            else if (message.Text == "/keybord")
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new
                (
                    new[]
                    {
                        new KeyboardButton[] { "назва валюти по VALCODE", "Видалити валюту" },
                        new KeyboardButton[] { "курс валют за датою", "прогноз валют" },
                        new KeyboardButton[] { "курс валют на сьогодні", "Записати валюту" },
                        new KeyboardButton[] { "Що таке VALCODE?" }
                    }
                )
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Оберіть потріюну кнопку:", replyMarkup: replyKeyboardMarkup);
                return;
            }

            else if (message.Text == "/information")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вітаю в боті!!! ");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Щож вміє цей бот ");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Цей бот може надати вам інформацію про курс будь-якої валюти в будь який момент часу ");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Також ви можете отримати прогноз курсу будь якої валюти на майбутьне ");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Окрім цього, якщо ви маєте VALCODE валюти та незнаєте назву, ви можете дізнатися її через VALCODE ");
                await botClient.SendTextMessageAsync(message.Chat.Id, "УВАГА Цей прогноз не має 100% точності, тож не слід використовувати його для валютних махінацій" +
                    ". А також розробник не несе відповідальність за те, що курс пішов не в ту сторону!!!");
               
            }
            else if (message.Text == "Що таке VALCODE?")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "VALCODE - це формат запису валюти наприклад як USD (долар США)");
            }
            else if (message.Text == "курс валют за датою")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "введіть дату і код валюти у форматі YYYY-MM-DD|VALCODE (дату вводити без -)");
            }
            else if (message.Text.Contains("|"))
            {
                var parts = message.Text.Split('|');
                if (parts.Length == 2)
                {
                    var date = parts[0];
                    var valcode = parts[1];
                    var rate = await GetCurrencyRate(date, valcode);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Курс {valcode} на {date} - {rate}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильний формат. Використовуйте формат YYYY-MM-DD|VALCODE");
                }
            }
            else if (message.Text == "курс валют на сьогодні")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "введіть код валюти у форматі ,VALCODE");
            }
            else if (message.Text.Contains(","))
            {
                var parts = message.Text.Split(',');
                if (parts.Length == 2)
                {
                    
                    var valcode = parts[1];
                    var rate = await GetCurrencyRate2(valcode);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Курс {valcode} на сьогодні - {rate}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильний формат. Використовуйте формат ,VALCODE");
                }
            }
            else if (message.Text == "прогноз валют")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "введіть код валюти у форматі .VALCODE прогноз якої хочете дізнатися");
            }
            else if (message.Text.Contains("."))
            {
                var parts = message.Text.Split('.');
                if (parts.Length == 2)
                {
                    
                    var valcode = parts[1];
                    var s = await GetCurrencyRate(valcode);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $" {s}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильний формат. Використовуйте формат .VALCODE");
                }
            }
            else if (message.Text == "НАТИСНІТЬ НА МЕНЕ")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Тут може бути ваша реулама)");
            }
            else if (message.Text == "назва валюти по VALCODE")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "введіть код валюти у форматі -VALCODE назву якої хочете дізнатися");
            }
            
            else if (message.Text.Contains("-"))
            {
                var parts = message.Text.Split('-');
                if (parts.Length == 2)
                {
                    var valcode = parts[1].Trim(); 
                    Console.WriteLine($"Received valcode: {valcode}"); 
                    var txt = await /*Get*/GetCurrencyName(valcode);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Назва валюти {valcode} - {txt}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильний формат. Використовуйте формат -VALCODE");
                }
            }
            else if (message.Text == "Записати валюту")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "введіть код валюти у форматі (VALCODE ");
            }
            else if (message.Text.Contains("("))
            {
                var parts = message.Text.Split('(');
                if (parts.Length == 2)
                {
                    var valcode = parts[1].Trim();
                    Console.WriteLine($"Received valcode: {valcode}");
                    var txt = await /*Get*/PostFavoriteName(valcode);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Дія - {txt}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильний формат. Використовуйте формат -VALCODE");
                }
            }
            else if (message.Text == "Видалити валюту")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "введіть код валюти у форматі )VALCODE ");
            }
            else if (message.Text.Contains(")"))
            {
                var parts = message.Text.Split(')');
                if (parts.Length == 2)
                {
                    var valcode = parts[1].Trim();
                    Console.WriteLine($"Received valcode: {valcode}");
                    var txt = await DeleteFavoriteName(valcode);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Дія - {txt}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильний формат. Використовуйте формат -VALCODE");
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Хм, тут щось не так. Нажаль ми поки неможемо вам допомогти з цією проблемою");
            }
            
        }

        private async Task<string> GetCurrencyRate(string date, string valcode)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5229/api/DateCourse?Date={date}&Valcode={valcode}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return "Error fetching rates";
            }
        }
        private async Task<string> GetCurrencyRate2(string valcode)
        {
            string date = DateTime.UtcNow.ToString("yyyyMMdd");
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5229/api/DateCourse?Date={date}&Valcode={valcode}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return "Error fetching rates";
            }
        }

        
        private async Task<string> GetCurrencyName(string valcode)
        {
            try
            {
                var requestData = new { Valcode = valcode };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5229/api/Name?Valcode={valcode}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return "Error fetching rates";
            }
        }


        private async Task<string> GetCurrencyRate(string valcode)
        {
            try
            {
                string date = DateTime.UtcNow.ToString("yyyyMMdd");
                HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5229/api/Forecast?Date={date}&Valcode={valcode}");
                Console.WriteLine($"Response status code: {response.StatusCode}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return "Error fetching rates";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                return "Error fetching rates";
            }
        }

        private async Task<string> PostFavoriteName(string valcode)
        {
            try
            {
                var requestData = new { Valcode = valcode };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync("http://localhost:5229/api/Name", content);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return "Error fetching rates";
            }
        }

        private async Task<string> DeleteFavoriteName(string valcode)
        {
            try
            {
                HttpResponseMessage response = await httpClient.DeleteAsync($"http://localhost:5229/api/Name/{valcode}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return "Error deleting currency";
            }
        }

    }
}



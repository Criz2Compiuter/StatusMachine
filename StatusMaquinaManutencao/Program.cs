using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotExample
{
    class Program
    {
        private static TelegramBotClient botClient;
        private static Dictionary<long, string> machineStatus = new Dictionary<long, string>();

        static void Main()
        {
            botClient = new TelegramBotClient("6349800967:AAGCqfz7BVIAI-nTtg38K06h6k0EPzNE9GI");
            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();
            Console.ReadLine();
            botClient.StopReceiving();
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Type == MessageType.Text)
            {
                var chatId = e.Message.Chat.Id;
                var messageText = e.Message.Text;

                if (messageText == "/Comecar" || messageText == "/c")
                {
                    // Criação dos grupos de nomes de máquinas
                    var machineGroups = new List<List<string>>
                    {
                        new List<string> {"HC 073", "HC 077", "HC 070", "HC 065"},
                        new List<string> {"DC 415", "SC 261", "SC 263"},
                        new List<string> {"RC 764", "LC 562", "LC 583"},
                        new List<string> {"LC 579", "LC 577", "LC 574", "LC 564"}
                    };

                    // Criação dos botões para cada grupo
                    var keyboardRows = new List<List<KeyboardButton>>();
                    foreach (var group in machineGroups)
                    {
                        var row = new List<KeyboardButton>();
                        foreach (var machineName in group)
                        {
                            row.Add(new KeyboardButton(machineName));
                        }
                        keyboardRows.Add(row);
                    }

                    // Criação do teclado personalizado com os botões
                    var keyboard = new ReplyKeyboardMarkup(keyboardRows, resizeKeyboard: true, oneTimeKeyboard: true);

                    // Envia a mensagem com o teclado personalizado
                    await botClient.SendTextMessageAsync(chatId, "Selecione uma máquina:", replyMarkup: keyboard);
                }
                else if (machineStatus.ContainsKey(chatId))
                {
                    string machineName = machineStatus[chatId];

                    // Separar as informações usando o separador "*"
                    string[] parts = messageText.Split(new string[] { " * " }, StringSplitOptions.None);

                    if (parts.Length >= 2)
                    {
                        string status = parts[0];
                        string additionalInfo = parts[1];

                        machineStatus.Remove(chatId);

                        string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        string message = $"Maq.: {machineName}\nStatus: {status}\nDescricao: {additionalInfo}\nTime: {currentTime}";

                        await botClient.SendTextMessageAsync(chatId, message, replyToMessageId: e.Message.MessageId);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Status falha: ");
                    }
                }
                else if (IsValidMachineName(messageText))
                {
                    machineStatus[chatId] = messageText;

                    await botClient.SendTextMessageAsync(chatId, "Status falha: ");
                }
                else if (messageText.Contains(" * "))
                {
                    // Separar as informações usando o separador "*"
                    string[] parts = messageText.Split(new string[] { " * " }, StringSplitOptions.None);

                    if (parts.Length >= 3)
                    {
                        string machineName = parts[0];
                        string status = parts[1];
                        string additionalInfo = parts[2];

                        string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        string message = $"Maq.: {machineName}\nStatus: {status}\nDescricao: {additionalInfo}\nTime: {currentTime}";

                        await botClient.SendTextMessageAsync(chatId, message, replyToMessageId: e.Message.MessageId);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Formato inválido. Por favor, informe os parâmetros no formato 'Nome da Máquina * Status * Descrição'.");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Por favor, selecione uma máquina válida ou informe os parâmetros no formato 'Nome da Máquina * Status * Descrição'.");
                }
            }
        }

        private static bool IsValidMachineName(string name)
        {
            var validMachineNames = new List<string>
            {
                "HC 073", "HC 077", "HC 070", "HC 065",
                "DC 415", "SC 261", "SC 263",
                "RC 764", "LC 562", "LC 583",
                "LC 579", "LC 577", "LC 574", "LC 564"
            };

            return validMachineNames.Contains(name);
        }
    }
}

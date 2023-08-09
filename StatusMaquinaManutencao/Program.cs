﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotExample {
    class Program {
        private static TelegramBotClient botClient;
        private static Dictionary<long, string> machineStatus = new Dictionary<long, string>();

        static void Main() {
            botClient = new TelegramBotClient("6443939914:AAG0BMQ0t_svMoqKqeAALnYLWD0b1OrvnxQ");
            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();
            ScheduleDailyTask();
            PerformDailyTask(null);
            Console.ReadLine();
            botClient.StopReceiving();
        }

        private static void SendBackupByEmail(string filePath, List<string> recipientAddresses) {
            try {
                var fromAddress = new MailAddress("cris.ferreirak10@gmail.com", "Cristian Ferreira");

                string subject = "backup do dia " + DateTime.Now.ToString("dd-MM-yyyy");
                string body = "Este é o backup diário dos dados.\nPor favor, verifique o anexo para mais detalhes.";

                var smtpClient = new SmtpClient {
                    Host = "smtp.gmail.com", // Servidor SMTP do Thunderbird
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, "agij poau pkhd nceb")
                };

                using (var message = new MailMessage { From = fromAddress, Subject = subject, Body = body }) {
                    foreach (var recipientAddress in recipientAddresses) {
                        message.To.Add(new MailAddress(recipientAddress));
                    }

                    var attachment = new Attachment(filePath);
                    message.Attachments.Add(attachment);

                    smtpClient.Send(message);
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
            }
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e) {
            var chatId = e.Message.Chat.Id;
            var messageText = e.Message.Text;
            if (e.Message.Type == MessageType.Text) {
                if (messageText == "/Comecar") {
                    var machineGroups = new List<List<string>>
                    {
                        new List<string> {"HC 073", "HC 077", "HC 070", "HC 065"},
                        new List<string> {"DC 415", "SC 261", "SC 263"},
                        new List<string> {"RC 764", "LC 562", "LC 583"},
                        new List<string> {"LC 579", "LC 577", "LC 574", "LC 564"}
                    };

                    var keyboardRows = new List<List<KeyboardButton>>();
                    foreach (var group in machineGroups) {
                        var row = new List<KeyboardButton>();
                        foreach (var machineName in group) {
                            row.Add(new KeyboardButton(machineName));
                        }
                        keyboardRows.Add(row);
                    }

                    var keyboard = new ReplyKeyboardMarkup(keyboardRows, resizeKeyboard: true, oneTimeKeyboard: true);

                    await botClient.SendTextMessageAsync(chatId, "Selecione uma máquina:", replyMarkup: keyboard);
                }
                else if (machineStatus.ContainsKey(chatId)) {
                    string machineName = machineStatus[chatId];

                    string[] parts = messageText.Split(new string[] { " * " }, StringSplitOptions.None);

                    if (parts.Length >= 2) {
                        string status = parts[0];
                        string additionalInfo = parts[1];

                        machineStatus.Remove(chatId);

                        string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        string message = $"Maq.: {machineName}\nStatus: {status}\nDescricao: {additionalInfo}\nTime: {currentTime} \n  ---------------";
                        LogMessage($"{message}");

                        await botClient.SendTextMessageAsync(chatId, message, replyToMessageId: e.Message.MessageId);
                    }
                    else {
                        await botClient.SendTextMessageAsync(chatId, "Status falha: ");
                    }
                }
                else if (IsValidMachineName(messageText)) {
                    machineStatus[chatId] = messageText;

                    await botClient.SendTextMessageAsync(chatId, "Status falha: ");
                }
                else {
                    await botClient.SendTextMessageAsync(chatId, "Por favor, selecione uma máquina válida.");
                }
            }
        }

        private static bool IsValidMachineName(string name) {
            var validMachineNames = new List<string>
            {
                "HC 073", "HC 077", "HC 070", "HC 065",
                "DC 415", "SC 261", "SC 263",
                "RC 764", "LC 562", "LC 583",
                "LC 579", "LC 577", "LC 574", "LC 564"
            };

            return validMachineNames.Contains(name);
        }


        private static void PerformDailyTask(object state) {
            var backupFolder = "C:\\Users\\Cristian\\Gmail\\Backup";
            var backupFilename = $"Backup_{DateTime.Now:yyyyMMdd}.txt";

            var backupData = new List<string>();

            // Caminho completo para o arquivo de backup
            string backupFilePath = Path.Combine(backupFolder, backupFilename);

            if (File.Exists(backupFilePath)) {
                backupData.AddRange(File.ReadAllLines(backupFilePath));
            }
            else {
                Console.WriteLine($"Arquivo de backup '{backupFilename}' não encontrado.");
            }
            // Ler o conteúdo do arquivo de log e adicionar ao backupData
            var logFolderPath = "C:\\Users\\Cristian\\Gmail\\Log";
            var logFileName = $"Log_{DateTime.Now:yyyyMMdd}.txt";
            var logFilePath = Path.Combine(logFolderPath, logFileName);

            if (File.Exists(logFilePath)) {
                backupData.AddRange(File.ReadAllLines(logFilePath));
            }

            foreach (var entry in machineStatus) {
                backupData.Add(entry.Value);
            }

            File.WriteAllLines(Path.Combine(backupFolder, backupFilename), backupData);

            List<string> recipients = new List<string>
            {
                "almoxarifadosistema@gmail.com"
            };

            SendBackupByEmail(Path.Combine(backupFolder, backupFilename), recipients);

            Console.WriteLine($"Backup e e-mail enviados às {DateTime.Now}");
        }

        private static void ScheduleDailyTask() {
            var now = DateTime.Now;
            var timeUntilNextMinute = TimeSpan.FromMinutes(1);

            var timer = new Timer(PerformDailyTask, null, timeUntilNextMinute, TimeSpan.FromMinutes(1));
        }

        private static void LogMessage(string message) {
            var logFolderPath = "C:\\Users\\Cristian\\Gmail\\Log";
            var logFileName = $"Log_{DateTime.Now:yyyyMMdd}.txt";
            var logFilePath = Path.Combine(logFolderPath, logFileName);

            try {
                Directory.CreateDirectory(logFolderPath);

                using (var writer = new StreamWriter(logFilePath, true)) {
                    writer.WriteLine(message);
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Erro ao registrar mensagem: {ex.Message}");
            }
        }
    }
}
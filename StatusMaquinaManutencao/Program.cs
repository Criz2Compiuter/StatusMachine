using System.Net;
using System.Net.Mail;
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
            Console.ReadLine();
            botClient.StopReceiving();
        }

        private static void SendBackupByEmail(string filePath, List<string> recipientAddresses) {
            try {
                var fromAddress = new MailAddress("matheus.adryan@mirvibrasil.com", "matheus.adryan@mirvibrasil.com");

                string subject = "backup do dia " + DateTime.Now.ToString("dd-MM-yyyy");
                string body = "Este é o backup diário dos dados.\nPor favor, verifique o anexo para mais detalhes.";

                var smtpClient = new SmtpClient {
                    Host = "smtp1.mirvibrasil.com",
                    Port = 25,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, "")
                };

                using (var message = new MailMessage { From = fromAddress, Subject = subject, Body = body }) {
                    foreach (var recipientAddress in recipientAddresses) {
                        message.To.Add(new MailAddress(recipientAddress));
                    }

                    message.Attachments.Add(new Attachment(filePath));
                    smtpClient.Send(message);
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
        private static async void Bot_OnMessage(object sender, MessageEventArgs e) {
            if (e.Message.Type == MessageType.Text) {
                var chatId = e.Message.Chat.Id;
                var messageText = e.Message.Text;

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
                        string message = $"Maq.: {machineName}\nStatus: {status}\nDescricao: {additionalInfo}\nTime: {currentTime}";

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
            var backupFolder = "C:\\Users\\Cristian\\PastaBackup";
            var backupFilename = $"Backup_{DateTime.Now:yyyyMMdd}.txt";

            var backupData = new List<string>();

            foreach (var entry in machineStatus) {
                backupData.Add(entry.Value);
            }

            File.WriteAllLines(Path.Combine(backupFolder, backupFilename), backupData);

            List<string> recipients = new List<string>
                {
                    "lucas.william@mirvibrasil.com"
                };

            SendBackupByEmail(Path.Combine(backupFolder, backupFilename), recipients);
            try {
                var smtpClient = new SmtpClient("smtp1.mirvibrasil.com") {
                    Port = 25,
                    Credentials = new NetworkCredential("matheus.adryan@mirvibrasil.com", ""),
                    EnableSsl = true,
                };

                var mail = new MailMessage {
                    From = new MailAddress("matheus.adryan@mirvibrasil.com"),
                    Subject = "Backup de Dados Diário",
                    Body = "Anexos: Dados do Backup",
                };

                foreach (var recipientAddress in recipients) {
                    mail.To.Add(recipientAddress);
                }

                mail.Attachments.Add(new Attachment(Path.Combine(backupFolder, backupFilename)));

                smtpClient.Send(mail);
            }
            catch (Exception ex) {
                Console.WriteLine("Erro ao enviar e-mail: " + ex.Message);
            }
        }

        private static void ScheduleDailyTask() {
            var now = DateTime.Now;
            var timeUntilMidnight = TimeSpan.FromMinutes(1);

            var timer = new Timer(PerformDailyTask, null, timeUntilMidnight, TimeSpan.FromMinutes(1));
        }
    }
}
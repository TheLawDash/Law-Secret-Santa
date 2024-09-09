using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Law_Secret_Santa.Functions;
using Law_Secret_Santa.Models;
using Law_Secret_Santa.SlashCommandService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using System.Timers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Law_Secret_Santa
{
    public class Program
    {
        #region Defining Private Variables
        private string _configLocation = "config.json";
        #endregion
        #region Defining Global Variables
        public static string DatabaseLocation = "LawSSBotDatabase.db";
        public static ILogger<Program> GlobalLogger;
        public static string EncryptionKey = "";
        public static string EncryptionIV = "";
        public static string AdministratorRoleId = "";
        public static System.Timers.Timer EventTimer;
        #endregion
        public static DiscordClient? Client { get; set; }
        public static void Main()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug); // Set minimum log level to Debug
            });

            GlobalLogger = loggerFactory.CreateLogger<Program>();
            Program programInstance = new Program();
            programInstance.StartBotAsync().GetAwaiter().GetResult();
        }
        public async Task StartBotAsync()
        {
            if (!File.Exists(DatabaseLocation))
            {
                GlobalLogger.LogWarning($"Database, {DatabaseLocation} does not exist. Creating it now...");
                bool createdDatabase = DatabaseFunctions.CreateDataBase(DatabaseLocation);
                if (!createdDatabase)
                {
                    GlobalLogger.LogError($"Unable to create database. Please contact thelawdash on discord regarding your issues. Press any key to close.");
                    Console.ReadKey();
                    return;
                }
            }
            else
                GlobalLogger.LogInformation("Database existst!");
            if(!File.Exists("ENCDb"))
            {
                var file = File.Create("ENCDb");
                file.Close();
                var keys = EncryptionFunctions.GenerateAesKeyAndIV();
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("AES_KEY", Convert.ToBase64String(keys.Key));
                data.Add("AES_IV", Convert.ToBase64String(keys.IV));
                string jsonString = JsonSerializer.Serialize(data);
                File.WriteAllText("ENCDb", jsonString);
                EncryptionIV = Convert.ToBase64String(keys.IV);
                EncryptionKey = Convert.ToBase64String(keys.Key);
            }
            else
            {
                var data = File.ReadAllText("ENCDb");
                Dictionary<string, string> deserializedData = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
                deserializedData.TryGetValue("AES_KEY", out EncryptionKey);
                deserializedData.TryGetValue("AES_IV", out EncryptionIV);
            }
            if(File.Exists(_configLocation))
            {
                string configString = File.ReadAllText(_configLocation);
                try
                {
                    ConfigModel? config = JsonSerializer.Deserialize<ConfigModel>(configString);
                    AdministratorRoleId = config.AdminRoleId;
                    if (config == null)
                    {
                        GlobalLogger.LogError("Failed to deserialize config.json. Please check the file format. Press any key to close.");
                        Console.ReadKey();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(config.AdminRoleId) || string.IsNullOrWhiteSpace(config.DiscordBotToken) || config.AdminRoleId == "CHANGE ME" || config.DiscordBotToken == "CHANGE ME")
                    {
                        GlobalLogger.LogError("Please make sure to replace the 'CHANGE HERE' placeholders in config.json. Press any key to close.");
                        Console.ReadKey();
                        return;
                    }
                    var discordConfig = new DiscordConfiguration
                    {
                        Token = config.DiscordBotToken,
                        TokenType = TokenType.Bot,
                        AutoReconnect = true,
                        Intents = DiscordIntents.All
                    };
                    Client = new DiscordClient(discordConfig);
                    
                    Client.UseInteractivity(new DSharpPlus.Interactivity.InteractivityConfiguration
                    {
                        Timeout = TimeSpan.FromMinutes(2)
                    });
                    var services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider();
                    var slashComands = Client.UseSlashCommands(new SlashCommandsConfiguration()
                    {
                        Services = services
                    });
                    Client.GuildMemberUpdated += UserGivenRole;
                    Client.Ready += OnReady;
                    slashComands.RegisterCommands<SlashCommands>();
                    await Client.ConnectAsync();

                    await Task.Delay(-1);
                }
                catch
                {
                    GlobalLogger.LogError($"Please ensure that the config.json file is not empty or malformatted. Press any key to close.");
                    Console.ReadKey();
                    return;
                }
            }
            else
            {
                var config = File.Create("config.json");
                config.Close();
                string defaultConfig = @"{
  ""DiscordBotToken"": ""CHANGE ME"",
  ""AdminRoleId"": ""CHANGE ME""
}";

                File.WriteAllText("config.json", defaultConfig);
                GlobalLogger.LogError("Config.json did not exist, please make sure to open it, and change the \"CHANGE ME\" to a valid value.  Press any key to close.");
                Console.ReadKey();
                return;
            }
        }

        private Task OnReady(DiscordClient sender, ReadyEventArgs args)
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent == new EventData() && secretSantaEvent.ActiveEvent == "NoEntries")
                return Task.CompletedTask;
            if (secretSantaEvent.StartedExchange =="1")
            {
                EventTimer = new System.Timers.Timer();
                EventTimer.Interval = 10000;
                EventTimer.Elapsed += CheckEventDeadline;
                EventTimer.Start();
            }

            return Task.CompletedTask;
        }

        private async void CheckEventDeadline(object? sender, ElapsedEventArgs e)
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent == new EventData() && secretSantaEvent.ActiveEvent == "NoEntries")
                return;
            if(DateTime.Now > secretSantaEvent.Deadline)
            { 

                var guildsList = Client.Guilds.ToList();
                var guild = guildsList.Find(x => x.Value.Id == ulong.Parse(secretSantaEvent.GuildId));
                var guildMembers = guild.Value.Members.ToList();
                var santaOwnerDiscord = guildMembers.Find(x => x.Value.Id == ulong.Parse(secretSantaEvent.EventCreatorId));
                GlobalLogger.LogInformation("Deadline Surpassed!");
                var pairs = DatabaseFunctions.GetAllPairData(connection, secretSantaEvent.EventId);
                string responseString = "```ansi\r\nSecret santa deadline has been hit, everyone should have their gifts purchased.\n";
                foreach (var pair in pairs)
                {
                    var santaDiscordUser = guildMembers.Find(x => x.Value.Id == ulong.Parse(pair.SantaId));
                    var subjectDiscordUser = guildMembers.Find(x => x.Value.Id == ulong.Parse(pair.SubjectId));
                    var santaUser = DatabaseFunctions.GetUserData(connection, secretSantaEvent.EventId, pair.SantaId);
                    var subjectUser = DatabaseFunctions.GetUserData(connection, secretSantaEvent.EventId, pair.SubjectId);
                    if(pair.GiftStatus == "Pending")
                    {
                        responseString += $"{santaDiscordUser.Value.Nickname} ({santaDiscordUser.Value.Username}) ({santaDiscordUser.Value.Id}) has not changed their purchase value to done.\n\n";
                    }
                }
                responseString += "Thank you for using the Secret Santa Discord Bot, have a \u001b[2;31m\u001b[0m\u001b[1;2m\u001b[1;31mM\u001b[0m\u001b[1;32me\u001b[1;34mr\u001b[0m\u001b[1;32m\u001b[0m\u001b[1;32mr\u001b[0m\u001b[1;31my\u001b[0m \u001b[1;31mC\u001b[0m\u001b[1;34m\u001b[1;32mh\u001b[0m\u001b[1;34m\u001b[0m\u001b[1;32m\u001b[1;34mr\u001b[0m\u001b[1;32m\u001b[0m\u001b[1;31mi\u001b[0m\u001b[1;32ms\u001b[0m\u001b[1;34mt\u001b[0m\u001b[1;31mm\u001b[0m\u001b[1;32ma\u001b[0m\u001b[1;31ms\u001b[0m\u001b[1;2m\u001b[1;2m\u001b[1;2m\u001b[1;2m\u001b[0m\u001b[0m\u001b[0m\u001b[0m\u001b[0m\r\n```";
                await santaOwnerDiscord.Value.SendMessageAsync(responseString);
                EventTimer.Stop();
                DatabaseFunctions.ChangeStatusOfEvent(connection, secretSantaEvent.EventId);
            }
        }

        private async Task UserGivenRole(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
                return;
            if (secretSantaEvent.StartedExchange != "1")
            {
                string roleId = secretSantaEvent.DiscordRoleId;
                var oldRoles = args.RolesBefore.Select(role => role.Id).ToHashSet();
                var newRoles = args.RolesAfter.Select(role => role.Id).ToHashSet();

                var addedRoles = newRoles.Except(oldRoles);
                var removedRoles = oldRoles.Except(newRoles);
                if (addedRoles.Count() > 0)
                {
                    foreach (var role in addedRoles)
                    {
                        if (role == ulong.Parse(roleId))
                        {
                            DatabaseFunctions.InsertUser(connection, args.Member.Id.ToString(), int.Parse(secretSantaEvent.EventId));
                            try
                            {
                                var signedUpEmbed = new DiscordEmbedBuilder()
                                {
                                    Color = DiscordColor.Green,
                                    Title = "You've been signed up!",
                                    Description = $"You've been signed up to participate in the {args.Guild.Name} secret santa event.\n\n\n" +
        $"Please use the /register-address command and enter your address in order to be successfully signed up!\n\n" +
                                    "\u200B\n\u200B\n\u200B",
                                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                                    {
                                        Url = "https://i.postimg.cc/SNY8JCKK/Law-Secret-Santa.png",
                                    },
                                    Footer = new DiscordEmbedBuilder.EmbedFooter
                                    {
                                        Text = $"\n\nPlease note, the host of this bot in your server will have access to what you put as your address, if you want it private, do not use this.",
                                    }
                                };
                                await args.Member.SendMessageAsync(signedUpEmbed);
                            }
                            catch
                            {
                                var channels = await args.Guild.GetChannelsAsync();
                                var channelList = channels.ToList();
                                var general = channelList.FindAll(x => x.Name.Contains("general")).First();
                                if (general != null)
                                    await general.SendMessageAsync($"{args.Member.Mention} your dms are closed, please open them, and have the administrator give you the role again.");
                            }
                        }
                    }
                }
                else if (removedRoles.Count() > 0)
                {
                    foreach (var role in removedRoles)
                    {
                        if (role == ulong.Parse(roleId))
                        {
                            bool userRemoved = DatabaseFunctions.RemoveUser(connection, args.Member.Id.ToString(), secretSantaEvent.EventId);
                            if (userRemoved)
                            {
                                try
                                {
                                    GlobalLogger.LogInformation($"{args.Member.Id} has been removed from the secret santa event.");
                                    await args.Member.SendMessageAsync($"You have been removed from {args.Guild.Name}'s secret santa event.");
                                }
                                catch
                                {
                                    var channels = await args.Guild.GetChannelsAsync();
                                    var channelList = channels.ToList();
                                    var general = channelList.FindAll(x => x.Name.Contains("general")).First();
                                    if (general != null)
                                        await general.SendMessageAsync($"{args.Member.Mention} your dms are closed, please open them, you have been removed from the secret santa event.");
                                }
                                
                            }
                            else
                                GlobalLogger.LogError($"Error removing {args.Member.Id} from secret santa event.");
                        }
                    }
                }
            }
            else
                GlobalLogger.LogWarning("Secret santa has already begun, nothing was done.");
        }
    }
}
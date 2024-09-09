using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Law_Secret_Santa.Functions;
using Law_Secret_Santa.Models;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using System.Globalization;
using static Law_Secret_Santa.Program;

namespace Law_Secret_Santa.SlashCommandService
{
    public class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand("create-secret-santa", "This will create an instance of secret santa.")]
        public async Task StartSecretSantaAsync(
            InteractionContext ctx,
            [Option("Roles", "Input the role @ that you'd like to have involved in secret santa.")] DiscordRole role,
            [Option("PriceRange", "This will give a price range for users to abide by. Ex. $0-$50.")] string priceRange,
            [Option("Deadline", "This will be the last possible time to opt in to Secret Santa. MM/DD/YYYY")] string deadline
            )
        {
            var adminRole = ctx.Guild.GetRole(ulong.Parse(AdministratorRoleId));
            if(!ctx.Member.Roles.Contains(adminRole))
            {
                await ctx.CreateResponseAsync("You are not able to execute this command.", true);
                return;
            }
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent != new EventData() && secretSantaEvent.ActiveEvent != "NoEntries")
            {
                await ctx.CreateResponseAsync("There is already an active secret santa, if you want to reset it, please type /stop-secret-santa...", true);
                return;
            }
            try
            {

                DateTime expirationDate = DateTime.ParseExact(deadline, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                if (DateTime.Now > expirationDate)
                {
                    await ctx.CreateResponseAsync("Sorry, this input cannot be in the past.", true);
                    return;
                }
                string title = $"{ctx.Member.DisplayName}'s Secret Santa {DateTime.Now.Year}";
                await ctx.CreateResponseAsync("Creating secret santa object!", true);
                int eventId = DatabaseFunctions.InsertSecretSantaEvent(connection, ctx.Guild.Id.ToString(), ctx.Member.Id.ToString(), title, priceRange, expirationDate, role.Id.ToString(), 1);
                foreach (DiscordMember member in ctx.Guild.Members.Values)
                {
                    if (member.Roles.Contains(role))
                    {
                        DatabaseFunctions.InsertUser(connection, member.Id.ToString(), eventId);
                        try
                        {
                            var signedUpEmbed = new DiscordEmbedBuilder()
                            {
                                Color = DiscordColor.Green,
                                Title = "You've been signed up!",
                                Description = $"You've been signed up to participate in the {title} event.\n\n\n" +
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
                            await member.SendMessageAsync(signedUpEmbed);
                        }
                        catch
                        {
                            var channels = await ctx.Guild.GetChannelsAsync();
                            var channelList = channels.ToList();
                            var general = channelList.FindAll(x => x.Name.Contains("general")).First();
                            if (general != null)
                                await general.SendMessageAsync($"{member.Mention} your dms are closed, please open them, and have the administrator give you the role again.");
                        }
                    }
                } 

            }
            catch (FormatException)
            {
                GlobalLogger.LogError($"{ctx.Member.DisplayName} did not enter a propper deadline.");
                await ctx.CreateResponseAsync("Please enter a valid deadline, the format is MM/dd/yyyy ex. 09/08/2024", true);
            }
            connection.Close();
        }

        [SlashCommand("check-event", "Checks all events")]
        public async Task CheckSecretSantaEventAsync(InteractionContext ctx)
        {
            var adminRole = ctx.Guild.GetRole(ulong.Parse(AdministratorRoleId));
            if (!ctx.Member.Roles.Contains(adminRole))
            {
                await ctx.CreateResponseAsync("You are not able to execute this command.", true);
                return;
            }
            string responseString = "Result:\n";
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
            {
                await ctx.CreateResponseAsync("There is no active secret santa instances.", true);
                return;
            }
            var discordMembers = ctx.Guild.Members.Values.ToList();
            if (secretSantaEvent.StartedExchange != "1")
            {
                var users = DatabaseFunctions.GetUsersInEvent(connection, secretSantaEvent.EventId);
                foreach (var user in users)
                {

                    var userObject = discordMembers.Find(x => x.Id == ulong.Parse(user.DiscordId));
                    responseString += $"```ansi\n{userObject.DisplayName} ({userObject.Id}) is participating. Address Input ";
                    if (user.StreetAddress == "")
                    {
                        responseString += @"Status [2;31m[Missing][0m```";
                    }
                    else
                    {
                        responseString += @"Status [2;31m[2;32m[Completed][0m[2;31m[0m```";
                    }
                    responseString += "\n";
                }
                await ctx.CreateResponseAsync(responseString, true);
            }
            else
            {
                var pairs = DatabaseFunctions.GetAllPairData(connection, secretSantaEvent.EventId);
                if (pairs != new List<PairData>())
                {
                    foreach (var pair in pairs)
                    {
                        DiscordMember? userObject = discordMembers.Find(x => x.Id == ulong.Parse(pair.SantaId));
                        responseString += $"```ansi\n{userObject.DisplayName} ({userObject.Id}) is participating. Purchase Gift ";
                        if (pair.GiftStatus == "Pending")
                            responseString += "Status \u001b[2;31m\u001b[2;32m\u001b[2;33m[Pending]\u001b[0m\u001b[2;32m\u001b[2;33m\u001b[0m\u001b[2;32m\u001b[0m\u001b[2;31m\u001b[0m\r\n```";
                        else if (pair.GiftStatus == "Done")
                            responseString += "Status \u001b[2;31m\u001b[2;32m\u001b[2;33m\u001b[2;32m\u001b[2;32m[Done]\u001b[0m\u001b[2;32m\u001b[0m\u001b[2;33m\u001b[0m\u001b[2;32m\u001b[2;33m\u001b[0m\u001b[2;32m\u001b[0m\u001b[2;31m\u001b[0m\r\n```";

                        responseString += "\n";
                    }
                }
                else
                {
                    await ctx.CreateResponseAsync("Error with this call.", true);
                    return;
                }
                await ctx.CreateResponseAsync(responseString, true);
            }
        }

        [SlashCommand("cancel-secret-santa", "Cancel current instance of secret santa.")]
        public async Task StopSecretSantaAsync(InteractionContext ctx)
        {
            var adminRole = ctx.Guild.GetRole(ulong.Parse(AdministratorRoleId));
            if (!ctx.Member.Roles.Contains(adminRole))
            {
                await ctx.CreateResponseAsync("You are not able to execute this command.", true);
                return;
            }
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
            {
                await ctx.CreateResponseAsync("There is no active secret santa instances.", true);
                return;
            }
            bool updated = DatabaseFunctions.ChangeStatusOfEvent(connection, secretSantaEvent.EventId);
            if (updated)
                await ctx.CreateResponseAsync("The current event has been stopped.", true);
            else
                await ctx.CreateResponseAsync("There was an error stopping this event.", true);
        }

        [SlashCommand("register-address", "Register your address for secret santa, bot host will be able to see this.")]
        public async Task RegisterUserAddressAsync(
            InteractionContext ctx,
            [Option("Address", "This will be the shipping address provided to your secret santa.")] string userAddress
            )
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
            {
                await ctx.CreateResponseAsync("There is no active secret santa instances.", true);
                return;
            }
            if (secretSantaEvent.StartedExchange == "0")
            {
                bool updated = DatabaseFunctions.UpdateUserAddress(connection, secretSantaEvent.EventId, userAddress, ctx.Interaction.User.Id.ToString());
                if (updated)
                    await ctx.CreateResponseAsync("Your address has been updated!", true);
                else
                    await ctx.CreateResponseAsync("There was an issue updating your address!", true);
            }
            else
            {
                await ctx.CreateResponseAsync("The event has started, you are unable to change your address!");
            }
        }

        [SlashCommand("check-address", "Check what address you have inputted into the database.")]
        public async Task CheckUserAddressAsync(
            InteractionContext ctx)
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
            {
                await ctx.CreateResponseAsync("There is no active secret santa instances.", true);
                return;
            }
            var userData = DatabaseFunctions.GetUserData(connection, secretSantaEvent.EventId, ctx.Interaction.User.Id.ToString());
            if (userData.StreetAddress == null)
                await ctx.CreateResponseAsync("You have no address in file, please use /register-address to register yours.", true);
            else
                await ctx.CreateResponseAsync($"The address on file is {userData.StreetAddress}", true);
        }

        [SlashCommand("choose-secret-santa", "This will manually begin assigning different people for secret santa.")]
        public async Task ChooseSecretSantaAsync(
            InteractionContext ctx)
        {
            var adminRole = ctx.Guild.GetRole(ulong.Parse(AdministratorRoleId));
            if (!ctx.Member.Roles.Contains(adminRole))
            {
                await ctx.CreateResponseAsync("You are not able to execute this command.", true);
                return;
            }
            Random random = new Random();
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
            {
                await ctx.CreateResponseAsync("There is no active secret santa instances.", true);
                return;
            }
            if (secretSantaEvent.StartedExchange == "1")
            {
                await ctx.CreateResponseAsync("The secret santa has already been chosen.");
                return;
            }
            var users = DatabaseFunctions.GetUsersInEvent(connection, secretSantaEvent.EventId);
            var noAddress = users.FindAll(x => x.StreetAddress == "");
            if (noAddress.Count > 0)
            {
                string userString = "";
                foreach (var address in noAddress)
                {
                    userString += $"\n\n<@{address.DiscordId}> has not input their address.\n\n";
                }

                await ctx.CreateResponseAsync($"Not all members have input their address. To forcefully remove them, remove the santa role then try again." + userString, true);
                return;
            }

            var userList = ctx.Guild.Members;
            users = users.OrderBy(x => random.Next()).ToList();
            Dictionary<UserObject, UserObject> assignments = new Dictionary<UserObject, UserObject>();
            for (int i = 0; i < users.Count; i++)
            {
                var currentUser = users[i];
                var assignedUserId = users[(i + 1) % users.Count];
                assignments[currentUser] = assignedUserId;
            }
            foreach (var user in assignments)
            {
                GlobalLogger.LogInformation($"{user.Key} is {user.Value}'s secret santa!");
                DatabaseFunctions.AddSantaPair(connection, secretSantaEvent.EventId, user.Key, user.Value);
                var santaUser = userList.Where(x => x.Value.Id == ulong.Parse(user.Key.DiscordId)).FirstOrDefault();
                var subjectUser = userList.Where(x => x.Value.Id == ulong.Parse(user.Value.DiscordId)).FirstOrDefault();
                var signedUpEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Green,
                    Title = "You've been signed up!",
                    Description = $"Secret Santa has begun!\n\n\n" +
                                    $"You have been tasked with purchasing a present for {subjectUser.Value.Mention}.\n\n" +
                                    $"Run /get-address to access their address for shipping.\n\n" +
                                    $"The price range is {secretSantaEvent.PriceRange}.\n\n" +
                                    $"The deadline for purchasing items is {secretSantaEvent.Deadline}.\n\n" +
                                    $"Once you have purchased an item please use /update-gift and set it to \"Done\"\n\n" +
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
                await santaUser.Value.SendMessageAsync(signedUpEmbed);
            }
            await ctx.CreateResponseAsync("The santa pairs have been chosen!", true);
            DatabaseFunctions.UpdateToStarted(connection, secretSantaEvent.EventId);
        }

        [SlashCommand("get-address", "This will provide you wiht the address of the person you're giving a gift to.")]
        public async Task GetSubjectAddressAsync(InteractionContext ctx)
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
            {
                await ctx.CreateResponseAsync("There is no active secret santa instances.", true);
                return;
            }
            if (secretSantaEvent.StartedExchange == "0")
            {
                await ctx.CreateResponseAsync("The secret santa event has not assigned you anyone yet.", true);
                return;
            }
            string subjectId = DatabaseFunctions.GetSubjectId(connection, ctx.Interaction.User.Id.ToString(), secretSantaEvent.EventId);
            var subjectUser = DatabaseFunctions.GetUserData(connection, secretSantaEvent.EventId, subjectId);
            if (subjectUser != new UserObject())
            {
                await ctx.CreateResponseAsync($"You need to purchase something for <@{subjectId}>, their address is `{subjectUser.StreetAddress}`", true);
            }
        }

        [SlashCommand("update-gift", "This will update the status of the gift you have purchased.")]
        public async Task UpdateGiftStatusAsync(
            InteractionContext ctx,
            [Choice("Pending", "Pending")]
            [Choice("Done", "Done")]
            [Option("status", "Choose Gift Status")] string status)
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;");
            connection.Open();
            var secretSantaEvent = DatabaseFunctions.GetActiveEvent(connection);
            if (secretSantaEvent.ActiveEvent == "NoEntries")
            {
                await ctx.CreateResponseAsync("There is no active secret santa instances.", true);
                return;
            }
            if (secretSantaEvent.StartedExchange == "0")
            {
                await ctx.CreateResponseAsync("The secret santa event has not assigned you anyone yet.", true);
                return;
            }
            bool success = DatabaseFunctions.UpdateGiftStatus(connection, secretSantaEvent.EventId, ctx.Interaction.User.Id.ToString(), status);
            if (success)
            {
                await ctx.CreateResponseAsync("Update your gift status.", true);
                return;
            }
            else
            {
                await ctx.CreateResponseAsync("There was an issue with your response.", true);
            }
        }
    }
}

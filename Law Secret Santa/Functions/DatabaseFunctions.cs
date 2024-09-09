using Law_Secret_Santa.Models;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using static Law_Secret_Santa.Program;
namespace Law_Secret_Santa.Functions
{
    public class DatabaseFunctions
    {
        public static bool CreateDataBase(string databasePath)
        {
            SQLiteConnection.CreateFile(databasePath);

            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                string createEventTable =
                    @"
                        CREATE TABLE IF NOT EXISTS SecretSantaEvents (
                            EventID INTEGER PRIMARY KEY AUTOINCREMENT,  -- Auto-incremented ID for each event
                            GuildID TEXT NOT NULL,                      -- Discord Guild (Server) ID
                            EventCreatorID TEXT NOT NULL,               -- Discord User ID of the event creator
                            EventTitle TEXT NOT NULL,                   -- Title or name of the Secret Santa event
                            PriceRange TEXT NOT NULL,                   -- Price range for gifts (e.g., '$0-$50')
                            Deadline DATETIME NOT NULL,                 -- Deadline for opting in to Secret Santa
                            DiscordRoleID TEXT NOT NULL,                -- Discord Role ID of the participants
                            StartedExchange INTEGER NOT NULL DEFAULT 0, -- If secret santa has started and the exchange of people has occurred.
                            ActiveEvent INTEGER NOT NULL DEFAULT 1,     -- Active status (0 = not active, 1 = active)
                            FOREIGN KEY (EventCreatorID) REFERENCES Users(DiscordID)  -- References the Users table
                        );";
                ExecuteCommand("Create Event Table", createEventTable, connection);

                string createUserTable =
                @"
                CREATE TABLE IF NOT EXISTS Users (
                    
                    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                    DiscordID TEXT NOT NULL,           -- Discord User ID (Unique Identifier)
                    EventID INTEGER NOT NULL,
                    UserAddress TEXT
                );";
                ExecuteCommand("Create User Table", createUserTable, connection);

                string createPairsTable =
                @"
                CREATE TABLE IF NOT EXISTS Pairs (
                    PairID INTEGER PRIMARY KEY AUTOINCREMENT,  -- Auto-incremented ID for each pairing
                    EventID INTEGER NOT NULL,                  -- References the Secret Santa event
                    SantaID TEXT NOT NULL,                     -- Discord User ID of the giver (Santa)
                    SubjectID TEXT NOT NULL,                   -- Discord User ID of the receiver (Subject)
                    GiftStatus TEXT NOT NULL DEFAULT 'Pending', -- Status of the gift (Pending, Sent, Received)
                    FOREIGN KEY (EventID) REFERENCES SecretSantaEvents(EventID),  -- References the SecretSantaEvents table
                    FOREIGN KEY (SantaID) REFERENCES Users(DiscordID),               -- References the Users table
                    FOREIGN KEY (SubjectID) REFERENCES Users(DiscordID)              -- References the Users table
                );
                ";
                ExecuteCommand("Create Pairs Table", createPairsTable, connection);

                GlobalLogger.LogInformation("Database has been created!");
                return true;
            }
        }
        public static bool ExecuteCommand(string queryName, string query, SQLiteConnection connection)
        {
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.ExecuteNonQuery();
                GlobalLogger.LogInformation($"Query executed for {queryName}.");
                return true;
            }
        }
        public static int InsertSecretSantaEvent(SQLiteConnection connection, string guildId, string eventCreatorId, string eventTitle, string priceRange, DateTime deadline, string discordRoleId, int activeEvent)
        {
            string insertQuery = @"
            INSERT INTO SecretSantaEvents (GuildID, EventCreatorID, EventTitle, PriceRange, Deadline, DiscordRoleID, ActiveEvent)
            VALUES (@GuildID, @EventCreatorID, @EventTitle, @PriceRange, @Deadline, @DiscordRoleID, @ActiveEvent);
        ";
            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@GuildID", guildId);
                command.Parameters.AddWithValue("@EventCreatorID", eventCreatorId);
                command.Parameters.AddWithValue("@EventTitle", eventTitle);
                command.Parameters.AddWithValue("@PriceRange", priceRange);
                command.Parameters.AddWithValue("@Deadline", deadline);
                command.Parameters.AddWithValue("@DiscordRoleID", discordRoleId);
                command.Parameters.AddWithValue("@ActiveEvent", activeEvent);

                try
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Event inserted successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting event: {ex.Message}");
                }
            }
            string getQuery = @"
            SELECT EventID FROM SecretSantaEvents WHERE GuildID == @GuildID AND EventCreatorID == @EventCreatorID AND EventTitle == @EventTitle AND PriceRange == @PriceRange AND Deadline == @Deadline AND DiscordRoleID == @DiscordRoleID AND ActiveEvent == @ActiveEvent;
        ";
            using (var command = new SQLiteCommand(getQuery, connection))
            {
                command.Parameters.AddWithValue("@GuildID", guildId);
                command.Parameters.AddWithValue("@EventCreatorID", eventCreatorId);
                command.Parameters.AddWithValue("@EventTitle", eventTitle);
                command.Parameters.AddWithValue("@PriceRange", priceRange);
                command.Parameters.AddWithValue("@Deadline", deadline);
                command.Parameters.AddWithValue("@DiscordRoleID", discordRoleId);
                command.Parameters.AddWithValue("@ActiveEvent", activeEvent);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return int.Parse(reader["EventID"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting event: {ex.Message}");
                }
            }
            return -1;
        }
        public static void InsertUser(SQLiteConnection connection, string discordId, int eventId, string userAddress = "")
        {
            string insertQuery = @"
                INSERT INTO Users (DiscordID, EventID)
                VALUES (@DiscordID, @EventID);
                ";

            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@DiscordID", discordId);
                command.Parameters.AddWithValue("@EventID", eventId);
                try
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("User inserted successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting user: {ex.Message}");
                }
            }
        }
        public static void InsertPair(SQLiteConnection connection, int eventId, string santaId, string subjectId, string giftStatus)
        {
            string insertQuery = @"
                INSERT INTO Pairs (EventID, SantaID, SubjectID, GiftStatus)
                VALUES (@EventID, @SantaID, @SubjectID, @GiftStatus);
            ";

            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@SantaID", santaId);
                command.Parameters.AddWithValue("@SubjectID", subjectId);
                command.Parameters.AddWithValue("@GiftStatus", giftStatus);

                try
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Pair inserted successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting pair: {ex.Message}");
                }
            }
        }
        public static EventData GetActiveEvent(SQLiteConnection connection)
        {
            EventData eventId = new EventData();

            
            string query = "SELECT * FROM SecretSantaEvents WHERE ActiveEvent == 1 OR StartedExchange == 1;";
            using (var command = new SQLiteCommand(query, connection))
            {
                try
                {
                    using (var reader = command.ExecuteReader())
                    { 
                        if (!reader.HasRows)
                        {
                            eventId.ActiveEvent = "NoEntries"; // Insantiating this so it passes the check;
                        }
                        while (reader.Read())
                        {
                            eventId.StartedExchange = reader["StartedExchange"].ToString();
                            eventId.EventId = reader["EventID"].ToString();
                            eventId.ActiveEvent = reader["ActiveEvent"].ToString();
                            eventId.EventTitle = reader["EventTitle"].ToString();
                            eventId.PriceRange = reader["PriceRange"].ToString();
                            eventId.Deadline = DateTime.Parse(reader["Deadline"].ToString());
                            eventId.DiscordRoleId = reader["DiscordRoleID"].ToString();
                            eventId.GuildId = reader["GuildID"].ToString();
                            eventId.EventCreatorId = reader["EventCreatorID"].ToString();
                        }
                    }
                    return eventId;
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"Error in GetActiveEvent: {ex.Message}");
                    return eventId;
                }
            }
        }
        public static List<EventNameIdQuery> GetEventNames(SQLiteConnection connection)
        {
            var eventNames = new List<EventNameIdQuery>();

            // Define the query to select EventTitle from SecretSantaEvents
            string query = "SELECT EventTitle, EventID FROM SecretSantaEvents;";

            using (var command = new SQLiteCommand(query, connection))
            {
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            eventNames.Add(new EventNameIdQuery() { EventTitle = reader["EventTitle"].ToString(), EventID = reader["EventID"].ToString() });
                        }
                    }
                    Console.WriteLine("Event names fetched successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching event names: {ex.Message}");
                }
            }

            return eventNames;
        }
        public static List<UserObject> GetUsersInEvent(SQLiteConnection connection, string eventId)
        {
            var users = new List<UserObject>();

            // Define the query to select EventTitle from SecretSantaEvents
            string query = "SELECT * FROM Users WHERE EventID = @EventID;";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if(reader["UserAddress"].ToString() == "")
                            {
                                UserObject user = new UserObject()
                                {
                                    DiscordId = reader["DiscordID"].ToString(),
                                    EventId = eventId,
                                    StreetAddress = ""
                                };
                                users.Add(user);
                            }
                            else
                            {
                                UserObject user = new UserObject()
                                {
                                    DiscordId = reader["DiscordID"].ToString(),
                                    EventId = eventId,
                                    StreetAddress = EncryptionFunctions.DecryptString(reader["UserAddress"].ToString())
                                };
                                users.Add(user);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"Error fetching event names: {ex.Message}");
                }
            }

            return users;
        }
        public static bool ChangeStatusOfEvent(SQLiteConnection connection, string eventId)
        {
            string query = "UPDATE SecretSantaEvents SET ActiveEvent = 0, StartedExchange = 0 WHERE EventID == @EventID;";

            using(var command = new SQLiteCommand(query,connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                try
                {
                    command.ExecuteNonQuery();
                    GlobalLogger.LogInformation("Current event has been disabled.");
                    return true;
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"Error updating current event: {ex.Message}");
                }
            }
            return false;
        }
        public static bool UpdateUserAddress(SQLiteConnection connection, string eventId, string address, string discordId)
        {
            string query = "UPDATE Users SET UserAddress = @UserAddress WHERE EventID == @EventID AND DiscordID == @DiscordID;";

            using (var command = new SQLiteCommand(query, connection))
            {
                string encryptedAddress = EncryptionFunctions.EncryptString(address);
                command.Parameters.AddWithValue("@UserAddress", encryptedAddress);
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@DiscordID", discordId);
                try
                {
                    command.ExecuteNonQuery();
                    GlobalLogger.LogInformation($"User address has been changed ({discordId}).");
                    return true;
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"Error updating user address for {discordId}: {ex.Message}");
                }
            }
            return false;
        }
        public static UserObject GetUserData(SQLiteConnection connection, string eventId, string discordId)
        {
            string query = "SELECT UserAddress FROM Users WHERE EventID = @EventID AND DiscordID = @DiscordID;";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@DiscordID", discordId);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())  // Check if there's at least one row
                        {
                            string address = reader["UserAddress"].ToString();
                            if (string.IsNullOrEmpty(address))
                            {
                                GlobalLogger.LogError($"No user address found for DiscordID: {discordId} and EventID: {eventId}.");
                                return new UserObject { DiscordId = discordId, EventId = eventId };
                            }
                            return new UserObject
                            {
                                DiscordId = discordId,
                                EventId = eventId,
                                StreetAddress = EncryptionFunctions.DecryptString(address)
                            };
                            }
                        else
                        {
                            // No rows found
                            GlobalLogger.LogError($"No user address found for DiscordID: {discordId} and EventID: {eventId}.");
                            return new UserObject { DiscordId = discordId, EventId = eventId };
                        }
                    }
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"There was an issue getting the user address for ({discordId}): {ex.Message}");
                    return new UserObject();
                }
            }
        }
        public static bool AddSantaPair(SQLiteConnection connection, string eventId, UserObject santaObject, UserObject subjectObject)
        {
            string query = "INSERT INTO Pairs (EventID, SantaID, SubjectID) VALUES (@EventID, @SantaID, @SubjectID);";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@SantaID", santaObject.DiscordId);
                command.Parameters.AddWithValue("@SubjectID", subjectObject.DiscordId);
                try
                {
                    // Execute the query
                    int rowsAffected = command.ExecuteNonQuery();

                    // Check if the insertion was successful
                    return rowsAffected > 0;  // Returns true if at least one row was inserted
                }
                catch (Exception ex)
                {
                    // Log error if any occurs
                    GlobalLogger.LogError($"Error adding Santa pair for EventID {eventId}: {ex.Message}");
                    return false;
                }
            }
        }
        public static bool RemoveUser(SQLiteConnection connection, string discordId, string eventId)
        {
            string query = "DELETE FROM Users WHERE EventID == @EventID AND DiscordID == @DiscordID;";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@DiscordID", discordId);
                try
                {
                    // Execute the query
                    int rowsAffected = command.ExecuteNonQuery();

                    // Check if the insertion was successful
                    return rowsAffected > 0;  // Returns true if at least one row was inserted
                }
                catch (Exception ex)
                {
                    // Log error if any occurs
                    GlobalLogger.LogError($"Error removing user pair for EventID {eventId}: {ex.Message}");
                    return false;
                }
            }
        }
        public static string GetSubjectId(SQLiteConnection connection, string santaDiscordId, string eventId)
        {
            string query = "SELECT SubjectID FROM Pairs WHERE SantaID == @SantaID AND EventID == @EventID;";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@SantaID", santaDiscordId);
                command.Parameters.AddWithValue("@EventID", eventId);
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())  // Check if there's at least one row
                        {
                            string subjectId = reader["SubjectID"].ToString();

                            return subjectId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"There was an issue getting the subject discord id: {ex.Message}");
                }
            }
            return "";
        }
        public static List<PairData> GetAllPairData(SQLiteConnection connection, string eventId)
        {
            var pairData = new List<PairData>();
            string query = "SELECT * FROM Pairs;";

            using (var command = new SQLiteCommand(query, connection))
            {
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pairData.Add(new PairData() { EventId = reader["EventID"].ToString(), GiftStatus = reader["GiftStatus"].ToString(), PairId = reader["PairID"].ToString(), SantaId = reader["SantaID"].ToString(), SubjectId = reader["SubjectID"].ToString() });
                        }
                    }
                    Console.WriteLine("Event names fetched successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching event names: {ex.Message}");
                }
            }
            return pairData;
        }
        public static void UpdateToStarted(SQLiteConnection connection, string eventId)
        {
            string query = "UPDATE SecretSantaEvents SET StartedExchange = 1 WHERE EventID == @EventID;";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                try
                {
                    command.ExecuteNonQuery();
                    GlobalLogger.LogInformation($"Updated event to started.");
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"Error updating event to started: {ex.Message}");
                }
            }
        }
        public static bool UpdateGiftStatus(SQLiteConnection connection, string eventId, string santaId, string status)
        {
            string query = "UPDATE Pairs SET GiftStatus == @GiftStatus WHERE EventID == @EventID AND SantaID == @SantaID;";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@GiftStatus", status);
                command.Parameters.AddWithValue("@SantaID", santaId);
                try
                {
                    command.ExecuteNonQuery();
                    GlobalLogger.LogInformation($"Updated gift status for {santaId}");
                    return true;
                }
                catch (Exception ex)
                {
                    GlobalLogger.LogError($"Error updating gift status: {ex.Message}");
                }
            }
            return false;
        }
    }
}

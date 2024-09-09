# Project Name

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

**Secret Santa Discord Bot** is a C# application designed for allowing serverse to individually host their own secret santa event. This bot is meant to make the organization and management of secret santa events easy and fun to use. This repository contains the source code for the bot, so you can see each individual function as well as a .zip containing the compiled binaries (exe).

### Features

- Feature 1: Dynamic token definintion and admin role definintion in the config.json
- Feature 2: Organized data storage, and output, allowing for admins to keep track of who's done what.
- Feature 3: Random secret santa assignment, no information needs to be directly exchanged from one user to another.
- Feature 4: If a user is given the event role when an event is started, they're automatically entered into the event.
- Feature 5: If a user has their event role removed, they are removed from the event.
- Feature 6: If a user has the role before the event starts, they will automatically be entered into the event.
- Feature 7: Once the deadline is hit, the event organizer will be sent a message alerting them to anyone that hasn't changed their status.

## Table of Contents

- [Installation](#building-and-installation)
- [Usage](#usage)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

# Setup From /releases

To set up this project locally, follow these steps.

Access Release (exe) [HERE](https://github.com/TheLawDash/Law-Secret-Santa/releases/tag/Release)

1. **Extract the binaries**

   Right click on `Secret Santa Discord Bot.zip` and click `Extract`.

2. **Run `Law Secret Santa.exe`**

   This will initiate the dabase, creating the local `LawSSBotDatabase.db` and will create the `config.json` file.

3. **Configure Your Bot**

   Replace the sections in `config.json` that say "CHANGE ME" to the relavant information. (Tutorial on getting DiscordBotToken https://www.writebots.com/discord-bot-token/)

4. **Run The Bot**

   Double click on `Law Secret Santa.exe`, once it is running everything should be set up and you should recieve no errors.

## Building and Installation

To set up this project locally, follow these steps:

1. **Clone the repository**:

    ```bash
    git clone https://github.com/yourusername/yourrepository.git
    ```

2. **Navigate to the project directory**:

    ```bash
    cd yourrepository
    ```

3. **Install dependencies**:

    Ensure you have [.NET SDK](https://dotnet.microsoft.com/download) installed.

    ```bash
    dotnet restore
    ```

4. **Build the project**:

    ```bash
    dotnet build
    ```

5. **Run the application**:

    ```bash
    dotnet run
    ```

## Usage

Once the project is set up, you can use it as follows:

1. **Configuration**: Configure the application using the `ConfigModel.json` file. Update necessary fields such as API keys, etc.
   
2. **Running Commands**: Utilize `SlashCommands.cs` for executing commands within the application.

3. **Database Operations**: Refer to `DatabaseFunctions.cs` for handling database interactions. Database models are defined in `DatabaseModels.cs`.

4. **Encryption**: Use `EncryptionFunctions.cs` for encrypting sensitive data.

## Command Breakdown

1. `/create-secret-santa`: This will create an event that will allow you to start the secret santa process.
     Required Information:
         Roles: This will be the role you will assign to those that are participating.
         PriceRange: This will be your defined price range, IE. $10-$25.
         Deadline: This will be the date you wish for everyone to have purchased a present by.

2. `/check-event`: **ADMIN ONLY**, this will give details on the event.
    Before Santas are Chosen:
         This will give the status on everyone that is participating, letting you know who has and has not inputted their address.
    After Santas are Chosen:
         This will give the status of each santa, whether or not they have purchase a gift yet. (This will not show who has who)

3. `/cancel-secret-santa`: **ADMIN ONLY**, mess up on the creation of your event? No worries, this will remove it and archive it, allowing you to start another.

4. `/register-address`: This can be executed either in DMs with the bot or your server, but it will allow those participating to add their shipping addresses. (NOTE: this information will be stored by the bot host.)

5. `/choose-secret-santa`: **ADMIN ONLY**, this command will initiate the pairings of santas and their subject. Each santa will get a subject, and will need to type, `/get-address` to view their subject's address.

6. `/get-address`: This will get the address of the person you need to purchas a gift for.

7. `/update-gift`: This will allow the santa to update the status of their gift purchasing, options are Pending or Done.
   
## Configuration

The application can be configured using the `ConfigModel.cs` file. Here are the steps to configure:

1. Open `ConfigModel.cs`.
2. Update the configuration settings as needed:
    - **API Key**: Set up your API key.
    - **Database Connection String**: Provide your database connection details.
    - **Other Settings**: Adjust other configuration settings according to your environment.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository.
2. Create a new feature branch (`git checkout -b feature-name`).
3. Commit your changes (`git commit -m 'Add some feature'`).
4. Push to the branch (`git push origin feature-name`).
5. Open a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

For questions, issues, suggestions, or requests for custom bots feel free to open an issue or contact the maintainers.

- **Maintainer**: [TheLawDash](https://github.com/TheLawDash)
- **Email**: thelawdashdev@gmail.com
- **Discord** : thelawdash

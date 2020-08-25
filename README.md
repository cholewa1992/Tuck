# Broder Tuck

## Introduction
Broder Tuck is a Discord bot written in C#, that allows multiple guilds across a server sync Onyxia, Nefarian and Rend buffs. It can be added to multiple servers and post updates when new World Buffs are scheduled. 

### Basic usage
Below you can find how to add the bot to your server and receive updates, as well as how to schedule a World Buff.

#### Adding the bot to your server
All you need to do is [authorize the bot through Discord](https://discord.com/oauth2/authorize?client_id=723867721590112277&permissions=404544&scope=bot) and it will magically appear in your server. 

#### Required Permissions
In order for the bot to work properly, it needs the following permissions in its designated channel.
* Send Messages
* Read Messages
* Manage Messages
* Embed Links
* Read Message History
* Use External Emojis

#### Commands
The group prefix for all commands thus far is `!wbuff <Command> <...Params>`.
##### Subscription
Subscribe to a guild to get an embedded message containing world buffs posted in that guild. The message will be updated when a buff is added or removed. 

```!wbuff subscribe <Int64 GuildId>```  
Adds a subscription to the channel where the command is executed.  
*Example:* `!wbuff subscribe 629446846858919947`
   
```!wbuff unsubscribe```  
Removes subscription from the channel if one is present.

##### Alerts
Highlight a Discord Group when an update is made on the subscribed guild. 
*Requires the current guild to have an active subscription.

```!wbuff alert add <String @Role>```  
Enables alerts for @role inside the guilds subscribed channel or replaces the existing role.  
*Example:* `!wbuff alert add @everyone`   
   
`!wbuff alert remove`  
Removes the alerts if present.  

##### World Buffs
Add or remove an upcomming World Buff.
*These should only be posted in the "Master" guild (Usually Realm Discord).

```!wbuff add <[ony|nef|rend] buff> <DateTime Time>```  
Adds the specified buff to the selected time, if time is less than current server time, the buff will be listed for the next day.  
*Example:* `!wbuff add ony 18:45`

```!wbuff remove <[ony|nef|rend] buff> <DateTime Time>```  
Removes the specified buff from the list.  
*Example:* `!wbuff remove ony 18:45`

## Development
### Requirements
* [.Net Core](https://dotnet.microsoft.com/download) => 3.1
* [EF Core Tools](https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/dotnet) => 3.x

### Setting up your local environment
1. Clone this repository.
2. Create an application at the [Discord Developer Portal](https://discord.com/developers/).
3. Copy your application's bot token into your environment variables under the key: `DiscordToken`.
4. Run the DB setup and migrations through your CLI:
   1. `$ dotnet restore`
   2. `$ dotnet ef database upgrade`

### Running the bot
To start the application:
```$ dotnet run```
To exit the application just press ^C.
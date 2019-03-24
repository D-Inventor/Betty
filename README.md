# Betty
Betty is my personal bot assistant on Discord. Betty is timezone aware and contains functionality that can help server owners to manage their server.

# Features
Betty is a discord bot designed as an aid for international gaming communities. As such, Betty has the following features:

**Commands for Public**
`ping`: Returns a pong. Use this to test if Betty is online.

**Commands for Members**
`time (12 hour time) (utc | local)`: Returns a table with given time in either local or utc time, expressed in all relevant timezones. If no time was provided, Betty will use the current time.
*example: $time 7 pm*
`plan [DD:MM:YYYY] [12 hour time] [name]`: Plans an event at the given date and time in your local timezone. Betty will give notifications 2 hours before the event, 30 minutes before the event and at the start of the event.
*example: $plan 05:09:2019 8:30pm Weekly minigame event*
`cancel [name]`: Cancels event with given name. No more notifications will be sent for event with given name.
*example: $cancel Weekly minigame event*
`get events (all | first)`: Returns events that correspond to given parameter.

**Commands for Admin**
`app start [date] [12 hour time]` Starts applications until given deadline. Creates a channel and an invite.
`app stop` Removes the applications channel and deletes the invite. Be careful when using this command as there is no way to get the messages in the applications chat back.

`get state` returns all information that Betty knows about this guild.
`get language` returns information about languages for this guild.

`set language [language]` sets language to given language.
`set public` Sets the channel of execution as the public channel.
`set notification` Sets the channel of execution as the notification channel.
`set timezones` Creates/Updates the timezone roles in this discord.

**Other commands**
`set permission [mention role/user] [permission]` Changes the permission of given role/user. The caller needs to have a higher permission than the user/role and the desired permission.
There are currently 4 permissions: Public, Member, Admin and Owner. Only Admins and Owners can make people Members and only Owners can make people Admins.

**Message analysis**
Betty will actively search for time indications in messages and translate them accordingly. So if you write $7:00pm in the middle of a sentence like this, Betty will notice that.

# Dependencies
This project depends on:
- Discord.Net: The .NET interface with the Discord API.
- timezoneconverter: An OS independent library for reliable timezone conversion and naming.

# Note before use
This repo comes with a release that can be run independently on a raspberry pi.
If you want to use Betty on a different platform, you can create your own standalone app using the `dotnet publish` command
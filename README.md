# Betty
Betty is my personal bot assistant on Discord. Betty is timezone aware and contains functionality that can help server owners to manage their server.

# Features
Betty is a discord bot designed as an aid for international gaming communities. As such, Betty has the following features:
- generation of responses, based on language files.
	Betty can interpret language files into a dictionary. She generates random messages based on a given start keyword
- conversion of times between timezones.
	Betty can read the timezone from the user and convert the given time from his/her local time to UTC.
	- using the command `$time [12 hour time]`, Betty will translate the given time from user's local time to the time in all relevant timezones.
	- any 12 hour time, prefixed with a '$', will be interpreted and translated by Betty.
	These commands require the timezones to be initialised and require the user to have assigned themselves the role for their timezone.
- planning of events.
	Betty keeps an internal agenda with all planned events. The corresponding guild will receive notifications before and at the start of every given event.
	- using the command `$plan [DD:MM:YYYY] [12 hour time] [title of the event]`, users can add an event to the agenda.
	- using the command `$event`, Betty returns a message with the date and time of the first coming event.
	- using the command `$cancel [title of the event]`, users can cancel the first coming event with given title. This title needs to match 100%
- moderation.
	- using the `$set [option]` command, the user can configure and initialise the discord server.
		- `public` sets the channel of execution as the public channel. This channel will be used for messages that need to be visible to all users in a server.
			the public channel will also be used if the notification channel has not been set.
		- `notification` sets the channel of execution as the notification channel. This channel will be used for messages that are only visible to 'members' of your community.
		- `timezones` initialises timezone functionality. This command will create a role for each Windows timezone*.
			The roles that this bot creates allow the user to define their local time for the bot.
			(*) Windows timezones are timezones as defined in the Microsoft Windows OS registry.
	- using the `$unset [option]` command, the user can deconfigure given option.
	- using the `$appstart [DD:MM:YYYY] [12 hour time]` command, the user can start applications for the server of execution.
		This command creates an application text channel in the same category as the public channel and will therefore inherit the same privileges.
		This command will create an invite link to this application channel and will delete the invite as soon as the deadline expires or the application is cancelled.
	- using the `$appstop` command, the user can stop applications.
		This command will delete the application channel and delete the invite link.

# Dependencies
This project depends on:
- Discord.Net: The .NET interface with the Discord API.
- timezoneconverter: An OS independent library for reliable timezone conversion and naming.

# Note before use
To run this bot on a raspberry pi, you need to have dotnet 2.0 installed on it. To build and deploy this bot to the rpi, you need to edit the `build.cake` file so that it contains the details of your rpi.
Before the bot can be used, it needs to know your private token. This token needs to be pasted into the config file behind `TOKEN:`.
ALWAYS KEEP YOUR TOKEN PRIVATE! otherwise, everyone can get access to your bot and potentially ruin your servers.



Get the latest Cake build.ps1 by opening a powershell script, change the working directory to this one, and run:

Invoke-WebRequest http://cakebuild.net/download/bootstrapper/windows -OutFile build.ps1

Now open the build.cake file, and replace the placeholders with values for your Raspberry Pi's IP address, the destination directory, and the username you use to log into your Pi.
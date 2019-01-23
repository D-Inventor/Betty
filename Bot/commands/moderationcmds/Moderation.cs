using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Betty.commands
{
	public partial class Moderation : ModuleBase<SocketCommandContext>
	{
		public IServiceProvider services { get; set; }
		Settings settings;
		Constants constants;
		DateTimeUtils datetimeutils;
		Agenda agenda;

		public Moderation(IServiceProvider services)
		{
			this.services = services;
			settings = services.GetService<Settings>();
			constants = services.GetService<Constants>();
			datetimeutils = services.GetService<DateTimeUtils>();
			agenda = services.GetService<Agenda>();
		}

		[Command("set"), Summary("Sets a given parameter to a new value")]
		public async Task set([Remainder]string input = null)
		{
			// make sure that the input is not empty
			if(input == null)
			{
				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.mod.error"));
				return;
			}

			// get keyword and arguments
			string keyword, residue;
			GetKeyValue(input, out keyword, out residue);

			switch (keyword)
			{
				case "notification":
					await set_notification();
					break;
				case "timezones":
					await set_timezones();
					break;
				case "public":
					await set_public();
					break;
				default:
					await error();
					break;
			}
		}

		[Command("unset"), Summary("Sets a given parameter to a new value")]
		public async Task unset([Remainder]string input = null)
		{
			// make sure that the input is not empty
			if (input == null)
			{
				await error();
				return;
			}

			// get keyword and arguments
			string keyword, residue;
			GetKeyValue(input, out keyword, out residue);

			switch (keyword)
			{
				case "public":
					await unset_public();
					break;
				case "notification":
					await unset_notification();
					break;
				case "timezones":
					await unset_timezones();
					break;
				default:
					await error();
					break;
			}
		}

		private async Task error(string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.mod.error"));
		}

		private void GetKeyValue(string input, out string keyword, out string residue)
		{
			int keywordlength = input.IndexOf(' ');
			if (keywordlength > -1)
			{
				keyword = input.Substring(0, keywordlength).ToLower();
				residue = input.Substring(++keywordlength);
			}
			else
			{
				keyword = input.ToLower();
				residue = "";
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Betty.utilities;
using Betty.databases.guilds;

namespace Betty.commands
{
	[Group("set"), Summary("Sets a given setting to a given value")]
	public partial class Set : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Constants constants;
		Logger logger;

		public Set(IServiceProvider services)
		{
			this.statecollection = services.GetService<StateCollection>();
			this.constants = services.GetService<Constants>();
			this.logger = services.GetService<Logger>();
		}

        [RequireContext(ContextType.Guild)]
        [Command("public"), Summary("Sets the channel of execution as the public channel")]
		public async Task set_public([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "set public", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				// apply change and report to user
				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				var language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Admin, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				gtb.Public = Context.Channel.Id;
				statecollection.SetGuildEntry(gtb, database);
				await Context.Channel.SendMessageAsync(language.GetString("command.set.public"));
			}
		}

        [RequireContext(ContextType.Guild)]
        [Command("notification"), Alias("notifications"), Summary("Sets the channel of execution as the notification channel")]
		public async Task set_notification([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "set notification", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				// apply change and report to user
				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				var language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Admin, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				gtb.Notification = Context.Channel.Id;
				statecollection.SetGuildEntry(gtb, database);
				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild, database).GetString("command.set.notification"));
			}
		}

        [RequireContext(ContextType.Guild)]
        [Command("timezones"), Alias("timezone"), Summary("Goes through all the timezones and creates/updates them accordingly")]
		public async Task set_timezones([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "set timezones", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				var language = statecollection.GetLanguage(Context.Guild, database);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Admin, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				await Context.Channel.SendMessageAsync(language.GetString("command.set.timezones.wait"));

				// find all the current roles in the guild
				IEnumerable<SocketRole> present = from role in Context.Guild.Roles
												  where DateTimeMethods.IsTimezone(role.Name)
												  select role;

				// add all the timezones that are not present already
				foreach (string t in DateTimeMethods.Timezones())
				{
					if (!present.Any(x => x.Name == t))
					{
						var role = await Context.Guild.CreateRoleAsync(t, isHoisted: false, permissions: constants.RolePermissions);
						await role.ModifyAsync(x =>
						{
							x.Mentionable = false;
						});
					}
				}

				// update the roles of the timezones that áre present
				foreach(SocketRole sr in present)
				{
					await sr.ModifyAsync((x) =>
					{
						x.Permissions = constants.RolePermissions;
						x.Mentionable = false;
					});
				}

				// return success to the user
				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild, database).GetString("command.set.timezones.done"));
			}
		}

        [RequireContext(ContextType.Guild)]
        [Command("permission"), Alias("permissions"), Summary("Sets a permission for a certain person")]
		public async Task set_permission(SocketGuildUser user, string permissionstr, [Remainder]string rest = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "set permission", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				StringConverter language = statecollection.GetLanguage(Context.Guild, database, gtb);

				//convert string permission to byte
				byte permission = PermissionHelper.StringToPermission(permissionstr);
				byte target_permission = PermissionHelper.GetUserPermission(user, database);

				// make sure that the caller has the proper permission
				if(!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, (byte)(Math.Max(permission, target_permission) << 1), database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				// now actually perform the command
				PermissionHelper.SetUserPermission(user, permission, database, gtb);

				// return success
				await Context.Channel.SendMessageAsync(language.GetString("command.set.permission"));
			}
		}

        [RequireContext(ContextType.Guild)]
        [Command("permission"), Alias("permissions"), Summary("Sets the permission for a specific role.")]
		public async Task set_permission(SocketRole role, string permissionstr, [Remainder]string rest = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "set permission", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				StringConverter language = statecollection.GetLanguage(Context.Guild, database, gtb);

				byte permission = PermissionHelper.StringToPermission(permissionstr);
				byte target_permission = PermissionHelper.GetRolePermission(role, database);

				// make sure that the calling user has the right permission to perform this command
				if(!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, Math.Max(permission, target_permission), database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				// execute the command
				PermissionHelper.SetRolePermission(role, permission, database, gtb);

				// return success
				await Context.Channel.SendMessageAsync(language.GetString("command.set.permission"));
			}
		}

        [RequireContext(ContextType.Guild)]
        [Command("language"), Alias("lang"), Summary("Sets the language of given guild to given language")]
		public async Task set_language(string newlang)
		{
			using(var database = new GuildDB())
			{
				// log execution
				CommandMethods.LogExecution(logger, "set language", Context);

				// indicate that the command is being worked on
				await Context.Channel.TriggerTypingAsync();

				// grab the guild language
				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				var language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that the user has the right permissions
				if(!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Admin, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				// set the language
				if(!statecollection.SetLanguage(Context.Guild, newlang, database, gtb))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nolanguage"));
					return;
				}

				// indicate success
				language = statecollection.GetLanguage(Context.Guild, database, gtb);
				await Context.Channel.SendMessageAsync(language.GetString("command.set.language"));
			}
		}
	}
}

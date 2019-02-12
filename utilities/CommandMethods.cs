using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Betty.databases.guilds;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Betty.utilities
{
	public static class CommandMethods
	{
		public static void LogExecution(Logger logger, string command, SocketCommandContext Context)
		{
			logger.Log(new LogMessage(LogSeverity.Info, "Commands", $"Received '{command}' command from {Context.User.Username} in {Context.Guild.Name}:{Context.Channel.Name}"));
		}

		public static bool UserHasPrivilege(SocketGuildUser guilduser, Permission minimumpermission, GuildDB database)
		{
			switch (minimumpermission)
			{
				// if the permission is public, everyone can use it
				case Permission.Public:
					return true;

				// if the permission is owner, then only the owner of the guild can use it
				case Permission.Owner:
					return guilduser.Guild.OwnerId == guilduser.Id;

				// otherwise, find the proper permission in the database and compare.
				default:
					Permission p = GetUserPermission(guilduser, database);
					return p >= minimumpermission;
			}
		}

		private static Permission GetUserPermission(SocketGuildUser guilduser, GuildDB database)
		{
			// check if user is owner
			if (guilduser.Id == guilduser.Guild.OwnerId) return Permission.Owner;

			// check if this user has a specific permission for themselves
			var userperm = (from p in database.Permissions
						   where p.Guild.GuildId == guilduser.Guild.Id
									&& p.PermissionType == PermissionType.User
									&& p.PermissionTarget == guilduser.Id
						   select p).FirstOrDefault();

			// personal permissions always have priority over role specific permissions
			if (userperm != null) return userperm.Permission;

			// get all the roles of this user
			var roles = guilduser.Roles;

			// find the highest permission from all the roles that this user has
			var roleperm = (from p in database.Permissions
							where p.PermissionType == PermissionType.Role && roles.Any(r => r.Id == p.PermissionTarget)
							orderby p.Permission descending
							select p).FirstOrDefault();

			// return the appropriate permission
			if (roleperm == null) return Permission.Public;
			return roleperm.Permission;
		}
	}

	public enum Permission
	{
		Public = 1,
		Member = 2,
		Admin = 4,
		Owner = 8
	}
}

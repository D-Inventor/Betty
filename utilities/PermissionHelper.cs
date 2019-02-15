using Betty.databases.guilds;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Betty.utilities
{
	public static class PermissionHelper
	{
		public const byte Public = 1;
		public const byte Member = 2;
		public const byte Admin  = 4;
		public const byte Owner = 8;

		public static bool UserHasPermission(SocketGuildUser guilduser, byte minimumpermission, GuildDB database) {
			switch (minimumpermission) {
				// if the permission is public, everyone can use it
				case Public:
					return true;

				// if the permission is owner, then only the owner of the guild can use it
				case Owner:
					return guilduser.Guild.OwnerId == guilduser.Id;

				// otherwise, find the proper permission in the database and compare.
				default:
					byte p = GetUserPermission(guilduser, database);
					return p >= minimumpermission;
			}
		}

		public static byte GetUserPermission(SocketGuildUser guilduser, GuildDB database) {
			// check if user is owner
			if (guilduser.Id == guilduser.Guild.OwnerId)
				return Owner;

			// check if this user has a specific permission for themselves
			var userperm = (from p in database.Permissions
							where p.Guild.GuildId == guilduser.Guild.Id
									 && p.PermissionType == PermissionType.User
									 && p.PermissionTarget == guilduser.Id
							select p).FirstOrDefault();

			// personal permissions always have priority over role specific permissions
			if (userperm != null)
				return userperm.Permission;

			// get all the roles of this user
			var roles = guilduser.Roles;

			// find the highest permission from all the roles that this user has
			var roleperm = (from p in database.Permissions
							where p.PermissionType == PermissionType.Role && roles.Any(r => r.Id == p.PermissionTarget)
							orderby p.Permission descending
							select p).FirstOrDefault();

			// return the appropriate permission
			if (roleperm == null)
				return Public;
			return roleperm.Permission;
		}

		public static byte GetRolePermission(SocketRole role, GuildDB database)
		{
			// find any permissions in the database
			PermissionTB dbresult = (from p in database.Permissions
									 where p.PermissionType == PermissionType.Role && p.Guild.GuildId == role.Guild.Id && p.PermissionTarget == role.Id
									 select p).FirstOrDefault();

			// if no permission was found, this role has public permission
			if (dbresult == null) return Public;

			// otherwise, return permission
			return dbresult.Permission;
		}

		public static bool SetUserPermission(SocketGuildUser user, byte permission, GuildDB database, GuildTB guildentry)
		{
			// First check if there is already a permission for this user
			PermissionTB p = (from perm in database.Permissions
							  where perm.Guild.GuildId == user.Guild.Id && perm.PermissionType == PermissionType.User && perm.PermissionTarget == user.Id
							  select perm).FirstOrDefault();

			if (p != null)
			{
				// if there is already a permission for given user, update it
				p.Permission = permission;
				database.Permissions.Update(p);
			}
			else
			{
				// if no permission is specified, create a new one
				p = new PermissionTB
				{
					Guild = guildentry,
					PermissionType = PermissionType.User,
					PermissionTarget = user.Id,
					Permission = permission,
				};
				database.Permissions.Add(p);
			}

			try
			{
				database.SaveChanges();
				return true;
			}
			catch(Exception)
			{
				return false;
			}
		}

		public static bool SetRolePermission(SocketRole role, byte permission, GuildDB database, GuildTB guildentry)
		{
			// First check if there is already a permission for this role
			PermissionTB p = (from perm in database.Permissions
							  where perm.Guild.GuildId == role.Guild.Id && perm.PermissionType == PermissionType.Role && perm.PermissionTarget == role.Id
							  select perm).FirstOrDefault();

			if (p != null)
			{
				// if there is already a permission for given role, update it
				p.Permission = permission;
				database.Permissions.Update(p);
			}
			else
			{
				// if no permission is specified, create a new one
				p = new PermissionTB
				{
					Guild = guildentry,
					PermissionType = PermissionType.Role,
					PermissionTarget = role.Id,
					Permission = permission,
				};
				database.Permissions.Add(p);
			}

			try
			{
				database.SaveChanges();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static byte StringToPermission(string permissionstr)
		{
			switch (permissionstr.ToLower())
			{
				case "admin":
					return Admin;
				case "public":
					return Public;
				case "member":
					return Member;
				case "owner":
					return Owner;
				default:
					throw new Exception($"Cannot parse '{permissionstr}' as a permission");
			}
		}
	}
}

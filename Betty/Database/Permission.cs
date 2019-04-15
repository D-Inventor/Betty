using System.ComponentModel.DataAnnotations;

namespace Betty.Database
{
    public class Permission
    {
        [Key]
        public ulong Id { get; set; }

        #region DiscordServer Foreign key
        [Required]
        public ulong DiscordServerId { get; set; }
        public DiscordServer DiscordServer { get; set; }
        #endregion

        [Required]
        public PermissionType Type { get; set; }

        [Required]
        public ulong Target { get; set; }

        [Required]
        public PermissionLevel Level { get; set; }
    }

    public enum PermissionType
    {
        User, Role
    }

    public enum PermissionLevel
    {
        Public, Member, Admin, Owner
    }
}

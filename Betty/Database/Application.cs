using System;
using System.ComponentModel.DataAnnotations;

namespace Betty.Database
{
    public class Application
    {
        #region DiscordServer foreign key
        [Required, Key]
        public ulong DiscordServerId { get; set; }
        public DiscordServer DiscordServer { get; set; }
        #endregion

        [Required]
        public ulong Channel { get; set; }

        [Required]
        public string Invite { get; set; }

        public DateTime? Deadline { get; set; }
    }
}

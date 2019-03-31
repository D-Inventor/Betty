using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Betty.databases.guilds;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Betty.WebAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class BettyController : ControllerBase
    {
        // GET: api/Betty
        [HttpGet]
        public IEnumerable<KeyValuePair<string, ulong>> Get()
        {
            using (var database = new GuildDB())
            {
                var dbresult = from g in database.Guilds
                               select new KeyValuePair<string, ulong>(g.Name, g.GuildId);

                return dbresult.ToArray();
            }
        }

        // GET: api/Betty/{guildid}/events
        [HttpGet("{id}/events")]
        public IEnumerable<EventAndNotifications> GuildEvents(ulong id)
        {
            using (var database = new GuildDB())
            {
                var dbresult = (from g in database.Guilds
                                where g.GuildId == id
                                select g.Events).FirstOrDefault();
                

                if (dbresult == null) return null;

                var output = dbresult.Select(x =>
                {
                    var notifications = (from n in database.EventNotifications
                                         where n.Event == x
                                         select new Notification { Date = n.Date, Response = n.ResponseKeyword }).ToArray();

                    return new EventAndNotifications { Event = new Event { Id = x.EventId, Date = x.Date, Name = x.Name }, Notifications = notifications };
                }).ToArray();

                return output;
            }
        }

        // GET: api/Betty/{guildid}/events/{eventid}
        [HttpGet("{gid}/events/{evid}")]
        public EventAndNotifications GetEventDetails(ulong gid, ulong evid)
        {
            using (var database = new GuildDB())
            {
                var events = (from g in database.Guilds
                              where g.GuildId == gid
                              select g.Events).FirstOrDefault();

                if (events == null) return null;

                EventTB ev = events.FirstOrDefault(x => x.EventId == evid);
                if (ev == null) return null;

                var notifications = (from n in database.EventNotifications
                                     where n.Event == ev
                                     select new Notification { Date = n.Date, Response = n.ResponseKeyword }).ToArray();

                return new EventAndNotifications { Event = new Event { Id = ev.EventId, Date = ev.Date, Name = ev.Name }, Notifications = notifications };
            }
        }

        [HttpGet("{gid}/permissions")]
        public IEnumerable<Permission> Permissions(ulong gid)
        {
            using(var database = new GuildDB())
            {
                var permissions = from p in database.Permissions
                                  where p.Guild.GuildId == gid
                                  select new Permission { Level = p.Permission, Target = p.PermissionTarget, Type = p.PermissionType };

                return permissions.ToArray();
            }
        }







        public class EventAndNotifications
        {
            public Event Event { get; set; }
            public IEnumerable<Notification> Notifications { get; set; }
        }

        public class Event
        {
            public ulong Id { get; set; }
            public string Name { get; set; }
            public DateTime Date { get; set; }
        }

        public class Notification
        {
            public string Response { get; set; }
            public DateTime Date { get; set; }
        }

        public class Permission
        {
            public PermissionType Type { get; set; }
            public ulong Target { get; set; }
            public byte Level { get; set; }
        }
    }
}

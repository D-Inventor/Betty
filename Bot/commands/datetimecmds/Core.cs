using System;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

namespace Betty.commands
{
	public partial class DateConvert : ModuleBase<SocketCommandContext>
	{
		public IServiceProvider services { get; set; }
		Settings settings;
		DateTimeUtils datetimeutils;

		public DateConvert(IServiceProvider services)
		{
			this.services = services;
			this.settings = services.GetService<Settings>();
			datetimeutils = services.GetService<DateTimeUtils>();
		}
	}
}

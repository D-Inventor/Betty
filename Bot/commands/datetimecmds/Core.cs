using System;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

namespace Betty.commands
{
	public partial class DateConvert : ModuleBase<SocketCommandContext>
	{
		public IServiceProvider services { get; set; }
		StateCollection statecollection;

		public DateConvert(IServiceProvider services)
		{
			this.services = services;
			this.statecollection = services.GetService<StateCollection>();
		}
	}
}

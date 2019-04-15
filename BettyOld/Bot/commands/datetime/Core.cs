using System;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

namespace Betty.commands
{
	public partial class DateConvert : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Logger logger;
		Agenda agenda;
		Constants constants;

		public DateConvert(IServiceProvider services)
		{
			this.statecollection = services.GetService<StateCollection>();
			logger = services.GetService<Logger>();
			agenda = services.GetService<Agenda>();
			constants = services.GetService<Constants>();
		}
	}
}

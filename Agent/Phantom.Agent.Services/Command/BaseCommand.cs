using Phantom.Agent.Command;

namespace Phantom.Agent.Services.Command; 

abstract record BaseCommand<TResult> : Command<AgentServices, CommandListener, TResult>;

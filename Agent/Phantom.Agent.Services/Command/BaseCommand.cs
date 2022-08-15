using Phantom.Agent.Command;

namespace Phantom.Agent.Services.Command; 

public abstract record BaseCommand<TResult> : Command<AgentServices, CommandListener, TResult>;

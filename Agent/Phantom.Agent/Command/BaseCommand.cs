namespace Phantom.Agent.Command; 

abstract record BaseCommand<TResult> : Command<AgentServices, CommandListener, TResult>;

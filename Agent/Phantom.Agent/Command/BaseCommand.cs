namespace Phantom.Agent.Command; 

abstract class BaseCommand<TResult> : Command<AgentServices, CommandListener, TResult> {}

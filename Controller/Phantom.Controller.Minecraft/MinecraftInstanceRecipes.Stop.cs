using System.Collections.Immutable;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Agent.Instance.Stop;

namespace Phantom.Controller.Minecraft;

public sealed partial class MinecraftInstanceRecipes {
	private static readonly ushort[] Stops = [60, 30, 10, 5, 4, 3, 2, 1];
	private static readonly IInstanceValue StopCommand = new InstanceValues.Text("stop");
	
	private static InstanceValues.Text SayCommand(string message) {
		return new InstanceValues.Text($"say {message}");
	}
	
	private static InstanceValues.Text SayCountDownAnnouncementCommand(ushort seconds) {
		return SayCommand("Server shutting down in " + seconds + (seconds == 1 ? " second." : " seconds."));
	}
	
	public InstanceStopRecipe Stop(ushort afterSeconds) {
		var steps = ImmutableArray.CreateBuilder<IInstanceStopStep>();
		
		if (afterSeconds > 0) {
			steps.Add(new InstanceStopStep.SendToStandardInput(SayCountDownAnnouncementCommand(afterSeconds)));
			
			int remainingSeconds = afterSeconds;
			
			for (int stopIndex = Array.FindIndex(Stops, stop => stop < afterSeconds); stopIndex != -1 && stopIndex < Stops.Length; stopIndex++) {
				ushort currentStop = Stops[stopIndex];
				
				steps.Add(new InstanceStopStep.Wait(TimeSpan.FromSeconds(remainingSeconds - currentStop)));
				steps.Add(new InstanceStopStep.SendToStandardInput(SayCountDownAnnouncementCommand(currentStop)));
				
				remainingSeconds = currentStop;
			}
			
			steps.Add(new InstanceStopStep.Wait(TimeSpan.FromSeconds(remainingSeconds)));
		}
		
		return new InstanceStopRecipe(steps.ToImmutable(), StopCommand);
	}
}

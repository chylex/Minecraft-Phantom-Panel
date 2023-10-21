using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Users;

namespace Phantom.Web.Utils;

static class Messages {
	public static string ToSentences(this AddUserError error, string delimiter) {
		return error switch {
			AddUserError.NameIsEmpty         => "Name cannot be empty.",
			AddUserError.NameIsTooLong e     => "Name cannot be longer than " + e.MaximumLength + " character(s).",
			AddUserError.NameAlreadyExists   => "Name is already occupied.",
			AddUserError.PasswordIsInvalid e => string.Join(delimiter, e.Violations.Select(static v => v.ToSentence())),
			_                                => "Unknown error."
		};
	}

	public static string ToSentences(this SetUserPasswordError error, string delimiter) {
		return error switch {
			SetUserPasswordError.UserNotFound        => "User not found.",
			SetUserPasswordError.PasswordIsInvalid e => string.Join(delimiter, e.Violations.Select(static v => v.ToSentence())),
			_                                        => "Unknown error."
		};
	}

	public static string ToSentence(this PasswordRequirementViolation violation) {
		return violation switch {
			PasswordRequirementViolation.TooShort v              => "Password must be at least " + v.MinimumLength + " character(s) long.",
			PasswordRequirementViolation.LowercaseLetterRequired => "Password must contain a lowercase letter.",
			PasswordRequirementViolation.UppercaseLetterRequired => "Password must contain an uppercase letter.",
			PasswordRequirementViolation.DigitRequired           => "Password must contain a digit.",
			_                                                    => "Unknown error."
		};
	}

	public static string ToSentence(this JvmArgumentsHelper.ValidationError? result) {
		return result switch {
			JvmArgumentsHelper.ValidationError.InvalidFormat => "Invalid format.",
			JvmArgumentsHelper.ValidationError.XmxNotAllowed => "The -Xmx argument must not be specified manually.",
			JvmArgumentsHelper.ValidationError.XmsNotAllowed => "The -Xms argument must not be specified manually.",
			_                                                => throw new ArgumentOutOfRangeException(nameof(result), result, null)
		};
	}

	public static string ToSentence(this LaunchInstanceResult reason) {
		return reason switch {
			LaunchInstanceResult.LaunchInitiated          => "Launch initiated.",
			LaunchInstanceResult.InstanceAlreadyLaunching => "Instance is already launching.",
			LaunchInstanceResult.InstanceAlreadyRunning   => "Instance is already running.",
			LaunchInstanceResult.InstanceLimitExceeded    => "Agent does not have any more available instances.",
			LaunchInstanceResult.MemoryLimitExceeded      => "Agent does not have enough available memory.",
			_                                             => "Unknown error."
		};
	}

	public static string ToSentence(this InstanceLaunchFailReason reason) {
		return reason switch {
			InstanceLaunchFailReason.ServerPortNotAllowed                   => "Server port not allowed.",
			InstanceLaunchFailReason.ServerPortAlreadyInUse                 => "Server port already in use.",
			InstanceLaunchFailReason.RconPortNotAllowed                     => "Rcon port not allowed.",
			InstanceLaunchFailReason.RconPortAlreadyInUse                   => "Rcon port already in use.",
			InstanceLaunchFailReason.JavaRuntimeNotFound                    => "Java runtime not found.",
			InstanceLaunchFailReason.CouldNotDownloadMinecraftServer        => "Could not download Minecraft server.",
			InstanceLaunchFailReason.CouldNotConfigureMinecraftServer       => "Could not configure Minecraft server.",
			InstanceLaunchFailReason.CouldNotPrepareMinecraftServerLauncher => "Could not prepare Minecraft server launcher.",
			InstanceLaunchFailReason.CouldNotStartMinecraftServer           => "Could not start Minecraft server.",
			_                                                               => "Unknown error."
		};
	}

	public static string ToSentence(this SendCommandToInstanceResult reason) {
		return reason switch {
			SendCommandToInstanceResult.Success => "Command sent.",
			_                                   => "Unknown error."
		};
	}

	public static string ToSentence(this StopInstanceResult reason) {
		return reason switch {
			StopInstanceResult.StopInitiated           => "Stopping initiated.",
			StopInstanceResult.InstanceAlreadyStopping => "Instance is already stopping.",
			StopInstanceResult.InstanceAlreadyStopped  => "Instance is already stopped.",
			_                                          => "Unknown error."
		};
	}
}

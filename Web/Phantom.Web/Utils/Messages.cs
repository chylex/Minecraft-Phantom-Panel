using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Minecraft;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Utils;

static class Messages {
	public static string ToSentences(this AddUserError error, string delimiter) {
		return error switch {
			Common.Data.Web.Users.AddUserErrors.NameIsInvalid e     => e.Violation.ToSentence(),
			Common.Data.Web.Users.AddUserErrors.PasswordIsInvalid e => string.Join(delimiter, e.Violations.Select(static v => v.ToSentence())),
			Common.Data.Web.Users.AddUserErrors.NameAlreadyExists   => "Username is already occupied.",
			_                                                       => "Unknown error."
		};
	}

	public static string ToSentences(this SetUserPasswordError error, string delimiter) {
		return error switch {
			Common.Data.Web.Users.SetUserPasswordErrors.UserNotFound        => "User not found.",
			Common.Data.Web.Users.SetUserPasswordErrors.PasswordIsInvalid e => string.Join(delimiter, e.Violations.Select(static v => v.ToSentence())),
			_                                                               => "Unknown error."
		};
	}

	public static string ToSentence(this UsernameRequirementViolation violation) {
		return violation switch {
			Common.Data.Web.Users.UsernameRequirementViolations.IsEmpty   => "Username must not be empty.",
			Common.Data.Web.Users.UsernameRequirementViolations.TooLong v => "Username must not be longer than " + v.MaxLength + " character(s).",
			_                                                             => "Unknown error."
		};
	}

	public static string ToSentence(this PasswordRequirementViolation violation) {
		return violation switch {
			Common.Data.Web.Users.PasswordRequirementViolations.TooShort v                 => "Password must be at least " + v.MinimumLength + " character(s) long.",
			Common.Data.Web.Users.PasswordRequirementViolations.MustContainLowercaseLetter => "Password must contain a lowercase letter.",
			Common.Data.Web.Users.PasswordRequirementViolations.MustContainUppercaseLetter => "Password must contain an uppercase letter.",
			Common.Data.Web.Users.PasswordRequirementViolations.MustContainDigit           => "Password must contain a digit.",
			_                                                                              => "Unknown error."
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
			LaunchInstanceResult.ServerPortNotAllowed     => "Server port not allowed.",
			LaunchInstanceResult.ServerPortAlreadyInUse   => "Server port already in use.",
			LaunchInstanceResult.RconPortNotAllowed       => "Rcon port not allowed.",
			LaunchInstanceResult.RconPortAlreadyInUse     => "Rcon port already in use.",
			_                                             => "Unknown error."
		};
	}

	public static string ToSentence(this InstanceLaunchFailReason reason) {
		return reason switch {
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
			SendCommandToInstanceResult.Success            => "Command sent.",
			SendCommandToInstanceResult.InstanceNotRunning => "Instance is not running.",
			_                                              => "Unknown error."
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

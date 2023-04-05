namespace Phantom.Utils.IO; 

public static class Chmod {
	public const UnixFileMode URWX = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
	public const UnixFileMode URW_GR = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead;
	public const UnixFileMode URWX_GRX = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute;
}

namespace WuXueMaJong.Core
{
    public sealed class CommandResult
    {
        public bool Success { get; }
        public string Error { get; }

        private CommandResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        public static CommandResult Ok()
        {
            return new CommandResult(true, null);
        }

        public static CommandResult Fail(string error)
        {
            return new CommandResult(false, error);
        }
    }
}

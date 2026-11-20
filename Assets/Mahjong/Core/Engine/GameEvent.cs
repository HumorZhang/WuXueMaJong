namespace WuXueMaJong.Core
{
    public sealed class GameEvent
    {
        public string Type { get; }
        public int Seat { get; }
        public string Message { get; }

        public GameEvent(string type, int seat, string message)
        {
            Type = type;
            Seat = seat;
            Message = message;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}

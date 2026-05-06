namespace CementGB.NetBeardModule.SteamForwarding.Exceptions;

public class SteamException : Exception
{
    public SteamException()
    {
    }

    public SteamException(string message) : base(message)
    {
    }
}
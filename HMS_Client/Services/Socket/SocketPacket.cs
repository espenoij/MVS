public class SocketPacket
{
    public string clientID { get; set; }
    public PacketCommand command { get; set; }
    public string payload { get; set; }
    public uint crcHash { get; set; }

    public SocketPacket()
    {
        clientID = string.Empty;
        command = PacketCommand.None;
        payload = string.Empty;
        crcHash = 0;
    }
}

public enum PacketCommand
{
    None,
    GetDataUpdate,
    GetSensorStatus,
    SetUserInputs,
    ClientDenied
}

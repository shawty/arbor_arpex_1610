namespace Arpex
{
  public enum OsType
  {
    Windows = 1,
    Linux
  }

  public enum OperationType : byte
  {
    Read = 1,
    Write = 2,
    ReadAck = 3,
    WriteAck = 4
  }

  public enum CommandType : byte
  {
    InitialiseSystem = 0x81,  // -127
    DigitalIO = 0x82,         // -126
    DigitalIOInverted = 0x83, // -125
    AnalogIO = 0x84           // -124
  }

}

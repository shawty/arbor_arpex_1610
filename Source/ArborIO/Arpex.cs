using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Arpex
{
  public class IO
  {
    const string WindowsPortName = "COM5";
    const string LinuxPortName = "/dev/ttyS4";
    const string ControllerPassPhrase = "ARBOR";

    private readonly OsType _selectedOsType;
    private SerialPort _ioControllerPort;

    private bool commandWaiting = false; // Set this to true when sending a command to the serial port, otherwise receive event will just discard data
    private byte[] commandRecieveBuffer = new byte[32]; // Response packets are always 32 bytes in length
    private byte commandRecieveBufferOffset = 0; // We always start at the beginning of the buffer
    
    public IO(OsType osType)
    {
      _selectedOsType = osType;
    }

    public void Startup()
    {
      // Do startup tasks here
      InitializeSerialDevice();
      InitialiseController(ControllerPassPhrase);

    }

    public void Shutdown()
    {
      // Do shutdown tasks here
      ShutdownSerialDevice();

    }

    private string GetPortName()
    {
      return _selectedOsType == OsType.Windows ? WindowsPortName : LinuxPortName;
    }

    //private void SerialRecieveEvent(object sender, SerialDataReceivedEventArgs e)
    private void SerialRecieveEvent(byte[] recievedData)
    {
      // Called by the serial IO system when data is received by the opened serial port

      //byte byteCount = (byte)_ioControllerPort.BytesToRead;
      byte byteCount = (byte)recievedData.Length; // Hard coded in our own handler
      if (!commandWaiting)
      {
        // If there is no command waiting for the data we received then we just expunge it
        //byte[] waste = new byte[byteCount];
        //_ioControllerPort.Read(waste, 0, byteCount);
        return;
      }

      // Get the data received into our command packet buffer
      //_ioControllerPort.Read(commandRecieveBuffer, commandRecieveBufferOffset, byteCount);
      Array.ConstrainedCopy(recievedData, 0, commandRecieveBuffer, commandRecieveBufferOffset, byteCount);
      commandRecieveBufferOffset += byteCount; // increase the offset so next chunk of data is added correctly (Responses are always 32 bytes)

      if (commandRecieveBufferOffset >= 32) // If we've received 32 bytes, then process the full buffer
      {
        // We've filled the buffer, so we toggle the command waiting flag back off
        // the sender is waiting for this to revert back to false, so it'll notice
        // when it's toggled.
        commandWaiting = false;
      }

    }

    private void InitializeSerialDevice()
    {
      _ioControllerPort = new SerialPort(GetPortName(), 9600, Parity.None, 8, StopBits.One);
      //_ioControllerPort.DataReceived += SerialRecieveEvent;
      _ioControllerPort.Open();

      Stream portBase = _ioControllerPort.BaseStream;
      Action dataReceivedHandler = null;

      dataReceivedHandler = () => {
        byte[] recevieBuffer = new byte[8];
        AsyncCallback callback = asyncResult => {
          int byteCount = portBase.EndRead(asyncResult);
          byte[] bufferToSend = new byte[byteCount];
          Array.Copy(recevieBuffer, bufferToSend, byteCount);
          SerialRecieveEvent(bufferToSend);
          dataReceivedHandler();
        };
        portBase.BeginRead(recevieBuffer, 0, 8, callback, null);
      };
      dataReceivedHandler();

    }

    private void ShutdownSerialDevice()
    {
      if (_ioControllerPort.IsOpen)
      {
        _ioControllerPort.Close();
      }
      //_ioControllerPort.DataReceived -= SerialRecieveEvent;
    }

    private void SendSerialIoCommandPacket(byte[] commandPacket, Action<byte[]> responseHandler)
    {
      if (!_ioControllerPort.IsOpen)
      {
        throw new IoControlPortNotOpenException();
      }

      if (commandWaiting == true)
      {
        // if another command is already waiting we return false indicating not sent
        throw new CommandWaitingException();
      }

      commandRecieveBuffer = new byte[32]; // Create an empty buffer
      commandRecieveBufferOffset = 0; // set the buffer offset to 0
      commandWaiting = true; // set the command waiting flag so the receive event saves the data

      _ioControllerPort.Write(commandPacket, 0, 32); // Command packets are always 32 bytes in length
      while (commandWaiting) { } // wait for the receive event to reset the command waiting flag
      responseHandler(commandRecieveBuffer); // then send the received data to the response handler

    }

    private byte[] CreateCommandBuffer(OperationType operationType, CommandType command)
    {
      byte[] buffer = new byte[32];
      buffer[0] = 0x7B;
      buffer[1] = 0x23;
      buffer[2] = (byte)operationType;
      buffer[3] = (byte)command;
      buffer[30] = 0x23;
      buffer[31] = 0x7D;
      return buffer;
    }

    private void InitialiseController(string passPhrase)
    {
      if (passPhrase.Length > 25)
      {
        throw new PassPhraseToLargeException(passPhrase.Length);
      }

      byte[] passPhraseBytes = Encoding.ASCII.GetBytes(passPhrase);
      byte[] commandBuffer = CreateCommandBuffer(OperationType.Write, CommandType.InitialiseSystem);
      commandBuffer[4] = (byte)passPhrase.Length;
      Array.ConstrainedCopy(passPhraseBytes, 0, commandBuffer, 5, passPhrase.Length);

      byte[] responseBuffer = new byte[32]; // Responses from the IO system like the commands are always 32 bytes
      SendSerialIoCommandPacket(commandBuffer, (recievedBytes) =>
      {
        Array.Copy(recievedBytes, responseBuffer, 32); // Copy the received packet into our own buffer
      });

      // Because the send serial method waits until the data receive event clears the command waiting flag, we can
      // be sure that by the time we get to this point we actually have a valid received packet from the IO controller

      // If our initialization was successful the byte at offset 4 in our response buffer should be 0x01, if it's
      // not then our init failed.  0xFF has been seen for an incorrect pass phrase I'm unaware if there are any others
      if (responseBuffer[4] != 0x01)
      {
        throw new InitializationFailedException();
      }

    }

    private void ShowBufferAsHex(byte[] buffer)
    {
      Console.WriteLine("0x" + BitConverter.ToString(buffer).Replace("-", " 0x"));
    }

    public byte ReadDigitalInputs()
    {
      byte result = 0;

      byte[] commandBuffer = CreateCommandBuffer(OperationType.Read, CommandType.DigitalIO);
      byte[] responseBuffer = new byte[32]; // Responses from the IO system like the commands are always 32 bytes
      SendSerialIoCommandPacket(commandBuffer, (recievedBytes) =>
      {
        Array.Copy(recievedBytes, responseBuffer, 32); // Copy the received packet into our own buffer
      });

      // If we get to here input line status is in the top nibble of byte 5
      // 0x10 = DI0 (bit 4 - 16 decimal)
      // 0x20 = DI1 (bit 5 - 32 decimal)
      // 0x30 = DI2 (bit 6 - 64 decimal)
      // 0x40 = DI3 (bit 7 - 128 decimal)
      // Or combinations thereof, EG: 0xF0 means ALL 4 lines are active
      result = responseBuffer[5];

      return result;
    }

    public void SetDigitalOutputs(byte outValue)
    {
      byte[] commandBuffer = CreateCommandBuffer(OperationType.Write, CommandType.DigitalIO);

      commandBuffer[4] = 0x01; // I believe this represents data length

      // Only the lower 5 bits matter
      // 4 output lines, the remaining bits:
      // 0x01 = DO0 (bit 0 - 1 decimal)
      // 0x02 = DO1 (bit 1 - 2 decimal)
      // 0x03 = DO2 (bit 2 - 4 decimal)
      // 0x04 = DO3 (bit 3 - 8 decimal)
      // Or combinations thereof, EG: 0x0F means ALL 4 lines are active
      commandBuffer[5] = (byte)(outValue & 0x0F);

      byte[] responseBuffer = new byte[32]; // Responses from the IO system like the commands are always 32 bytes
      SendSerialIoCommandPacket(commandBuffer, (recievedBytes) =>
      {
        Array.Copy(recievedBytes, responseBuffer, 32); // Copy the received packet into our own buffer
      });

    }

    public void ReadAnalog(out int AnalogOne, out int AnalogTwo)
    {
      byte[] commandBuffer = CreateCommandBuffer(OperationType.Read, CommandType.AnalogIO);
      byte[] responseBuffer = new byte[32]; // Responses from the IO system like the commands are always 32 bytes

      SendSerialIoCommandPacket(commandBuffer, (recievedBytes) =>
      {
        Array.Copy(recievedBytes, responseBuffer, 32); // Copy the received packet into our own buffer
      });

      // Analog read returns 6 bytes, a 0x04 in what appears to be the length position
      // followed by a 0x00 followed by what appears to be 2 16 bit (2 byte) values
      // the first value appears to be the A1+ input and hovers between just over 32,000 and 65,000
      // the second value I suspect is the A1- input but if I connect that to an external 5V it
      // grounds a saps the power from the supply but never changes, hovering at around 32,000

      AnalogOne = (responseBuffer[7] << 8) + responseBuffer[6];
      AnalogTwo = (responseBuffer[9] << 8) + responseBuffer[8];
    }

    public void SendArbitraryCommand(byte operationType, byte commandType)
    {
      byte[] commandBuffer = CreateCommandBuffer((OperationType)operationType, (CommandType)commandType); // We need to cast to correct type
      byte[] responseBuffer = new byte[32]; // Responses from the IO system like the commands are always 32 bytes

      Console.WriteLine("================================================================");
      Console.WriteLine("ARBITRARY COMMAND SENDER");
      Console.WriteLine("Send buffer");
      ShowBufferAsHex(commandBuffer);

      SendSerialIoCommandPacket(commandBuffer, (recievedBytes) =>
      {
        Array.Copy(recievedBytes, responseBuffer, 32); // Copy the received packet into our own buffer
      });

      Console.WriteLine("Received buffer");
      ShowBufferAsHex(responseBuffer);
    }

  }
}

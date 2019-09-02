using Arpex;
using System;

namespace readdigitaltest
{
  class Program
  {
    static byte currentOutputLines = 0x00;

    static IO myIoSystem;

    static void Main(string[] args)
    {
      myIoSystem = new IO(OsType.Windows);
      myIoSystem.Startup();

      Console.WriteLine("ArborIO - Digital Read Test");
      Console.WriteLine("Monitoring Digital Input Lines (Press 'Q' to quit, 1 to 4 to toggle outputs)");

      // do digital stuff here
      bool quitFlag = false;
      while (!quitFlag)
      {
        byte DI = myIoSystem.ReadDigitalInputs();
        if ((DI & 16) == 16) Console.WriteLine("DI0 is active");
        if ((DI & 32) == 32) Console.WriteLine("DI1 is active");
        if ((DI & 64) == 64) Console.WriteLine("DI2 is active");
        if ((DI & 128) == 128) Console.WriteLine("DI3 is active");

        //int A1 = 0;
        //int A2 = 0;
        //myIoSystem.ReadAnalog(out A1, out A2);
        //Console.WriteLine("{0},{1}",A1,A2);

        if (Console.KeyAvailable)
        {
          var theKey = Console.ReadKey(true).Key;

          switch (theKey)
          {
            case ConsoleKey.Q:
              quitFlag = true;
              break;

            case ConsoleKey.D1:
              currentOutputLines ^= 1;
              myIoSystem.SetDigitalOutputs(currentOutputLines);
              break;

            case ConsoleKey.D2:
              currentOutputLines ^= (1 << 1);
              myIoSystem.SetDigitalOutputs(currentOutputLines);
              break;

            case ConsoleKey.D3:
              currentOutputLines ^= (1 << 2);
              myIoSystem.SetDigitalOutputs(currentOutputLines);
              break;

            case ConsoleKey.D4:
              currentOutputLines ^= (1 << 3);
              myIoSystem.SetDigitalOutputs(currentOutputLines);
              break;

          }

        }
      }

      myIoSystem.Shutdown();
      Console.WriteLine("Finished, press enter...");
      Console.ReadLine();

    }
  }
}

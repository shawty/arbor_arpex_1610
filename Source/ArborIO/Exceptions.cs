using System;

namespace Arpex
{
  public class PassPhraseToLargeException : Exception
  {
    public int PassPhraseSize = 0;

    public PassPhraseToLargeException(int passPhraseSize) : base ("Pass Phrase size was to large, pass phrases must be 25 chars or less in length.")
    {
      PassPhraseSize = passPhraseSize;
    }

  }

  public class InitializationFailedException : Exception
  {
    public InitializationFailedException() : base("IO System initialization failed, check you used the correct pass phrase.")
    {
    }

  }

  public class CommandWaitingException : Exception
  {
    public CommandWaitingException() : base("Another command is waiting to be processed.")
    {
    }

  }

  public class IoControlPortNotOpenException : Exception
  {
    public IoControlPortNotOpenException() : base("Serial port for IO controller is not open or has not been initialized.")
    {
    }

  }

}

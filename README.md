# arbor_arpex_1610
Small library and demo program written in C# showing how to talk to the Digital IO system built into the now rather old Arbor Arpex-1610 industrial box PC.

In august 2019 I took on a small task (WHich turned out to be not so small) to recondition and program 2 10 year old specialized industrial box PC units, made by the Tiwaneese firm Arobor Technology.

The PC's themselves from a software point of view where fairly standard X86 32 Bit intel Atom based units, both capable of running most 32 bit versions of both Linux and Windows based operating systems.

After replacing the hard drives in them with new SSD's and installing Debian 8 on one device, and windows Embedded Compact 7 on the other, I then set about trying to work out how the I/O system on the devices was used.

Each unit has a thin green pin based connector on one side of the main chassis, 4 of the pins are Digital Inputs for reading switches or other binary based inputs.  The next along are 4 digital output's, they don't have a particulaly high current drive, and only just managed to deliver 5v but they are enough to switch a transistor to drive a higher power load.

Lastly there are 4 analog I/O ports, 2 negative and 2 positive, I'm not yet sure how the negative side works (My gut tells me it's some kind of a comparator) but the positive side sit's on a center measurment of around 32,000 with negative voltages dipping below, and positive pushing above, giving a 15 bit conversion resolution.

There was a requirement for me to produce some code to allow new software to be written that would interact with this I/O system.

Over the course of 3 weeks I set about trying to find out everything I could about these units, the company that produced them stopped marketing them quite some time back in favor of more modern up-to date models.

I tried various avenues poking about in the OS software, generally doing random port I/O and other crazy things, and examining the motherboard with a high power magnifer, and eventually came to the conclusion that the I/O system was connected to an internal COM port labeled as COM5, that was connected to the Uart connection of a Texas instruments M430F423 Microcontroller embedded on the mother board and connected to the Green I/O connectors via a bank of opto isolators.

I tried a lot of things, incuding just throwing random data at the COM5 port, but nothing I did seemed to make any difference.

I managed to reach out to Arbors UK office, and they where able to provide me with a user guide, and at a later date some demo software that used to ship with the units when they where sold brand new.

The demo software, used an old Win32 trick of dynamically loading a driver at runtime, as if it was a service managed by the service control manager, and was all written in a very old version of MIcrosoft C/C++, with the app itself being written using the MCF foundation classes.

However, after opening one of the units up and managing to attach some jumper wires to the RX/TX uart pins on the TI MCU, then running the software, I was with the aid of a logic probe finally able to see just exactly how the COM5 port was used to communicate with the host PC.

Along with a partial reverse engineering (Using IDA) of the binary code provided to me, I was finally able to crack the protocol used.

The code you'll find in this github repository contains a .NET DLL project and a simple console mode program demonstrating it's use.

The DLL Includes functions to read the digital inputs, write the digital outputs, read the analog inputs and initialise the entire I/O system ready for use.

I have more notes in a thread I started on the EEVBlog electronics forum that describes the protocol here : https://www.eevblog.com/forum/microcontrollers/industrial-pc-with-an-embedded-ti-m430f423-microcontroller/

The whole thing is written in portable C# code, and works on both Windows and Linux using the full .NET framework (Sorry not .NET core... wwell not yet anyway :-)   )

Iv'e tested it under both, and have written the data recived code so that it works around the problems in Mono with the datarecieve event not working correctly.

I suspect there are other industrial box PC's kicking around out there that will use a similar system, I'm putting this code here for anyone now or on the future that might find them sleves faced with this peculiar (or similar) device and may need to work out how to use it.

I may even at some point write a protocol document explaining everythig in detail.

Shawty

SESimulator Dev Environment
===========================

Copy of my script development environment, for backup purposes. Feel free to grab anything useful.

The VS projects expect to find the contents of the Space Engineers Bin or Bin64 directories in lib/se-binaries.

SESimulator was intended to simulate resource flows in Space Engineers, using data from the game files to make it accurate. At the time the source code for the game was not available and reverse-engineering actual game behaviour was difficult, so it never progressed beyond a simple game file parser. Now that the source code *is* available, there's not much point writing such a tool...

SESimulator is therefore a misnamed dev tool for scraping Space Engineers game files for various useful numbers and mangling them for presentation.

My refinery control script is also included here. It was the original reason for writing such a tool.
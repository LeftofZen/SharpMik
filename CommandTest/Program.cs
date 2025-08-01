using System;
using SharpMik.Player;
using SharpMik.Drivers;
using SharpMik;
using System.Diagnostics;
using System.Threading;
using SharpMik.Common;

namespace mikmodSpeedTest
{
	class Program
	{
		static int Main(string[] args)
		{
			ModPlayer.SetFixedRandom = true;
			var result = 0;
			var iterations = 0;
			string modName = null;
			try
			{
				var startTime = DateTime.Now;
				Module mod;

				if (args.Length == 0)
				{
					ModDriver.LoadDriver<NoAudio>();
					ModDriver.MikMod_Init("");
					mod = ModuleLoader.Load("Music1.mod");
					modName = "Music1.mod";
				}
				else
				{
					if (args.Length > 2)
					{
						ModDriver.LoadDriver<NoAudio>();
					}
					else
					{
						ModDriver.LoadDriver<WavDriver>();
					}

					ModDriver.MikMod_Init(args[1]);
					mod = ModuleLoader.Load(args[0]);
					modName = args[0];
				}

				var lookingForByte = -1;
				var byteCount = 44; // wav header

				var loadTime = DateTime.Now;

				if (mod != null)
				{
					mod.Loop = false;
					ModPlayer.Player_Start(mod);

					// Trap for wrapping mods.
					while (ModPlayer.Player_Active() && iterations < 5000)
					{
						if (lookingForByte > 0)
						{
							var test = byteCount + (int)WavDriver.BUFFERSIZE;
							if (test > lookingForByte)
							{
								Debug.WriteLine("Will fail on the next pass, at {0} byes in", lookingForByte - byteCount);
							}
						}

						if (args.Length == 0)
						{
							ModPlayer.Player_HandleTick();
						}
						else
						{
							ModDriver.MikMod_Update();
						}

						byteCount += (int)WavDriver.BUFFERSIZE;
						iterations++;
					}

					ModPlayer.Player_Stop();
					ModDriver.MikMod_Exit();
				}

				var span = DateTime.Now - startTime;

				var loadSpan = loadTime - startTime;

				while (args.Length == 0)
				{
					Console.WriteLine("Took {0} seconds in total for mod of {1} seconds", span.TotalSeconds, mod.SongTime / 1024);
					Console.WriteLine("Took {0} seconds to load and thus {1} seconds to process", loadSpan.TotalSeconds, span.TotalSeconds - loadSpan.TotalSeconds);

					Thread.Sleep(1000);
				}

			}
			catch (Exception ex)
			{
				result = -1;
				Console.WriteLine("Mod file " + modName + " Hit an execption:\n\titterations till error: " + iterations + "\n\t" + ex.Message);
			}

			return result;
		}
	}
}

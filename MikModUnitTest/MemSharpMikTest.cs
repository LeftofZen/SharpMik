using SharpMik;
using SharpMik.Common;
using SharpMik.Drivers;
using SharpMik.Extensions;
using SharpMik.Player;
using System;
using System.IO;
using System.Threading;

namespace MikModUnitTest
{
	public class MemDriver : VirtualSoftwareDriver
	{
		sbyte[] m_Audiobuffer;

		public static uint BUFFERSIZE = 32768;

		public MemoryStream MemStream { get; private set; }

		public MemDriver()
		{
			NextDriver = null;
			Name = "Mem Writer";
			Version = "Mem stream writer";
			HardVoiceLimit = 0;
			SoftVoiceLimit = 255;
			AutoUpdating = false;
		}

		public override void CommandLine(string command)
		{
		}

		public override bool IsPresent() => true;

		public override bool Init()
		{
			m_Audiobuffer = new sbyte[BUFFERSIZE];

			return base.Init();
		}

		public override void PlayStop() => base.PlayStop();

		public override bool PlayStart()
		{
			MemStream = new MemoryStream();
			return base.PlayStart();
		}

		public override void Exit()
		{

		}

		public override void Update()
		{
			var done = WriteBytes(m_Audiobuffer, BUFFERSIZE);
			MemStream.Write(m_Audiobuffer, 0, (int)done);
		}
	}

	public class MemSharpMikTest
	{
		string m_FileName;
		readonly MemDriver m_MemDriver;
		Module mod;

		readonly Thread m_Thread;
		bool m_Running;
		bool m_Working;

		readonly AutoResetEvent m_Blocker = new(false);

		public string ErrorMessage { get; private set; }

		public MemoryStream MemStream => m_MemDriver.MemStream;

		public float TimeTaken { get; private set; }

		public MemSharpMikTest()
		{
			ModPlayer.SetFixedRandom = true;
			m_MemDriver = ModDriver.LoadDriver<MemDriver>();
			_ = ModDriver.MikMod_Init("");
			m_Running = true;

			m_Thread = new Thread(new ThreadStart(WorkThread))
			{
				Name = "SharpTest",
				Priority = ThreadPriority.Highest
			};
			m_Thread.Start();
		}

		public void Start(string fileName)
		{
			m_FileName = fileName;
			ErrorMessage = null;
			m_Working = true;
			_ = m_Blocker.Set();
		}

		public void ShutDown()
		{
			m_FileName = null;
			m_Running = false;
			_ = m_Blocker.Set();
			m_Thread.Abort();
		}

		public bool IsRunning()
		{
			if (m_Thread != null && m_Running)
			{
				return m_Working;
			}

			return false;
		}

		void WorkThread()
		{
			while (m_Running)
			{
				_ = m_Blocker.WaitOne();
				if (m_FileName != null)
				{
					var startTime = DateTime.Now;
					try
					{
						mod = ModuleLoader.Load(m_FileName);
						var iterations = 0;

						if (mod != null)
						{
							mod.Loop = false;
							ModPlayer.Player_Start(mod);

							// Trap for wrapping mods.
							while (ModPlayer.Player_Active() && iterations < 5000)
							{
								ModDriver.MikMod_Update();
								iterations++;
							}

							ModPlayer.Player_Stop();

							ModuleLoader.UnLoad(mod);
						}
					}
					catch (Exception ex)
					{
						ErrorMessage = ex.Message;
					}

					var span = DateTime.Now - startTime;
					TimeTaken = (float)span.TotalSeconds;
				}

				m_Working = false;
			}
		}
	}
}

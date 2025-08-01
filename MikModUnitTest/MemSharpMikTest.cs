using System;
using System.IO;
using SharpMik.Drivers;

using SharpMik.Extentions;
using SharpMik.Player;
using SharpMik;
using System.Threading;
using SharpMik.Common;

namespace MikModUnitTest
{
	public class MemDriver : VirtualSoftwareDriver
	{
		MemoryStream m_MemoryStream;
		sbyte[] m_Audiobuffer;

		public static uint BUFFERSIZE = 32768;

		public MemoryStream MemStream => m_MemoryStream;

		public MemDriver()
		{
			m_Next = null;
			m_Name = "Mem Writer";
			m_Version = "Mem stream writer";
			m_HardVoiceLimit = 0;
			m_SoftVoiceLimit = 255;
			m_AutoUpdating = false;
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
			m_MemoryStream = new MemoryStream();
			return base.PlayStart();
		}

		public override void Exit()
		{

		}

		public override void Update()
		{
			var done = WriteBytes(m_Audiobuffer, BUFFERSIZE);
			m_MemoryStream.Write(m_Audiobuffer, 0, (int)done);
		}
	}

	public class MemSharpMikTest
	{
		string m_FileName;
		readonly MemDriver m_MemDriver;
		Module mod;

		readonly Thread m_Thread;
		string m_Error;

		float m_TimeTaken;

		bool m_Running;
		bool m_Working;

		readonly AutoResetEvent m_Blocker = new(false);

		public string ErrorMessage => m_Error;

		public MemoryStream MemStream => m_MemDriver.MemStream;

		public float TimeTaken => m_TimeTaken;

		public MemSharpMikTest()
		{
			ModPlayer.SetFixedRandom = true;
			m_MemDriver = ModDriver.LoadDriver<MemDriver>();
			ModDriver.MikMod_Init("");
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
			m_Error = null;
			m_Working = true;
			m_Blocker.Set();
		}

		public void ShutDown()
		{
			m_FileName = null;
			m_Running = false;
			m_Blocker.Set();
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
				m_Blocker.WaitOne();
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
						m_Error = ex.Message;
					}

					var span = DateTime.Now - startTime;
					m_TimeTaken = (float)span.TotalSeconds;
				}

				m_Working = false;
			}
		}
	}
}

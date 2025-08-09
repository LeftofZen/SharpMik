using SharpMik.Common;
using SharpMik.Player;

namespace SharpMik.Types
{
	public class BinaryModule
	{
		public bool m_Loaded;
		public Module m_Module;
		public static short[][] m_Samples;

		public BinaryModule() => m_Loaded = false;

		public BinaryModule(Module mod)
		{
			// Shouldn't need to clone it...
			m_Module = mod;

			m_Samples = new short[m_Module.Samples.Length][];

			for (var i = 0; i < m_Module.Samples.Length; i++)
			{
				m_Samples[i] = ModDriver.MD_GetSample((short)i);
			}
		}

		public void Load()
		{
			if (ModDriver.Driver != null)
			{
				for (var i = 0; i < m_Module.Samples.Length; i++)
				{
					m_Module.Samples[i].handle = ModDriver.MD_SetSample(m_Samples[i]);
				}

				_ = ModPlayer.Player_Init(m_Module);

				m_Loaded = true;
			}
		}
	}
}

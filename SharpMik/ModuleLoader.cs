using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SharpMik.Interfaces;
using System.Reflection;
using SharpMik.IO;
using SharpMik.Player;
using SharpMik.Common;
using Module = SharpMik.Common.Module;

namespace SharpMik
{

	/*
	 * Handles the mod loading and unloading, by the way of finding which loader should be used
	 * and asking it to load the basics of the module then do some extra setup after.
	 * 
	 * I don't see much need to change the basics of this file, the static nature of it should be fine.
	 */
	public class ModuleLoader
	{

		#region private static variables
		static readonly List<Type> s_RegistedModuleLoader = new();
		static bool s_HasAutoRegisted;
		#endregion

		#region static accessors
		static public bool UseBuiltInModuleLoaders { get; set; } = true;
		#endregion

		#region loader registration
		static public void BuildRegisteredModules()
		{
			if (!s_HasAutoRegisted && UseBuiltInModuleLoaders)
			{
				var list = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(IModLoader)));

				foreach (var type in list)
				{
					s_RegistedModuleLoader.Add(type);
				}

				s_HasAutoRegisted = false;
			}
		}

		public static void RegisterModuleLoader<T>() where T : IModLoader => s_RegistedModuleLoader.Add(typeof(T));
		#endregion

		#region Module Loading
		public static Module Load(string fileName)
		{
			//try
			{
				using (Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					return Load(stream, 64, 0);
				}
			}
			//catch (System.Exception ex)
			{
				//throw new Exception("Failed to open " + fileName,ex);
			}
		}

		public static Module Load(Stream stream, int maxchan, int curious)
		{
			BuildRegisteredModules();
			Module mod = null;

			var modReader = new ModuleReader(stream);
			IModLoader loader = null;

			for (var i = 0; i < s_RegistedModuleLoader.Count; i++)
			{
				modReader.Rewind();
				var tester = (IModLoader)Activator.CreateInstance(s_RegistedModuleLoader[i]);
				tester.ModuleReader = modReader;

				if (tester.Test())
				{
					loader = tester;
					tester.Cleanup();
					break;
				}

				tester.Cleanup();
			}

			if (loader != null)
			{

				var t = 0;
				mod = new Module();
				loader.Module = mod;

				var loaded = false;

				var track = new InternalModuleFormat();
				track.UniInit();
				loader.Tracker = track;

				mod.BpmLimit = 33;
				mod.InitialVolume = 128;

				for (t = 0; t < Constants.UF_MAXCHAN; t++)
				{
					mod.ChannelVolume[t] = 64;
					mod.Panning[t] = (ushort)((((t + 1) & 2) == 2) ? Constants.PAN_RIGHT : Constants.PAN_LEFT);
				}

				if (loader.Init())
				{
					modReader.Rewind();

					loaded = loader.Load(curious);

					if (loaded)
					{
						for (t = 0; t < mod.NumSamples; t++)
						{
							if (mod.Samples[t].inflags == 0)
							{
								mod.Samples[t].inflags = mod.Samples[t].flags;
							}
						}
					}
				}

				loader.Cleanup();
				track.UniCleanup();

				if (loaded)
				{
					ML_LoadSamples(mod, modReader);

					if ((mod.Flags & Constants.UF_PANNING) != Constants.UF_PANNING)
					{
						for (t = 0; t < mod.NumChannels; t++)
						{
							mod.Panning[t] = (ushort)((((t + 1) & 2) == 2) ? Constants.PAN_HALFRIGHT : Constants.PAN_HALFLEFT);
						}
					}

					if (maxchan > 0)
					{
						if ((mod.Flags & Constants.UF_NNA) != Constants.UF_NNA && (mod.NumChannels < maxchan))
						{
							maxchan = mod.NumChannels;
						}
						else
						{
							if ((mod.NumVoices != 0) && (mod.NumVoices < maxchan))
							{
								maxchan = mod.NumVoices;
							}
						}

						if (maxchan < mod.NumChannels)
						{
							mod.Flags |= Constants.UF_NNA;
						}

						if (ModDriver.MikMod_SetNumVoices_internal(maxchan, -1))
						{
							mod = null;
							return null;
						}
					}

					SampleLoader.SL_LoadSamples();

					ModPlayer.Player_Init(mod);
				}
				else
				{
					mod = null;
					LoadFailed(loader, null);
				}
			}
			else
			{
				throw new Exception("File {0} didn't match any of the loader types");
			}

			return mod;
		}

		private static void LoadFailed(IModLoader loader, Exception ex)
		{
			if (loader != null)
			{
				if (loader.LoadError != null)
				{
					throw new Exception(loader.LoadError, ex);
				}
			}

			throw new Exception("Failed to load", ex);
		}
		#endregion

		#region Common Load Implementation
		private static bool ML_LoadSamples(Module of, ModuleReader modreader)
		{
			int u;

			for (u = 0; u < of.NumSamples; u++)
			{
				if (of.Samples[u].length != 0)
				{
					SampleLoader.SL_RegisterSample(of.Samples[u], (int)MDTypes.MD_MUSIC, modreader);
				}
			}

			return true;
		}

		#endregion

		#region Module unloading
		public static void UnLoad(Module mod)
		{
			ModPlayer.Player_Exit_internal(mod);
			ML_FreeEx(mod);
		}

		static void ML_FreeEx(Module mf)
		{
			if (mf.Samples != null)
			{
				for (ushort t = 0; t < mf.NumSamples; t++)
				{
					if (mf.Samples[t].length != 0)
					{
						ModDriver.Driver?.SampleUnload(mf.Samples[t].handle);
					}
				}
			}
		}
		#endregion
	}
}

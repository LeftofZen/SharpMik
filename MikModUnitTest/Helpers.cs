using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace MikModUnitTest
{
	public static class UnitTestHelpers
	{

		public static void FindRepeats()
		{
			var repeatTest = new Dictionary<string, int>();

			foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!ass.GlobalAssemblyCache)
				{
					var types = ass.GetTypes();

					foreach (var type in types)
					{
						if (repeatTest.ContainsKey(type.Name))
						{
							repeatTest[type.Name]++;
						}
						else
						{
							repeatTest.Add(type.Name, 1);
						}
					}
				}
			}

			Console.WriteLine("----");
			foreach (var key in repeatTest.Keys)
			{
				if (repeatTest[key] > 1)
				{
					Console.WriteLine(key);
				}
			}

			Console.WriteLine("----");
		}

		public static bool ReadXML<T>(string fileName, ref T obj)
		{
			FileStream xmlStream = null;

			if (File.Exists(fileName))
			{
				try
				{
					var xmlSer = new XmlSerializer(typeof(T));

					xmlStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
					obj = (T)xmlSer.Deserialize(xmlStream);
					return true;
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					return false;
				}
				finally
				{
					xmlStream?.Close();
				}
			}

			return false;
		}

		public static bool WriteXML<T>(string fileName, T obj)
		{
			FileStream xmlStream = null;

			try
			{
				var xmlSer = new XmlSerializer(typeof(T));

				xmlStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
				xmlSer.Serialize(xmlStream, obj);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}
			finally
			{
				xmlStream?.Close();
			}
		}
	}
}

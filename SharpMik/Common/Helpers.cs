using SharpMik.Attributes;
using SharpMik.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpMik.Common
{
	public static class Helpers
	{

		static string[] s_FileTypes;

		public static string[] ModFileExtensions
		{
			get
			{
				if (s_FileTypes == null)
				{
					var extensions = new List<string>();
					var list = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(IModLoader)));

					foreach (var item in list)
					{
						var attributes = item.GetCustomAttributes(typeof(ModFileExtensionsAttribute), false);
						foreach (var attribute in attributes)
						{
							var modExtension = attribute as ModFileExtensionsAttribute;

							if (modExtension != null)
							{
								extensions.AddRange(modExtension.FileExtensions);
							}
						}
					}

					s_FileTypes = extensions.Distinct().ToArray();
				}

				return s_FileTypes;
			}
		}

		public static bool MatchesExtensions(string filename)
		{
			var match = false;
			foreach (var ext in ModFileExtensions)
			{
				var tolower = filename.ToLower();

				if (tolower.StartsWith(ext + ".") || tolower.EndsWith("." + ext))
				{
					match = true;
					break;
				}
			}

			return match;
		}
	}
}

using System.Reflection;
using System.Linq;
using SharpMik.Interfaces;
using SharpMik.Attributes;
using System.Collections.Generic;

namespace SharpMik.Common
{
	public static class Helpers
	{

		private static string[] s_FileTypes;

		public static string[] ModFileExtensions
		{
			get
			{
				if (s_FileTypes == null)
				{
					var extentions = new List<string>();
					var list = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(IModLoader)));

					foreach (var item in list)
					{
						var attributes = item.GetCustomAttributes(typeof(ModFileExtentionsAttribute), false);
						foreach (var attribute in attributes)
						{
							var modExtention = attribute as ModFileExtentionsAttribute;

							if (modExtention != null)
							{
								extentions.AddRange(modExtention.FileExtentions);
							}
						}
					}

					s_FileTypes = extentions.Distinct().ToArray();
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

using System;

namespace SharpMik.Attributes
{
	public class ModFileExtensionsAttribute : Attribute
	{
		public string[] FileExtensions { get; }

		public ModFileExtensionsAttribute(params string[] extensions) => FileExtensions = extensions;
	}
}

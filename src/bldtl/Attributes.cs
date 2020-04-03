using System;

namespace CampAI.BuildTools {
	public sealed class ArgAttribute : Attribute {
		public string Description { get { return description; } set { description = value; } }
		public ActionArgFlags Flags { get { return flags; } set { flags = value; } }
		public string Name { get { return name; } set { name = value; } }
		public string ValueName { get { return valueName; } set { valueName = value; } }

		private string name;
		private string valueName;
		private string description;
		private ActionArgFlags flags;
	}

	public sealed class ToolAttribute : Attribute {
		public string Description { get { return description; } set { description = value; } }
		public string Name { get { return name; } set { name = value; } }

		private string name;
		private string description;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CampAI.BuildTools {

	[Flags]
	public enum ActionArgFlags : uint {
		None = 0,
		Required = 1,
		Positional = 2,
	}

	public class ActionArg {
		public string Description { get { return description; } }
		public ActionArgFlags Flags { get { return flags; } }
		public bool IsList { get { return typeof(IList).IsAssignableFrom(type); } }
		public string Name { get { return name; } }
		public string ValueName { get { return valueName; } }
		public Type Type { get { return type; } }

		public void AddValue(string value) {
			object v;
			Type elemT;
			IList list;
			elemT = null;
			if ((data == null || data == Type.Missing) && (elemT = GetListElemT(type)) != null) { data = Activator.CreateInstance(typeof(List<>).MakeGenericType(elemT)); }
			if (elemT == null) { elemT = type; }
			v = elemT.IsEnum ? Enum.Parse(elemT, value) : elemcvt(value);
			list = data as IList;
			if (list != null) { list.Add(v); } else { data = v; }
		}
		public object GetValue() {
			if (data == null || data == Type.Missing) {
				if ((flags & ActionArgFlags.Required) != 0) { throw new ArgumentException(); }
			} else if (data is IList) {
				if (type.IsArray) { return data.GetType().GetMethod("ToArray").Invoke(data, null); }
			}
			return data;
		}

		public static ActionArg CreateArg(ParameterInfo info) {
			ActionArg arg;
			ArgAttribute attr;
			Type t;
			Type elemT;
			arg = new ActionArg();
			attr = info.GetCustomAttribute<ArgAttribute>();
			if (attr != null) {
				arg.name = attr.Name;
				arg.valueName = attr.ValueName;
				arg.description = attr.Description;
				arg.flags = attr.Flags;
			} else {
				if (info.IsDefined(typeof(ParamArrayAttribute))) { arg.flags = ActionArgFlags.Positional; }
			}
			if (arg.name == null) { arg.name = info.Name; }
			if (info.IsOptional) { arg.data = Type.Missing; }
			t = info.ParameterType;
			arg.type = t;
			elemT = GetListElemT(t);
			if (elemT == null) { elemT = t; }
			if (elemT.IsEnum) { return arg; }
			foreach (KeyValuePair<Type, Func<string, object>> kv in elemcvts) {
				if (elemT.IsAssignableFrom(kv.Key)) {
					arg.elemcvt = kv.Value;
					return arg;
				}
			}
			throw new ArgumentException();
		}

		protected ActionArg() { }

		private object data;
		private Type type;
		private Func<string, object> elemcvt;
		private string name;
		private string valueName;
		private string description;
		private ActionArgFlags flags;

		private static Regex rexTrue = new Regex(@"^\s*(\+|1|on|t|true|y|yes)\s*$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
		private static Regex rexFalse = new Regex(@"^\s*(-|0|f|false|n|no|off)\s*$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
		private static KeyValuePair<Type, Func<string, object>>[] elemcvts = {
			new KeyValuePair<Type, Func<string, object>>(typeof(Boolean), ConvertBoolean),
			new KeyValuePair<Type, Func<string, object>>(typeof(Double), ConvertDouble),
			new KeyValuePair<Type, Func<string, object>>(typeof(Int32), ConvertInt32),
			new KeyValuePair<Type, Func<string, object>>(typeof(Int64), ConvertInt64),
			new KeyValuePair<Type, Func<string, object>>(typeof(Single), ConvertSingle),
			new KeyValuePair<Type, Func<string, object>>(typeof(String), ConvertString),
			new KeyValuePair<Type, Func<string, object>>(typeof(UInt32), ConvertUInt32),
			new KeyValuePair<Type, Func<string, object>>(typeof(UInt64), ConvertUInt64),
		};

		private static object ConvertBoolean(string value) {
			if (String.IsNullOrWhiteSpace(value) || rexTrue.Match(value).Success) { return true; }
			if (rexFalse.Match(value).Success) { return false; }
			return Boolean.Parse(value);
		}
		private static object ConvertDouble(string value) {
			return Double.Parse(value, CultureInfo.InvariantCulture);
		}
		private static object ConvertInt32(string value) {
			if (value.StartsWith("0x", StringComparison.Ordinal)) { return Convert.ToInt32(value, 16); }
			return Int32.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}
		private static object ConvertInt64(string value) {
			if (value.StartsWith("0x", StringComparison.Ordinal)) { return Convert.ToInt64(value, 16); }
			return Int64.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}
		private static object ConvertSingle(string value) {
			return Single.Parse(value, CultureInfo.InvariantCulture);
		}
		private static object ConvertString(string value) { return value; }
		private static object ConvertUInt32(string value) {
			if (value.StartsWith("0x", StringComparison.Ordinal)) { return Convert.ToUInt32(value, 16); }
			return UInt32.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}
		private static object ConvertUInt64(string value) {
			if (value.StartsWith("0x", StringComparison.Ordinal)) { return Convert.ToUInt64(value, 16); }
			return UInt64.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}
		private static Type GetListElemT(Type type) {
			Type[] types;
			if (type.IsArray) { return type.GetElementType(); }
			if (type.IsConstructedGenericType) {
				types = type.GetGenericArguments();
				if (types.Length == 1 && typeof(IList<>).MakeGenericType(types).IsAssignableFrom(type)) { return types[0]; }
			}
			return null;
		}
	}
}

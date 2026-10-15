//  ExcludeFromJSONSerialize


using System;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Attribute that can be used to exclude single field from JSON serialization. With default serialization settings, this does same as System.NonSerialized attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	public sealed class ExcludeFromJSONSerializeAttribute : Attribute {

		/// <summary>
		/// Constructor for new ExcludeFromJSONSerialize attribute.
		/// </summary>
		public ExcludeFromJSONSerializeAttribute() {
		}

	}

}

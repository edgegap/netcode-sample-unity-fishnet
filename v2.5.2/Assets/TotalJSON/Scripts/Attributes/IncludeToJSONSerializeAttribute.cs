//  IncludeToJSONSerialize


using System;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Attribute that can be used to include single field to JSON serialization. With default serialization settings, this does same as UnityEngine.SerializeField attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	public sealed class IncludeToJSONSerializeAttribute : Attribute {

		/// <summary>
		/// Constructor for new IncludeToJSONSerialize attribute.
		/// </summary>
		public IncludeToJSONSerializeAttribute() {
		}

	}

}

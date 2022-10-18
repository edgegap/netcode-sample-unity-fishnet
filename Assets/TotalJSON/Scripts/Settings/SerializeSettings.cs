//  SerializeSettings


namespace Leguar.TotalJSON {
	
	/// <summary>
	/// Settings for serialization.
	/// </summary>
	public class SerializeSettings {

		private bool allowNonStringDictionaryKeys = false;

		/// <summary>
		/// By default this is false. Meaning that if any dictionary is serialized to JSON object, source dictionary must be using string keys, like JSON itself is using.
		/// 
		/// If this is set false, dictionaries key type may be anything and serialization is just using ToString() to create key. In this case, make sure each dictionary
		/// key string representation is unique.
		/// </summary>
		/// <value>
		/// False by default, set true to allow any dictionary keys.
		/// </value>
		public bool AllowNonStringDictionaryKeys {
			set {
				allowNonStringDictionaryKeys = value;
			}
			get {
				return allowNonStringDictionaryKeys;
			}
		}

		private bool ignoreSystemAndUnitySerializeAttributes = false;

		/// <summary>
		/// By default this is false. Meaning that serialization will check fields for UnityEngine.SerializeField and System.NonSerialized attributes and follow those.
		/// 
		/// In case these attributes are required for other uses but JSON serialization should not follow these attributes, you can set this setting to true and those
		/// attributes will be ignored during JSON serialization. You can still include/exclude single fields from serialization using TotalJSON's own
		/// IncludeToJSONSerialize and ExcludeFromJSONSerialize attributes which are always followed regardless of this setting.
		/// </summary>
		/// <value>
		/// False by default, set true to ignore non-json specific serialization attributes.
		/// </value>
		public bool IgnoreSystemAndUnitySerializeAttributes {
			set {
				ignoreSystemAndUnitySerializeAttributes = value;
			}
			get {
				return ignoreSystemAndUnitySerializeAttributes;
			}
		}

	}

}

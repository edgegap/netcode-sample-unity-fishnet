//  JValue


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {
	
	/// <summary>
	/// Abstract base class for all JSON objects: JSON, JArray, JNumber, JBoolean, JString and JNull.
	/// </summary>
	public abstract class JValue {

		protected JValue() {
		}

		/// <summary>
		/// Turns this object to JSON formatted string.
		/// </summary>
		/// <returns>
		/// This object as JSON formatted string, containing only basic ascii characters between [32..126] without line feeds.
		/// </returns>
		public virtual string CreateString() {
			return CreateString(new CreateStringSettings());
		}

		/// <summary>
		/// Turns this object to JSON formatted string using specified settings.
		/// </summary>
		/// <returns>
		/// This object as JSON formatted string.
		/// </returns>
		public virtual string CreateString(CreateStringSettings settings) {
			CreateStringRunner createStringRunner = new CreateStringRunner(settings);
			zCreate(createStringRunner);
			return createStringRunner.getFinalString();
		}

		internal abstract void zCreate(CreateStringRunner createStringRunner);

		internal abstract object zDeserialize(Type type, string toFieldName, DeserializeSettings deserializeSettings);

	}

}

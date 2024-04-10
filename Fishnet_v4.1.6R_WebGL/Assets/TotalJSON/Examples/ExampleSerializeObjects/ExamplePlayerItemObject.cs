//    Example - Serialize and Deserialize - ExamplePlayerObject - ExamplePlayerItemObject

//    This class is part of "SerializeAndDeserialize.cs" example code


using Leguar.TotalJSON;

namespace Leguar.TotalJSON.Examples {

	public struct ExamplePlayerItemObject { // No particular reason for this to be 'struct' instead of 'class', just for the sake of different example

		public string name;
		public int uses;

		public override string ToString() {
			return "[ExamplePlayerItemObject: name = \""+name+"\", uses = "+uses+"]";
		}
		
	}

}

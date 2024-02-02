========================
 TotalJSON - ReadMe.txt
========================



JSON
====

If you are not familiar of JSON format itself, check out http://json.org/ first.



Basic usage
===========

This package is nothing but C# code (excluding one example scene). All the public scripts
are in folder TotalJSON/Scripts/

Take these in use in your own code:
using Leguar.TotalJSON;

Classes represeting JSON objects and values are:
* JSON - Main JSON object class where data is saved as key/value pairs
* JArray - JSON array where data is accessed by index
* JString - String value
* JNumber - Number value (integer or floating point number)
* JBoolean - Boolean (bool) value
* JNull - JSON null value

Scripts folder also contains JValue, which is just abstract base class for all above classes
and is usually not used directly.

JSON and JArray are different from others in sense that they can be modified after instance
is created. You can use provided methods to add, remove and replace values in them.


Creating and modifying
----------------------

You can create new JSON object
JSON json = new JSON();
and use provided method to add/modify/remove data.

Arrays can be created similarly
JArray jArray = new JArray();

Typically values are added to JSON or JArray objects directly like
json.Add("answer",42);
or
jArray.Add(true);

All the Add, Replace, Insert etc methods accepts 'object' as input parameter. This
object can be anything that can be converted to some JValue object without any ambiguity.
For example strings, any C# number types, booleans and null. Obviously other JSON objects
or JArray arrays are fine too.

But JSON-number, -string, -boolean and -null values can be created separately
JNumber jsonNumber = new JNumber(42);
and then be added to JSON or JArray objects.

This is useful especially with numbers that would go outside normal C# number range:
json.Add("huge_integer",new JNumber("123456789012345678901234567890"));
or
jArray.InsertAt(0,new JNumber("3.1415E+42000"));


Parsing and creating JSON formatted strings
-------------------------------------------

Strings that are in JSON format like
{"name":"Pingu","active":true,"items":[1,2,4]}
can be turned to JSON object using static method
JSON json = JSON.ParseString(inputString);

Likewise, JSON object can be turned to JSON-formatted string using
string outputString = json.CreateString();
usually for storing or sending to another system.

There's also method to get JSON out in more human friendly format using
Debug.Log(json.CreatePrettyString());
typically for debug purposes.

All the same can be done to JArrays.


Printing out values
-------------------

All the classes (JSON, JArray, JString, JNumber, JBoolean and JNull) have ToString()
method overridden to print out information of the object. This meant to be just debug
information about the object. In case of JSON, amount or key/value pairs in object.
In case of JArray, count of objects and their types, or just the value stored in object.
Generally, good for console output since length of the output is always relatively short.

All the classes also have CreateString() method that will print out all the content of
object, in JSON format. (See "Parsing and creating JSON formatted strings" above.)


Serializing and deserializing objects
-------------------------------------

For example, if you have class like
class ExamplePlayer {
	public string name;
	public bool active;
	public List<int> items;
	public int? skill;
}
and instance 'examplePlayer' of that class where fields "name", "active" and "items" have
some values set.

You can turn class content to JSON
JSON json = JSON.Serialize(examplePlayer);
And this can be turned to string using CreateString() method like in example above.

Only public fields and fields marked with attribute [UnityEngine.SerializeField] or
[IncludeToJSONSerialize] are serialized. Constant, static and readonly fields are
not serialized. Additionally, you can exclude public fields from serialization by
adding [System.NonSerialized] or [ExcludeFromJSONSerialize] attribute to it.

To other direction, if you have input string like
{"name":"Pingu","active":true,"items":[1,2,4],"skill":null}

Then string values can be copied to class fields by first turning string in to JSON
JSON json = JSON.ParseString(inputString);
and deserialize JSON to class instance
ExamplePlayer examplePlayer = json.Deserialize<ExamplePlayer>();

You now have instance of class ExamplePlayer where field 'name' is "Pingu" etc.



Additional features
===================


Debugging in Unity Editor
-------------------------

For easy debugging, you can follow content of JSON objects in Unity Editor when application
is running.

In your code, you can add any JSON or JArray object to debug by saying
myJsonObject.DebugInEditor("My JSON Object");
where parameter string is name that will appear in debug window.

In Unity Editor, open debug window from menu
Window -> Total JSON -> JSON Runtime Debug

Your objects and their content will appear there.

There is no harm leaving these "DebugInEditor(...)" lines in your code when making build.
They do not have any effect outside Unity Editor.


Debugging in production
-----------------------

Nothing is perfect and it may happen that JSON data you are trying to parse or handle
is invalid. In editor this isn't so much problem but in production builds all you may
get is exception like for example

> JSONKeyNotFoundException: Value for key "id" does not exist in this JSON

Depending on build, it might be that there isn't any stack trace available. If your
project does handle lots of JSON objects, above error message may not tell you at all
where the problem occurred.

This is why TotalJSON allows you to add Debug ID to JSON objects. For example:

JSON playerJSON = getPlayerJSONFromServerData();
playerJSON.SetDebugIDForExceptions("Player data from server");

If now any exceptions happens when handling 'playerJSON', they'll look like this:

> JSONKeyNotFoundException: Value for key "id" does not exist in this JSON - JSON Debug ID: "Player data from server"

So you'll immediately know which JSON caused the exception even if stacktrace is not
available.

This Debug ID string can be added already when parsing JSON:

string playerDataString = <json formatted data here>
JSON playerJSON = JSON.ParseString(playerDataString, "Player data from server");

In this case, Debug ID is added to exceptions already if they happen when parsing
JSON data. If parsing goes fine, that same Debug ID (in this case "Player data from
server") is added to resulting JSON object, so there's no need to use
SetDebugIDForExceptions(string) separately.


JSON validator
--------------

Open JSON validator from Unity Editor menu
Window -> Total JSON -> JSON Validator

To this window you can copy&paste JSON objects or arrays and click Validate button below.
Not only this will check that JSON is in valid format, but it will also make it much more
readable by adding indents and line feeds.

Validator will also make extra effort trying to remove leading or trailing texts that are
not part of JSON object. So you can for example copy text directly from Unity Console with
stack traces. As long as there is some JSON object in the text.


Customized string output
------------------------

When turning JSON object to string using CreateString() method, it is possible to customize
output string using CreateStringSettings.

string customOutput = json.CreateString( new CreateStringSettings() {
	HumanReadable = true,
	NewLine = CreateStringSettings.NewLineTypes.LF
});

Mostly this is needed when outputting human readable JSON. CreateStringSettings allows for
example changing type and size of indentation, character that is used for line feed etc.


Protecting JSON objects
-----------------------

You can set JSON or JArray objects protected using
json.SetProtected();

This will set this JSON object and all other objects it contains to write protected.
Nothing can be added, moved or changed. This is useful to make sure no accidental changes
are made to objects that should be only for reading.

Once this is set, pretection can't be removed.




Examples
========


TotalJSON package have directory Examples that contains example scene and multiple example
scripts.



Full API docs
=============


All the classes have full inline C# documentation that is also available online in HTML format:

http://www.leguar.com/unity/totaljson/apidoc/1.8/



Feedback
========


If you are happy with this asset, please rate us or leave feedback in Unity Asset Store:

https://assetstore.unity.com/packages/slug/130344


If you have any problems, or maybe suggestions for future versions, feel free to contact:

http://www.leguar.com/contact/?about=totaljson

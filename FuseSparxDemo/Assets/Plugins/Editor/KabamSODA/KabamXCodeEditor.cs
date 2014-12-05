using Kabam.MiniJSON;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityEditor;
using UnityEngine;
/*
 * Copyright (c) 2013 Calvin Rien
 *
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 *
 * Simplified it so that it doesn't throw exceptions
 * and can be used in Unity iPhone with maximum code stripping.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Kabam.MiniJSON {
    // Example usage:
    //
    //  using UnityEngine;
    //  using System.Collections;
    //  using System.Collections.Generic;
    //  using MiniJSON;
    //
    //  public class MiniJSONTest : MonoBehaviour {
    //      void Start () {
    //          var jsonString = "{ \"array\": [1.44,2,3], " +
    //                          "\"object\": {\"key1\":\"value1\", \"key2\":256}, " +
    //                          "\"string\": \"The quick brown fox \\\"jumps\\\" over the lazy dog \", " +
    //                          "\"unicode\": \"\\u3041 Men\u00fa sesi\u00f3n\", " +
    //                          "\"int\": 65536, " +
    //                          "\"float\": 3.1415926, " +
    //                          "\"bool\": true, " +
    //                          "\"null\": null }";
    //
    //          var dict = Json.Deserialize(jsonString) as Dictionary<string,object>;
    //
    //          Debug.Log("deserialized: " + dict.GetType());
    //          Debug.Log("dict['array'][0]: " + ((List<object>) dict["array"])[0]);
    //          Debug.Log("dict['string']: " + (string) dict["string"]);
    //          Debug.Log("dict['float']: " + (double) dict["float"]); // floats come out as doubles
    //          Debug.Log("dict['int']: " + (long) dict["int"]); // ints come out as longs
    //          Debug.Log("dict['unicode']: " + (string) dict["unicode"]);
    //
    //          var str = Json.Serialize(dict);
    //
    //          Debug.Log("serialized: " + str);
    //      }
    //  }

    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    ///
    /// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
    /// All numbers are parsed to doubles.
    /// </summary>
    public static class Json {
        /// <summary>
        /// Parses the string json into a value
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false</returns>
        public static object Deserialize(string json) {
            // save the string for debug information
            if (json == null) {
                return null;
            }

            return Parser.Parse(json);
        }

        sealed class Parser : IDisposable {
            const string WORD_BREAK = "{}[],:\"";

            public static bool IsWordBreak(char c) {
                return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }

            enum TOKEN {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            };

            StringReader json;

            Parser(string jsonString) {
                json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString) {
                using (var instance = new Parser(jsonString)) {
                    return instance.ParseValue();
                }
            }

            public void Dispose() {
                json.Dispose();
                json = null;
            }

            Dictionary<string, object> ParseObject() {
                Dictionary<string, object> table = new Dictionary<string, object>();

                // ditch opening brace
                json.Read();

                // {
                while (true) {
                    switch (NextToken) {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.COMMA:
                        continue;
                    case TOKEN.CURLY_CLOSE:
                        return table;
                    default:
                        // name
                        string name = ParseString();
                        if (name == null) {
                            return null;
                        }

                        // :
                        if (NextToken != TOKEN.COLON) {
                            return null;
                        }
                        // ditch the colon
                        json.Read();

                        // value
                        table[name] = ParseValue();
                        break;
                    }
                }
            }

            List<object> ParseArray() {
                List<object> array = new List<object>();

                // ditch opening bracket
                json.Read();

                // [
                var parsing = true;
                while (parsing) {
                    TOKEN nextToken = NextToken;

                    switch (nextToken) {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.COMMA:
                        continue;
                    case TOKEN.SQUARED_CLOSE:
                        parsing = false;
                        break;
                    default:
                        object value = ParseByToken(nextToken);

                        array.Add(value);
                        break;
                    }
                }

                return array;
            }

            object ParseValue() {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            object ParseByToken(TOKEN token) {
                switch (token) {
                case TOKEN.STRING:
                    return ParseString();
                case TOKEN.NUMBER:
                    return ParseNumber();
                case TOKEN.CURLY_OPEN:
                    return ParseObject();
                case TOKEN.SQUARED_OPEN:
                    return ParseArray();
                case TOKEN.TRUE:
                    return true;
                case TOKEN.FALSE:
                    return false;
                case TOKEN.NULL:
                    return null;
                default:
                    return null;
                }
            }

            string ParseString() {
                StringBuilder s = new StringBuilder();
                char c;

                // ditch opening quote
                json.Read();

                bool parsing = true;
                while (parsing) {

                    if (json.Peek() == -1) {
                        parsing = false;
                        break;
                    }

                    c = NextChar;
                    switch (c) {
                    case '"':
                        parsing = false;
                        break;
                    case '\\':
                        if (json.Peek() == -1) {
                            parsing = false;
                            break;
                        }

                        c = NextChar;
                        switch (c) {
                        case '"':
                        case '\\':
                        case '/':
                            s.Append(c);
                            break;
                        case 'b':
                            s.Append('\b');
                            break;
                        case 'f':
                            s.Append('\f');
                            break;
                        case 'n':
                            s.Append('\n');
                            break;
                        case 'r':
                            s.Append('\r');
                            break;
                        case 't':
                            s.Append('\t');
                            break;
                        case 'u':
                            var hex = new char[4];

                            for (int i=0; i< 4; i++) {
                                hex[i] = NextChar;
                            }

                            s.Append((char) Convert.ToInt32(new string(hex), 16));
                            break;
                        }
                        break;
                    default:
                        s.Append(c);
                        break;
                    }
                }

                return s.ToString();
            }

            object ParseNumber() {
                string number = NextWord;

                if (number.IndexOf('.') == -1) {
                    long parsedInt;
                    Int64.TryParse(number, out parsedInt);
                    return parsedInt;
                }

                double parsedDouble;
                Double.TryParse(number, out parsedDouble);
                return parsedDouble;
            }

            void EatWhitespace() {
                while (Char.IsWhiteSpace(PeekChar)) {
                    json.Read();

                    if (json.Peek() == -1) {
                        break;
                    }
                }
            }

            char PeekChar {
                get {
                    return Convert.ToChar(json.Peek());
                }
            }

            char NextChar {
                get {
                    return Convert.ToChar(json.Read());
                }
            }

            string NextWord {
                get {
                    StringBuilder word = new StringBuilder();

                    while (!IsWordBreak(PeekChar)) {
                        word.Append(NextChar);

                        if (json.Peek() == -1) {
                            break;
                        }
                    }

                    return word.ToString();
                }
            }

            TOKEN NextToken {
                get {
                    EatWhitespace();

                    if (json.Peek() == -1) {
                        return TOKEN.NONE;
                    }

                    switch (PeekChar) {
                    case '{':
                        return TOKEN.CURLY_OPEN;
                    case '}':
                        json.Read();
                        return TOKEN.CURLY_CLOSE;
                    case '[':
                        return TOKEN.SQUARED_OPEN;
                    case ']':
                        json.Read();
                        return TOKEN.SQUARED_CLOSE;
                    case ',':
                        json.Read();
                        return TOKEN.COMMA;
                    case '"':
                        return TOKEN.STRING;
                    case ':':
                        return TOKEN.COLON;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        return TOKEN.NUMBER;
                    }

                    switch (NextWord) {
                    case "false":
                        return TOKEN.FALSE;
                    case "true":
                        return TOKEN.TRUE;
                    case "null":
                        return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }
        }

        /// <summary>
        /// Converts a IDictionary / IList object or a simple type (string, int, etc.) into a JSON string
        /// </summary>
        /// <param name="json">A Dictionary&lt;string, object&gt; / List&lt;object&gt;</param>
        /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
        public static string Serialize(object obj) {
            return Serializer.Serialize(obj);
        }

        sealed class Serializer {
            StringBuilder builder;

            Serializer() {
                builder = new StringBuilder();
            }

            public static string Serialize(object obj) {
                var instance = new Serializer();

                instance.SerializeValue(obj);

                return instance.builder.ToString();
            }

            void SerializeValue(object value) {
                IList asList;
                IDictionary asDict;
                string asStr;

                if (value == null) {
                    builder.Append("null");
                } else if ((asStr = value as string) != null) {
                    SerializeString(asStr);
                } else if (value is bool) {
                    builder.Append((bool) value ? "true" : "false");
                } else if ((asList = value as IList) != null) {
                    SerializeArray(asList);
                } else if ((asDict = value as IDictionary) != null) {
                    SerializeObject(asDict);
                } else if (value is char) {
                    SerializeString(new string((char) value, 1));
                } else {
                    SerializeOther(value);
                }
            }

            void SerializeObject(IDictionary obj) {
                bool first = true;

                builder.Append('{');

                foreach (object e in obj.Keys) {
                    if (!first) {
                        builder.Append(',');
                    }

                    SerializeString(e.ToString());
                    builder.Append(':');

                    SerializeValue(obj[e]);

                    first = false;
                }

                builder.Append('}');
            }

            void SerializeArray(IList anArray) {
                builder.Append('[');

                bool first = true;

                foreach (object obj in anArray) {
                    if (!first) {
                        builder.Append(',');
                    }

                    SerializeValue(obj);

                    first = false;
                }

                builder.Append(']');
            }

            void SerializeString(string str) {
                builder.Append('\"');

                char[] charArray = str.ToCharArray();
                foreach (var c in charArray) {
                    switch (c) {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        int codepoint = Convert.ToInt32(c);
                        if ((codepoint >= 32) && (codepoint <= 126)) {
                            builder.Append(c);
                        } else {
                            builder.Append("\\u");
                            builder.Append(codepoint.ToString("x4"));
                        }
                        break;
                    }
                }

                builder.Append('\"');
            }

            void SerializeOther(object value) {
                // NOTE: decimals lose precision during serialization.
                // They always have, I'm just letting you know.
                // Previously floats and doubles lost precision too.
                if (value is float) {
                    builder.Append(((float) value).ToString("R"));
                } else if (value is int
                    || value is uint
                    || value is long
                    || value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is ulong) {
                    builder.Append(value);
                } else if (value is double
                    || value is decimal) {
                    builder.Append(Convert.ToDouble(value).ToString("R"));
                } else {
                    SerializeString(value.ToString());
                }
            }
        }
    }
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXBuildFile : PBXObject
	{
		private const string FILE_REF_KEY = "fileRef";
		private const string SETTINGS_KEY = "settings";
		private const string ATTRIBUTES_KEY = "ATTRIBUTES";
		private const string WEAK_VALUE = "Weak";
		private const string COMPILER_FLAGS_KEY = "COMPILER_FLAGS";
		
		public string name;
		
		public PBXBuildFile( PBXFileReference fileRef, bool weak = false ) : base()
		{
			this.Add( FILE_REF_KEY, fileRef.guid );
			SetWeakLink( weak );
			name = fileRef.name;
		}

		public PBXBuildFile( string guid, PBXDictionary dictionary ) : base ( guid, dictionary )
		{
			if(!this.data.ContainsKey(SETTINGS_KEY))
				return;
			object settingsObj = this.data[SETTINGS_KEY];
			
			if(!(settingsObj is PBXDictionary))
				return;
			PBXDictionary settingsDict = (PBXDictionary) settingsObj;
			settingsDict.internalNewlines = false;
			
			if( !settingsDict.ContainsKey(ATTRIBUTES_KEY) )
				return;
			object attributesObj = settingsDict[ATTRIBUTES_KEY];
			
			if(!(attributesObj is PBXList))
				return;
			
			PBXList attributesCast = (PBXList)attributesObj;
			attributesCast.internalNewlines = false;
		}

		public bool SetWeakLink( bool weak = false )
		{
			PBXDictionary settings = null;
			PBXList attributes = null;
			
			if( !_data.ContainsKey( SETTINGS_KEY ) ) {
				if( weak ) {
					attributes = new PBXList();
					attributes.internalNewlines = false;
					attributes.Add( WEAK_VALUE );
					
					settings = new PBXDictionary();
					settings.Add( ATTRIBUTES_KEY, attributes );
					settings.internalNewlines = false;
					
					this.Add( SETTINGS_KEY, settings );
				}
				return true;
			}
			
			settings = _data[ SETTINGS_KEY ] as PBXDictionary;
			settings.internalNewlines = false;
			if( !settings.ContainsKey( ATTRIBUTES_KEY ) ) {
				if( weak ) {
					attributes = new PBXList();
					attributes.internalNewlines = false;
					attributes.Add( WEAK_VALUE );
					settings.Add( ATTRIBUTES_KEY, attributes );
					return true;
				}
				else {
					return false;
				}
			}
			else {
				attributes = settings[ ATTRIBUTES_KEY ] as PBXList;
			}
			
			attributes.internalNewlines = false;
			if( weak ) {
				attributes.Add( WEAK_VALUE );
			}
			else {
				attributes.Remove( WEAK_VALUE );
			}
			
			settings.Add( ATTRIBUTES_KEY, attributes );
			this.Add( SETTINGS_KEY, settings );
			
			return true;
		}
		
		public bool AddCompilerFlag( string flag )
		{
			if( !_data.ContainsKey( SETTINGS_KEY ) )
				_data[ SETTINGS_KEY ] = new PBXDictionary();
			
			if( !((PBXDictionary)_data[ SETTINGS_KEY ]).ContainsKey( COMPILER_FLAGS_KEY ) ) {
				((PBXDictionary)_data[ SETTINGS_KEY ]).Add( COMPILER_FLAGS_KEY, flag );
				return true;
			}
			
			string[] flags = ((string)((PBXDictionary)_data[ SETTINGS_KEY ])[ COMPILER_FLAGS_KEY ]).Split( ' ' );
			foreach( string item in flags ) {
				if( item.CompareTo( flag ) == 0 )
					return false;
			}
			
			((PBXDictionary)_data[ SETTINGS_KEY ])[ COMPILER_FLAGS_KEY ] = ( string.Join( " ", flags ) + " " + flag );
			return true;
		}
		
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXBuildPhase : PBXObject
	{
		protected const string FILES_KEY = "files";
		
		public PBXBuildPhase() :base()
		{
			internalNewlines = true;
		}
		
		public PBXBuildPhase( string guid, PBXDictionary dictionary ) : base ( guid, dictionary )
		{
			internalNewlines = true;
		}
		
		public bool AddBuildFile( PBXBuildFile file )
		{
			if( !ContainsKey( FILES_KEY ) ){
				this.Add( FILES_KEY, new PBXList() );
			}
			((PBXList)_data[ FILES_KEY ]).Add( file.guid );
			
			return true;
		}
		
		public void RemoveBuildFile( string id )
		{
			if( !ContainsKey( FILES_KEY ) ) {
				this.Add( FILES_KEY, new PBXList() );
				return;
			}
			
			((PBXList)_data[ FILES_KEY ]).Remove( id );
		}
		
		public bool HasBuildFile( string id )
		{
			if( !ContainsKey( FILES_KEY ) ) {
				this.Add( FILES_KEY, new PBXList() );
				return false;
			}
			
			if( !IsGuid( id ) )
				return false;
			
			return ((PBXList)_data[ FILES_KEY ]).Contains( id );
		}
		
	}
	
	public class PBXFrameworksBuildPhase : PBXBuildPhase
	{
		public PBXFrameworksBuildPhase( string guid, PBXDictionary dictionary ) : base ( guid, dictionary )
		{
		}
	}

	public class PBXResourcesBuildPhase : PBXBuildPhase
	{
		public PBXResourcesBuildPhase( string guid, PBXDictionary dictionary ) : base ( guid, dictionary )
		{
		}
	}

	public class PBXShellScriptBuildPhase : PBXBuildPhase
	{
		public PBXShellScriptBuildPhase( string guid, PBXDictionary dictionary ) : base ( guid, dictionary )
		{
		}
	}

	public class PBXSourcesBuildPhase : PBXBuildPhase
	{
		public PBXSourcesBuildPhase( string guid, PBXDictionary dictionary ) : base ( guid, dictionary )
		{
		}
	}

	public class PBXCopyFilesBuildPhase : PBXBuildPhase
	{
		public PBXCopyFilesBuildPhase( string guid, PBXDictionary dictionary ) : base ( guid, dictionary )
		{
		}
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXDictionary : Dictionary<string, object>
	{
		public bool internalNewlines;

		public PBXDictionary() : base()
		{
			internalNewlines = true;
		}

		public void Append( PBXDictionary dictionary )
		{
			foreach( var item in dictionary) {
				this.Add( item.Key, item.Value );
			}
		}
		
		public void Append<T>( PBXDictionary<T> dictionary ) where T : PBXObject
		{
			foreach( var item in dictionary) {
				this.Add( item.Key, item.Value );
			}
		}
	}
	
	public class PBXDictionary<T> : Dictionary<string, T> where T : PBXObject
	{
		public PBXDictionary()
		{
			
		}
		
		public PBXDictionary( PBXDictionary genericDictionary )
		{
			foreach( KeyValuePair<string, object> currentItem in genericDictionary ) {
				if( ((string)((PBXDictionary)currentItem.Value)[ "isa" ]).CompareTo( typeof(T).Name ) == 0 ) {
					T instance = (T)System.Activator.CreateInstance( typeof(T), currentItem.Key, (PBXDictionary)currentItem.Value );
					this.Add( currentItem.Key, instance );
				}
			}	
		}
		
		public void Add( T newObject )
		{
			this.Add( newObject.guid, newObject );
		}
		
		public void Append( PBXDictionary<T> dictionary )
		{
			foreach( KeyValuePair<string, T> item in dictionary) {
				this.Add( item.Key, (T)item.Value );
			}
		}
		
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXFileReference : PBXObject
	{
		protected const string PATH_KEY = "path";
		protected const string NAME_KEY = "name";
		protected const string SOURCETREE_KEY = "sourceTree";
		protected const string EXPLICIT_FILE_TYPE_KEY = "explicitFileType";
		protected const string LASTKNOWN_FILE_TYPE_KEY = "lastKnownFileType";
		protected const string ENCODING_KEY = "fileEncoding";
		
		public string buildPhase;
		public readonly Dictionary<TreeEnum, string> trees = new Dictionary<TreeEnum, string> {
			{ TreeEnum.ABSOLUTE, "\"<absolute>\"" },
			{ TreeEnum.GROUP, "\"<group>\"" },
			{ TreeEnum.BUILT_PRODUCTS_DIR, "BUILT_PRODUCTS_DIR" },
			{ TreeEnum.DEVELOPER_DIR, "DEVELOPER_DIR" },
			{ TreeEnum.SDKROOT, "SDKROOT" },
			{ TreeEnum.SOURCE_ROOT, "SOURCE_ROOT" }
		};
		
		public static readonly Dictionary<string, string> typeNames = new Dictionary<string, string> {
			{ ".a", "archive.ar" },
			{ ".app", "wrapper.application" },
			{ ".s", "sourcecode.asm" },
			{ ".c", "sourcecode.c.c" },
			{ ".cpp", "sourcecode.cpp.cpp" },
			{ ".cs", "sourcecode.cpp.cpp" },
			{ ".framework", "wrapper.framework" },
			{ ".h", "sourcecode.c.h" },
			{ ".icns", "image.icns" },
			{ ".m", "sourcecode.c.objc" },
			{ ".mm", "sourcecode.cpp.objcpp" },
			{ ".nib", "wrapper.nib" },
			{ ".plist", "text.plist.xml" },
			{ ".png", "image.png" },
			{ ".rtf", "text.rtf" },
			{ ".tiff", "image.tiff" },
			{ ".txt", "text" },
			{ ".xcodeproj", "wrapper.pb-project" },
			{ ".xib", "file.xib" },
			{ ".strings", "text.plist.strings" },
			{ ".bundle", "wrapper.plug-in" },
			{ ".dylib", "compiled.mach-o.dylib" }
		 };
		
		public static readonly Dictionary<string, string> typePhases = new Dictionary<string, string> {
			{ ".a", "PBXFrameworksBuildPhase" },
			{ ".app", null },
			{ ".s", "PBXSourcesBuildPhase" },
			{ ".c", "PBXSourcesBuildPhase" },
			{ ".cpp", "PBXSourcesBuildPhase" },
			{ ".cs", null },
			{ ".framework", "PBXFrameworksBuildPhase" },
			{ ".h", null },
			{ ".icns", "PBXResourcesBuildPhase" },
			{ ".m", "PBXSourcesBuildPhase" },
			{ ".mm", "PBXSourcesBuildPhase" },
			{ ".nib", "PBXResourcesBuildPhase" },
			{ ".plist", "PBXResourcesBuildPhase" },
			{ ".png", "PBXResourcesBuildPhase" },
			{ ".rtf", "PBXResourcesBuildPhase" },
			{ ".tiff", "PBXResourcesBuildPhase" },
			{ ".txt", "PBXResourcesBuildPhase" },
			{ ".xcodeproj", null },
			{ ".xib", "PBXResourcesBuildPhase" },
			{ ".strings", "PBXResourcesBuildPhase" },
			{ ".bundle", "PBXResourcesBuildPhase" },
			{ ".dylib", "PBXFrameworksBuildPhase" }
		};
		
		public PBXFileReference( string guid, PBXDictionary dictionary ) : base( guid, dictionary )
		{
			
		}
		
		public PBXFileReference( string filePath, TreeEnum tree = TreeEnum.SOURCE_ROOT ) : base()
		{
			string temp = "\"" + filePath + "\"";
			this.Add( PATH_KEY, temp );
			this.Add( NAME_KEY, System.IO.Path.GetFileName( filePath ) );
			this.Add( SOURCETREE_KEY, (string)( System.IO.Path.IsPathRooted( filePath ) ? trees[TreeEnum.ABSOLUTE] : trees[tree] ) );
			this.GuessFileType();
		}
		
		public string name {
			get {
				if( !ContainsKey( NAME_KEY ) ) {
					return null;
				}
				return (string)_data[NAME_KEY];
			}
		}
		
		private void GuessFileType()
		{
			this.Remove( EXPLICIT_FILE_TYPE_KEY );
			this.Remove( LASTKNOWN_FILE_TYPE_KEY );
			string extension = System.IO.Path.GetExtension( (string)_data[ NAME_KEY ] );
			if( !PBXFileReference.typeNames.ContainsKey( extension ) ){
				Debug.LogWarning( "Unknown file extension: " + extension + "\nPlease add extension and Xcode type to PBXFileReference.types" );
				return;
			}
			
			this.Add( LASTKNOWN_FILE_TYPE_KEY, PBXFileReference.typeNames[ extension ] );
			this.buildPhase = PBXFileReference.typePhases[ extension ];
		}
		
		private void SetFileType( string fileType )
		{
			this.Remove( EXPLICIT_FILE_TYPE_KEY );
			this.Remove( LASTKNOWN_FILE_TYPE_KEY );
			
			this.Add( EXPLICIT_FILE_TYPE_KEY, fileType );
		}
		
//	class PBXFileReference(PBXType):
//	  def __init__(self, d=None):
//		  PBXType.__init__(self, d)
//		  self.build_phase = None
//
//	  types = {
//		  '.a':('archive.ar', 'PBXFrameworksBuildPhase'),
//		  '.app': ('wrapper.application', None),
//		  '.s': ('sourcecode.asm', 'PBXSourcesBuildPhase'),
//		  '.c': ('sourcecode.c.c', 'PBXSourcesBuildPhase'),
//		  '.cpp': ('sourcecode.cpp.cpp', 'PBXSourcesBuildPhase'),
//		  '.framework': ('wrapper.framework','PBXFrameworksBuildPhase'),
//		  '.h': ('sourcecode.c.h', None),
//		  '.icns': ('image.icns','PBXResourcesBuildPhase'),
//		  '.m': ('sourcecode.c.objc', 'PBXSourcesBuildPhase'),
//		  '.mm': ('sourcecode.cpp.objcpp', 'PBXSourcesBuildPhase'),
//		  '.nib': ('wrapper.nib', 'PBXResourcesBuildPhase'),
//		  '.plist': ('text.plist.xml', 'PBXResourcesBuildPhase'),
//		  '.png': ('image.png', 'PBXResourcesBuildPhase'),
//		  '.rtf': ('text.rtf', 'PBXResourcesBuildPhase'),
//		  '.tiff': ('image.tiff', 'PBXResourcesBuildPhase'),
//		  '.txt': ('text', 'PBXResourcesBuildPhase'),
//		  '.xcodeproj': ('wrapper.pb-project', None),
//		  '.xib': ('file.xib', 'PBXResourcesBuildPhase'),
//		  '.strings': ('text.plist.strings', 'PBXResourcesBuildPhase'),
//		  '.bundle': ('wrapper.plug-in', 'PBXResourcesBuildPhase'),
//		  '.dylib': ('compiled.mach-o.dylib', 'PBXFrameworksBuildPhase')
//	  }
//
//	  trees = [
//		  '<absolute>',
//		  '<group>',
//		  'BUILT_PRODUCTS_DIR',
//		  'DEVELOPER_DIR',
//		  'SDKROOT',
//		  'SOURCE_ROOT',
//	  ]
//
//	  def guess_file_type(self):
//		  self.remove('explicitFileType')
//		  self.remove('lastKnownFileType')
//		  ext = os.path.splitext(self.get('name', ''))[1]
//
//		  f_type, build_phase = PBXFileReference.types.get(ext, ('?', None))
//
//		  self['lastKnownFileType'] = f_type
//		  self.build_phase = build_phase
//
//		  if f_type == '?':
//			  print 'unknown file extension: %s' % ext
//			  print 'please add extension and Xcode type to PBXFileReference.types'
//
//		  return f_type
//
//	  def set_file_type(self, ft):
//		  self.remove('explicitFileType')
//		  self.remove('lastKnownFileType')
//
//		  self['explicitFileType'] = ft
//
//	  @classmethod
//	  def Create(cls, os_path, tree='SOURCE_ROOT'):
//		  if tree not in cls.trees:
//			  print 'Not a valid sourceTree type: %s' % tree
//			  return None
//
//		  fr = cls()
//		  fr.id = cls.GenerateId()
//		  fr['path'] = os_path
//		  fr['name'] = os.path.split(os_path)[1]
//		  fr['sourceTree'] = '<absolute>' if os.path.isabs(os_path) else tree
//		  fr.guess_file_type()
//
//		  return fr
	}
	
	public enum TreeEnum {
		ABSOLUTE,
		GROUP,
		BUILT_PRODUCTS_DIR,
		DEVELOPER_DIR,
		SDKROOT,
		SOURCE_ROOT
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXGroup : PBXObject
	{
		protected const string NAME_KEY = "name";
		protected const string CHILDREN_KEY = "children";
		protected const string PATH_KEY = "path";
		protected const string SOURCETREE_KEY = "sourceTree";
		
		#region Constructor
		
		public PBXGroup( string name, string path = null, string tree = "SOURCE_ROOT" ) : base()
		{	
			this.Add( NAME_KEY, name );
			this.Add( CHILDREN_KEY, new PBXList() );
			
			if( path != null ) {
				this.Add( PATH_KEY, path );
				this.Add( SOURCETREE_KEY, tree );
			}
			else {
				this.Add( SOURCETREE_KEY, "\"<group>\"" );
			}

			internalNewlines = true;
		}
		
		public PBXGroup( string guid, PBXDictionary dictionary ) : base( guid, dictionary )
		{
			internalNewlines = true;
		}
		
		#endregion
		#region Properties
		
		public string name {
			get {
				if( !ContainsKey( NAME_KEY ) ) {
					return null;
				}
				return (string)_data[NAME_KEY];
			}
		}
		
		public PBXList children {
			get {
				if( !ContainsKey( CHILDREN_KEY ) ) {
					this.Add( CHILDREN_KEY, new PBXList() );
				}
				return (PBXList)_data[CHILDREN_KEY];
			}
		}
		
		public string path {
			get {
				if( !ContainsKey( PATH_KEY ) ) {
					return null;
				}
				return (string)_data[PATH_KEY];
			}
		}
		
		public string sourceTree {
			get {
				return (string)_data[SOURCETREE_KEY];
			}
		}
		
		#endregion
		
		
		public string AddChild( PBXObject child )
		{
			if( child is PBXFileReference || child is PBXGroup ) {
				children.Add( child.guid );
				return child.guid;
			}
				
			return null;
		}
		
		public void RemoveChild( string id )
		{
			if( !IsGuid( id ) )
				return;
			
			children.Remove( id );
		}
		
		public bool HasChild( string id )
		{
			if( !ContainsKey( CHILDREN_KEY ) ) {
				this.Add( CHILDREN_KEY, new PBXList() );
				return false;
			}
			
			if( !IsGuid( id ) )
				return false;
			
			return ((PBXList)_data[ CHILDREN_KEY ]).Contains( id );
		}
		
		public string GetName()
		{
			return (string)_data[ NAME_KEY ];
		}
		
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXList : ArrayList
	{
		public bool internalNewlines;
		public PBXList()
		{
			internalNewlines=true;
		}
		
		public PBXList( object firstValue )
		{
			this.Add( firstValue );
			internalNewlines=true;
		}
	}
	
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXObject
	{
		protected const string ISA_KEY = "isa";
		//
		protected string _guid;
		protected PBXDictionary _data;

		private static string guidRegex = @"[A-Fa-f0-9]{24}\s*/\*[^*]+\*/";

		public bool internalNewlines;
		
		#region Properties
		
		public string guid {
			get {
				if( string.IsNullOrEmpty( _guid ) )
					_guid = GenerateGuid();
				
				return _guid;
			}
		}
		
		public PBXDictionary data {
			get {
				if( _data == null )
					_data = new PBXDictionary();
				
				return _data;
			}
		}
		
		
		#endregion
		#region Constructors
		
		public PBXObject()
		{
			_data = new PBXDictionary();
			_data[ ISA_KEY ] = this.GetType().Name;
			_guid = GenerateGuid();
			internalNewlines = false;
		}
		
		public PBXObject( string guid ) : this()
		{
			if( IsGuid( guid ) )
				_guid = guid;
		}
		
		public PBXObject( string guid, PBXDictionary dictionary ) : this( guid )
		{
			
			if( !dictionary.ContainsKey( ISA_KEY ) || ((string)dictionary[ ISA_KEY ]).CompareTo( this.GetType().Name ) != 0 )
				Debug.LogError( "PBXDictionary is not a valid ISA object" );
			
			foreach( KeyValuePair<string, object> item in dictionary ) {
				_data[ item.Key ] = item.Value;
			}
		}
		
		#endregion
		#region Static methods
		
		public static bool IsGuid( string aString )
		{
			return System.Text.RegularExpressions.Regex.IsMatch( aString, guidRegex );
		}
		
		public static string GenerateGuid()
		{
			return System.Guid.NewGuid().ToString("N").Substring( 8 ).ToUpper();
		}
		
		
		#endregion
		#region Data manipulation
		
		public void Add( string key, object obj )
		{
			_data.Add( key, obj );
		}
		
		public bool Remove( string key )
		{
			return _data.Remove( key );
		}
		
		public bool ContainsKey( string key )
		{
			return _data.ContainsKey( key );
		}
		
		#endregion
	}
	
	public class PBXNativeTarget : PBXObject
	{
		public PBXNativeTarget() : base() {
			internalNewlines=true;
		}
		
		public PBXNativeTarget( string guid, PBXDictionary dictionary ) : base( guid, dictionary ) {	
			internalNewlines = true;
		}
	}

	public class PBXContainerItemProxy : PBXObject
	{
		public PBXContainerItemProxy() : base() {
		}
		
		public PBXContainerItemProxy( string guid, PBXDictionary dictionary ) : base( guid, dictionary ) {	
		}
	}

	public class PBXReferenceProxy : PBXObject
	{
		public PBXReferenceProxy() : base() {
		}
		
		public PBXReferenceProxy( string guid, PBXDictionary dictionary ) : base( guid, dictionary ) {	
		}
	}

	public class PBXVariantGroup : PBXObject
	{
		public PBXVariantGroup() : base() {
		}
		
		public PBXVariantGroup( string guid, PBXDictionary dictionary ) : base( guid, dictionary ) {	
		}
	}
}


namespace UnityEditor.KabamXCodeEditor
{
    public class PBXParser
    {
        public const string PBX_HEADER_TOKEN = "// !$*UTF8*$!\n";
        public const char WHITESPACE_SPACE = ' ';
        public const char WHITESPACE_TAB = '\t';
        public const char WHITESPACE_NEWLINE = '\n';
        public const char WHITESPACE_CARRIAGE_RETURN = '\r';
        public const char ARRAY_BEGIN_TOKEN = '(';
        public const char ARRAY_END_TOKEN = ')';
        public const char ARRAY_ITEM_DELIMITER_TOKEN = ',';
        public const char DICTIONARY_BEGIN_TOKEN = '{';
        public const char DICTIONARY_END_TOKEN = '}';
        public const char DICTIONARY_ASSIGN_TOKEN = '=';
        public const char DICTIONARY_ITEM_DELIMITER_TOKEN = ';';
        public const char QUOTEDSTRING_BEGIN_TOKEN = '"';
        public const char QUOTEDSTRING_END_TOKEN = '"';
        public const char QUOTEDSTRING_ESCAPE_TOKEN = '\\';
        public const char END_OF_FILE = (char)0x1A;
        public const string COMMENT_BEGIN_TOKEN = "/*";
        public const string COMMENT_END_TOKEN = "*/";
        public const string COMMENT_LINE_TOKEN = "//";
        private const int BUILDER_CAPACITY = 20000;

        private char[] data;
        private int index;
        private int indent;
    
        public PBXDictionary Decode( string data )
        {
            if( !data.StartsWith( PBX_HEADER_TOKEN ) ) {
                Debug.Log( "Wrong file format." );
                return null;
            }

            data = data.Substring( 13 );
            this.data = data.ToCharArray();
            index = 0;
            
            return (PBXDictionary)ParseValue();
        }

        public string Encode( PBXDictionary pbxData)
        {
            indent = 0;

            StringBuilder builder = new StringBuilder( PBX_HEADER_TOKEN, BUILDER_CAPACITY );
            bool success = SerializeValue( pbxData, builder);

            return ( success ? builder.ToString() : null );
        }

        #region Move

        private char NextToken()
        {
            SkipWhitespaces();
            return StepForeward();
        }
        
        private string Peek( int step = 1 )
        {
            string sneak = string.Empty;
            for( int i = 1; i <= step; i++ ) {
                if( data.Length - 1 < index + i ) {
                    break;
                }
                sneak += data[ index + i ];
            }
            return sneak;
        }

        private bool SkipWhitespaces()
        {
            bool whitespace = false;
            while( Regex.IsMatch( StepForeward().ToString(), @"\s" ) )
                whitespace = true;

            StepBackward();
            
            if( SkipComments() ) {
                whitespace = true;
                SkipWhitespaces();
            }

            return whitespace;
        }

        private bool SkipComments()
        {
            string s = string.Empty;
            string tag = Peek( 2 );
            switch( tag ) {
                case COMMENT_BEGIN_TOKEN: {
                        while( Peek( 2 ).CompareTo( COMMENT_END_TOKEN ) != 0 ) {
                            s += StepForeward();
                        }
                        s += StepForeward( 2 );
                        break;
                    }
                case COMMENT_LINE_TOKEN: {
                        while( !Regex.IsMatch( StepForeward().ToString(), @"\n" ) )
                            continue;

                        break;
                    }
                default:
                    return false;
            }
            return true;
        }
        
        private char StepForeward( int step = 1 )
        {
            index = Math.Min( data.Length, index + step );
            return data[ index ];
        }
        
        private char StepBackward( int step = 1 )
        {
            index = Math.Max( 0, index - step );
            return data[ index ];
        }

        #endregion
        #region Parse

        private object ParseValue()
        {
            switch( NextToken() ) {
                case END_OF_FILE:
                    Debug.Log( "End of file" );
                    return null;
                case DICTIONARY_BEGIN_TOKEN:
                    return ParseDictionary();
                case ARRAY_BEGIN_TOKEN:
                    return ParseArray();
                case QUOTEDSTRING_BEGIN_TOKEN:
                    return ParseString();
                default:
                    StepBackward();
                    return ParseEntity();
            }
        }
        

        private PBXDictionary ParseDictionary()
        {
            SkipWhitespaces();
            PBXDictionary dictionary = new PBXDictionary();
            string keyString = string.Empty;
            object valueObject = null;

            bool complete = false;
            while( !complete ) {
                switch( NextToken() ) {
                    case END_OF_FILE:
                        Debug.Log( "Error: reached end of file inside a dictionary: " + index );
                        complete = true;
                        break;

                    case DICTIONARY_ITEM_DELIMITER_TOKEN:
                        keyString = string.Empty;
                        valueObject = null;
                        break;

                    case DICTIONARY_END_TOKEN:
                        keyString = string.Empty;
                        valueObject = null;
                        complete = true;
                        break;

                    case DICTIONARY_ASSIGN_TOKEN:
                        valueObject = ParseValue();
                        dictionary.Add( keyString, valueObject );
                        break;

                    default:
                        StepBackward();
                        keyString = ParseValue() as string;
                        break;
                }
            }
            return dictionary;
        }

        private PBXList ParseArray()
        {
            PBXList list = new PBXList();
            bool complete = false;
            while( !complete ) {
                switch( NextToken() ) {
                    case END_OF_FILE:
                        Debug.Log( "Error: Reached end of file inside a list: " + list );
                        complete = true;
                        break;
                    case ARRAY_END_TOKEN:
                        complete = true;
                        break;
                    case ARRAY_ITEM_DELIMITER_TOKEN:
                        break;
                    default:
                        StepBackward();
                        list.Add( ParseValue() );
                        break;
                }
            }
            return list;
        }

        private object ParseString()
        {
            string s = string.Empty;
            s += "\"";
            char c = StepForeward();
            while( c != QUOTEDSTRING_END_TOKEN ) {
                s += c;

                if( c == QUOTEDSTRING_ESCAPE_TOKEN )
                    s += StepForeward();

                c = StepForeward();
            }
            s += "\"";
            return s;
        }
        
        //there has got to be a better way to do this
        private string GetDataSubstring(int begin, int length)
        {
            string res = string.Empty;
            
            
            for(int i=begin; i<begin+length && i<data.Length; i++)
            {
                res += data[i];
            }
            return res;
        }
        
        private int CountWhitespace(int pos)
        {
            int i=0;
            for(int currPos=pos; currPos<data.Length && Regex.IsMatch( GetDataSubstring(currPos, 1), @"[;,\s=]" ); i++, currPos++) {}
            return i;
        }
        
        private string ParseCommentFollowingWhitespace()
        {
            int currIdx = index+1;
            int whitespaceLength = CountWhitespace(currIdx);
            currIdx += whitespaceLength;
            
            if(currIdx + 1 >= data.Length)
                return "";
            
            
            
            if(data[currIdx] == '/' && data[currIdx+1] == '*')
            {
                
                while(!GetDataSubstring(currIdx, 2).Equals(COMMENT_END_TOKEN))
                {
                    if(currIdx >= data.Length)
                    {
                        Debug.LogError("Unterminated comment found in .pbxproj file.  Bad things are probably going to start happening");
                        return "";
                    }
                    
                    currIdx++;
                }
                
                return GetDataSubstring (index+1, (currIdx-index+1));
                
            }
            else
            {
                return "";
            }
        }
        
        private object ParseEntity()
        {
            string word = string.Empty;
            
            while(!Regex.IsMatch( Peek(), @"[;,\s=]" )) 
            {
                word += StepForeward();
            }
            
            string comment = ParseCommentFollowingWhitespace();
            if(comment.Length > 0)
            {
                word += comment;
                index += comment.Length;
            }
            
            if( word.Length != 24 && Regex.IsMatch( word, @"^\d+$" ) ) {
                return Int32.Parse( word );
            }
            
            return word;
        }

        #endregion
        #region Serialize
        
        private void AppendNewline(StringBuilder builder)
        {
            builder.Append(WHITESPACE_NEWLINE);
            for(int i=0; i<indent; i++)
            {
                builder.Append (WHITESPACE_TAB);
            }
        }
        
        private void AppendLineDelim(StringBuilder builder, bool newline)
        {
            if(newline)
            {
                AppendNewline(builder);
            }
            else
            {
                builder.Append(WHITESPACE_SPACE);
            }
        }

        private bool SerializeValue( object value, StringBuilder builder)
        {
            bool internalNewlines = false;
            if(value is PBXObject)
            {
                internalNewlines = ((PBXObject)value).internalNewlines;
            }
            else if(value is PBXDictionary)
            {
                internalNewlines = ((PBXDictionary)value).internalNewlines;
            }
            else if(value is PBXList)
            {
                internalNewlines = ((PBXList)value).internalNewlines;
            }
            
            if( value == null ) {
                builder.Append( "null" );
            }
            else if( value is PBXObject ) {
                SerializeDictionary( ((PBXObject)value).data, builder, internalNewlines);
            }
            else if( value is PBXDictionary ) {
                SerializeDictionary( (Dictionary<string, object>)value, builder, internalNewlines);
            }
            else if( value is Dictionary<string, object> ) {
                SerializeDictionary( (Dictionary<string, object>)value, builder, internalNewlines);
            }
            else if( value.GetType().IsArray ) {
                SerializeArray( new ArrayList( (ICollection)value ), builder, internalNewlines);
            }
            else if( value is ArrayList ) {
                SerializeArray( (ArrayList)value, builder, internalNewlines);
            }
            else if( value is string ) {
                SerializeString( (string)value, builder);
            }
            else if( value is Char ) {
                SerializeString( Convert.ToString( (char)value ), builder);
            }
            else if( value is bool ) {
                builder.Append( Convert.ToInt32( value ).ToString() );
            }
            else if( value.GetType().IsPrimitive ) {
                builder.Append( Convert.ToString( value ) );
            }
            else {
                Debug.LogWarning( "Error: unknown object of type " + value.GetType().Name );
                return false;
            }
    
            return true;
        }

        private bool SerializeDictionary( Dictionary<string, object> dictionary, StringBuilder builder, bool internalNewlines)
        {
            builder.Append( DICTIONARY_BEGIN_TOKEN );
            if(dictionary.Count > 0)
                indent++;
            if(internalNewlines)
                AppendNewline(builder);
             
            int i=0;
            foreach( KeyValuePair<string, object> pair in dictionary ) {
                SerializeString( pair.Key, builder );
                builder.Append( WHITESPACE_SPACE );
                builder.Append( DICTIONARY_ASSIGN_TOKEN );
                builder.Append( WHITESPACE_SPACE );
                SerializeValue( pair.Value, builder );
                builder.Append( DICTIONARY_ITEM_DELIMITER_TOKEN );
                
                if(i == dictionary.Count-1)
                    indent--;
                AppendLineDelim(builder, internalNewlines);
                i++;
            }
            
            builder.Append( DICTIONARY_END_TOKEN );
            return true;
        }

        private bool SerializeArray( ArrayList anArray, StringBuilder builder, bool internalNewlines)
        {
            builder.Append( ARRAY_BEGIN_TOKEN );
            if(anArray.Count > 0)
                indent++;
            if(internalNewlines)
                AppendNewline(builder);
            
            
            for( int i = 0; i < anArray.Count; i++ )
            {
                object value = anArray[i];
                
                if( !SerializeValue( value, builder ) )
                {
                    return false;
                }
                
                builder.Append( ARRAY_ITEM_DELIMITER_TOKEN );
                
                if(i == anArray.Count-1)
                    indent--;
                AppendLineDelim(builder, internalNewlines);
            }
            
            builder.Append( ARRAY_END_TOKEN );
            return true;
        }

        private bool SerializeString( string aString, StringBuilder builder)
        {
            // Is a GUID?
            if(PBXObject.IsGuid(aString)) {
                builder.Append( aString );
                return true;
            }

            // Is an empty string?
            if( string.IsNullOrEmpty( aString ) ) {
                builder.Append( QUOTEDSTRING_BEGIN_TOKEN );
                builder.Append( QUOTEDSTRING_END_TOKEN );
                return true;
            }

            builder.Append( aString );

            return true;
        }

        #endregion
    }
}

namespace UnityEditor.KabamXCodeEditor
{
	public class PBXProject : PBXObject
	{
		protected string MAINGROUP_KEY = "mainGroup";
		
		public PBXProject() : base() {
		}
		
		public PBXProject( string guid, PBXDictionary dictionary ) : base( guid, dictionary ) {	
		}
		
		public string mainGroupID {
			get {
				return (string)_data[ MAINGROUP_KEY ];
			}
		}
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class XCBuildConfiguration : PBXObject
	{
		protected const string BUILDSETTINGS_KEY = "buildSettings";
		protected const string HEADER_SEARCH_PATHS_KEY = "HEADER_SEARCH_PATHS";
		protected const string LIBRARY_SEARCH_PATHS_KEY = "LIBRARY_SEARCH_PATHS";
		protected const string FRAMEWORK_SEARCH_PATHS_KEY = "FRAMEWORK_SEARCH_PATHS";
		protected const string OTHER_C_FLAGS_KEY = "OTHER_CFLAGS";
		
		public XCBuildConfiguration( string guid, PBXDictionary dictionary ) : base( guid, dictionary )
		{
			internalNewlines = true;
		}
		
		public PBXDictionary buildSettings {
			get {
				if( ContainsKey( BUILDSETTINGS_KEY ) )
					return (PBXDictionary)_data[BUILDSETTINGS_KEY];
			
				return null;
			}
		}
		
		protected bool AddSearchPaths( string path, string key, bool recursive = true )
		{
			PBXList paths = new PBXList();
			paths.Add( path );
			return AddSearchPaths( paths, key, recursive );
		}
		
		protected bool AddSearchPaths( PBXList paths, string key, bool recursive = true )
		{	
			bool modified = false;
			
			if( !ContainsKey( BUILDSETTINGS_KEY ) )
				this.Add( BUILDSETTINGS_KEY, new PBXDictionary() );
			
			foreach( string path in paths ) {
				string currentPath = path;
				if( recursive && !path.EndsWith( "/**" ) )
					currentPath += "**";
				if( !((PBXDictionary)_data[BUILDSETTINGS_KEY]).ContainsKey( key ) ) {
					((PBXDictionary)_data[BUILDSETTINGS_KEY]).Add( key, new PBXList() );
				}
				else if( ((PBXDictionary)_data[BUILDSETTINGS_KEY])[key] is string ) {
					PBXList list = new PBXList();
					list.Add( ((PBXDictionary)_data[BUILDSETTINGS_KEY])[key] );
					((PBXDictionary)_data[BUILDSETTINGS_KEY])[key] = list;
				}
				
				
				if( !((PBXList)((PBXDictionary)_data[BUILDSETTINGS_KEY])[key]).Contains( currentPath ) ) {
					((PBXList)((PBXDictionary)_data[BUILDSETTINGS_KEY])[key]).Add( currentPath );
					modified = true;
				}
			}
		
			return modified;
		}
		
		public bool AddHeaderSearchPaths( PBXList paths, bool recursive = true )
		{
			return this.AddSearchPaths( paths, HEADER_SEARCH_PATHS_KEY, recursive );
		}
		
		public bool AddLibrarySearchPaths( PBXList paths, bool recursive = true )
		{
			return this.AddSearchPaths( paths, LIBRARY_SEARCH_PATHS_KEY, recursive );
		}
		
		public bool AddFrameworkSearchPaths( PBXList paths, bool recursive = true )
		{
			return this.AddSearchPaths( paths, FRAMEWORK_SEARCH_PATHS_KEY, recursive );
		}
		
		public bool AddOtherCFlags( string flag )
		{
			Debug.Log( "INIZIO 1" );
			PBXList flags = new PBXList();
			flags.Add( flag );
			return AddOtherCFlags( flags );
		}
		
		public bool AddOtherCFlags( PBXList flags )
		{
			Debug.Log( "INIZIO 2" );
			
			bool modified = false;
			
			if( !ContainsKey( BUILDSETTINGS_KEY ) )
				this.Add( BUILDSETTINGS_KEY, new PBXDictionary() );
			
			foreach( string flag in flags ) {
				
				if( !((PBXDictionary)_data[BUILDSETTINGS_KEY]).ContainsKey( OTHER_C_FLAGS_KEY ) ) {
					((PBXDictionary)_data[BUILDSETTINGS_KEY]).Add( OTHER_C_FLAGS_KEY, new PBXList() );
				}
				else if ( ((PBXDictionary)_data[BUILDSETTINGS_KEY])[ OTHER_C_FLAGS_KEY ] is string ) {
					string tempString = (string)((PBXDictionary)_data[BUILDSETTINGS_KEY])[OTHER_C_FLAGS_KEY];
					((PBXDictionary)_data[BUILDSETTINGS_KEY])[ OTHER_C_FLAGS_KEY ] = new PBXList();
					((PBXList)((PBXDictionary)_data[BUILDSETTINGS_KEY])[OTHER_C_FLAGS_KEY]).Add( tempString );
				}
				
				if( !((PBXList)((PBXDictionary)_data[BUILDSETTINGS_KEY])[OTHER_C_FLAGS_KEY]).Contains( flag ) ) {
					((PBXList)((PBXDictionary)_data[BUILDSETTINGS_KEY])[OTHER_C_FLAGS_KEY]).Add( flag );
					modified = true;
				}
			}
			
			return modified;
		}
		
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class XCConfigurationList : PBXObject
	{	
		
		public XCConfigurationList( string guid, PBXDictionary dictionary ) : base( guid, dictionary ) {	
			internalNewlines = true;
		}
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class XCFileOperationQueue : System.IDisposable
	{

		public void Dispose()
		{
			
		}
		
	}
}

namespace UnityEditor.KabamXCodeEditor 
{
	public class XCMod 
	{
		private Hashtable _datastore;
		private List<object> _libs;
		
		public string name { get; private set; }
		public string path { get; private set; }
		
		public string group {
			get {
				return (string)_datastore["group"];
			}
		}
		
		public List<object> patches
		{
			get {
				return (List<object>)_datastore["patches"];
			}
		}
		
		public List<object> libs {
			get {
				if( _libs == null ) {
					List<object> libsCast = (List<object>)_datastore["libs"];
					int count = libsCast.Count;
					
					_libs = new List<object>( count );
					foreach( string fileRef in libsCast ) {
						_libs.Add( new XCModFile( fileRef ) );
					}
				}
				return _libs;
			}
		}
		
		public List<object> librarysearchpaths {
			get {
				return (List<object>)_datastore["librarysearchpaths"];
			}
		}
		
		public List<object> frameworks {
			get {
				return (List<object>)_datastore["frameworks"];
			}
		}
		
		public List<object> frameworksearchpath {
			get {
				return (List<object>)_datastore["frameworksearchpaths"];
			}
		}
		
		public List<object> headerpaths {
			get {
				return (List<object>)_datastore["headerpaths"];
			}
		}
		
		public List<object> files {
			get {
				return (List<object>)_datastore["files"];
			}
		}
		
		public List<object> folders {
			get {
				return (List<object>)_datastore["folders"];
			}
		}
		
		public List<object> excludes {
			get {
				return (List<object>)_datastore["excludes"];
			}
		}
		
		public XCMod( string projectPath, string filename )
		{	
			FileInfo projectFileInfo = new FileInfo( filename );
			if( !projectFileInfo.Exists ) {
				Debug.LogWarning( "File does not exist." );
			}
			
			name = System.IO.Path.GetFileNameWithoutExtension( filename );
			path = projectPath;//System.IO.Path.GetDirectoryName( filename );
			
			string contents = projectFileInfo.OpenText().ReadToEnd();
			Dictionary<string, object> dictJson = Json.Deserialize(contents) as Dictionary<string,object>;;
			_datastore = new Hashtable(dictJson);
			
			
		}
		
		
		
	}
	
	public class XCModFile
	{
		public string filePath { get; private set; }
		public bool isWeak { get; private set; }
		public string sourceTree {get; private set;}
		
		public XCModFile( string inputString )
		{
			isWeak = false;
			sourceTree = "SDKROOT";
			if( inputString.Contains( ":" ) ) {
				string[] parts = inputString.Split( ':' );
				filePath = parts[0];
				isWeak = System.Array.IndexOf(parts, "weak", 1) > 0;
				
				if(System.Array.IndexOf(parts, "<group>", 1) > 0)
					sourceTree = "GROUP";
				else
					sourceTree = "SDKROOT";
				
			}
			else {
				filePath = inputString;
			}
		}
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public partial class XCProject : System.IDisposable
	{
		
		private PBXDictionary _datastore;
		public PBXDictionary _objects;
		private PBXDictionary _configurations;
		
		private PBXGroup _rootGroup;
		private string _defaultConfigurationName;
		private string _rootObjectKey;
	
		public string projectRootPath { get; private set; }
		private FileInfo projectFileInfo;
		
		public string filePath { get; private set; }
		private string sourcePathRoot;
		private bool modified = false;
		
		#region Data
		
		// Objects
		private PBXDictionary<PBXBuildFile> _buildFiles;
		private PBXDictionary<PBXGroup> _groups;
		private PBXDictionary<PBXFileReference> _fileReferences;
		private PBXDictionary<PBXNativeTarget> _nativeTargets;
		
		private PBXDictionary<PBXFrameworksBuildPhase> _frameworkBuildPhases;
		private PBXDictionary<PBXResourcesBuildPhase> _resourcesBuildPhases;
		private PBXDictionary<PBXShellScriptBuildPhase> _shellScriptBuildPhases;
		private PBXDictionary<PBXSourcesBuildPhase> _sourcesBuildPhases;
		private PBXDictionary<PBXCopyFilesBuildPhase> _copyBuildPhases;
				
		private PBXDictionary<XCBuildConfiguration> _buildConfigurations;
		private PBXDictionary<XCConfigurationList> _configurationLists;
		
		private PBXProject _project;
		
		#endregion
		#region Constructor
		
		public XCProject()
		{
			
		}
		
		public XCProject( string filePath ) : this()
		{
			if( !System.IO.Directory.Exists( filePath ) ) {
				Debug.LogWarning( "Path does not exists." );
				return;
			}
			
			if( filePath.EndsWith( ".xcodeproj" ) ) {
				this.projectRootPath = Path.GetDirectoryName( filePath );
				this.filePath = filePath;
			} else {
				string[] projects = System.IO.Directory.GetDirectories( filePath, "*.xcodeproj" );
				if( projects.Length == 0 ) {
					Debug.LogWarning( "Error: missing xcodeproj file" );
					return;
				}
				
				this.projectRootPath = filePath;
				this.filePath = projects[ 0 ];	
			}
			
			projectFileInfo = new FileInfo( Path.Combine( this.filePath, "project.pbxproj" ) );
			string contents = projectFileInfo.OpenText().ReadToEnd();
			
			PBXParser parser = new PBXParser();
			_datastore = parser.Decode( contents );
			if( _datastore == null ) {
				throw new System.Exception( "Project file not found at file path " + filePath );
			}

			if( !_datastore.ContainsKey( "objects" ) ) {
				Debug.Log( "Errore " + _datastore.Count );
				return;
			}
			
			_objects = (PBXDictionary)_datastore["objects"];
			modified = false;
			
			_rootObjectKey = (string)_datastore["rootObject"];
			if( !string.IsNullOrEmpty( _rootObjectKey ) ) {
				_project = new PBXProject( _rootObjectKey, (PBXDictionary)_objects[ _rootObjectKey ] );
				_rootGroup = new PBXGroup( _rootObjectKey, (PBXDictionary)_objects[ _project.mainGroupID ] );
			}
			else {
				Debug.LogWarning( "Error: project has no root object" );
				_project = null;
				_rootGroup = null;
			}

		}
		
		#endregion
		#region Properties
		
		public PBXProject project {
			get {
				return _project;
			}
		}
		
		public PBXGroup rootGroup {
			get {
				return _rootGroup;
			}
		}
		
		public PBXDictionary<PBXBuildFile> buildFiles {
			get {
				if( _buildFiles == null ) {
					_buildFiles = new PBXDictionary<PBXBuildFile>( _objects );
				}
				return _buildFiles;
			}
		}
		
		public PBXDictionary<PBXGroup> groups {
			get {
				if( _groups == null ) {
					_groups = new PBXDictionary<PBXGroup>( _objects );
				}
				return _groups;
			}
		}
		
		public PBXDictionary<PBXFileReference> fileReferences {
			get {
				if( _fileReferences == null ) {
					_fileReferences = new PBXDictionary<PBXFileReference>( _objects );
				}
				return _fileReferences;
			}
		}
		
		public PBXDictionary<PBXNativeTarget> nativeTargets {
			get {
				if( _nativeTargets == null ) {
					_nativeTargets = new PBXDictionary<PBXNativeTarget>( _objects );
				}
				return _nativeTargets;
			}
		}
		
		public PBXDictionary<XCBuildConfiguration> buildConfigurations {
			get {
				if( _buildConfigurations == null ) {
					_buildConfigurations = new PBXDictionary<XCBuildConfiguration>( _objects );
				}
				return _buildConfigurations;
			}
		}
		
		public PBXDictionary<XCConfigurationList> configurationLists {
			get {
				if( _configurationLists == null ) {
					_configurationLists = new PBXDictionary<XCConfigurationList>( _objects );
				}
				return _configurationLists;
			}
		}
		
		public PBXDictionary<PBXFrameworksBuildPhase> frameworkBuildPhases {
			get {
				if( _frameworkBuildPhases == null ) {
					_frameworkBuildPhases = new PBXDictionary<PBXFrameworksBuildPhase>( _objects );
				}
				return _frameworkBuildPhases;
			}
		}
	
		public PBXDictionary<PBXResourcesBuildPhase> resourcesBuildPhases {
			get {
				if( _resourcesBuildPhases == null ) {
					_resourcesBuildPhases = new PBXDictionary<PBXResourcesBuildPhase>( _objects );
				}
				return _resourcesBuildPhases;
			}
		}
	
		public PBXDictionary<PBXShellScriptBuildPhase> shellScriptBuildPhases {
			get {
				if( _shellScriptBuildPhases == null ) {
					_shellScriptBuildPhases = new PBXDictionary<PBXShellScriptBuildPhase>( _objects );
				}
				return _shellScriptBuildPhases;
			}
		}
	
		public PBXDictionary<PBXSourcesBuildPhase> sourcesBuildPhases {
			get {
				if( _sourcesBuildPhases == null ) {
					_sourcesBuildPhases = new PBXDictionary<PBXSourcesBuildPhase>( _objects );
				}
				return _sourcesBuildPhases;
			}
		}
	
		public PBXDictionary<PBXCopyFilesBuildPhase> copyBuildPhases {
			get {
				if( _copyBuildPhases == null ) {
					_copyBuildPhases = new PBXDictionary<PBXCopyFilesBuildPhase>( _objects );
				}
				return _copyBuildPhases;
			}
		}
								
		
		#endregion
		#region PBXMOD
		
		public bool AddOtherCFlags( string flag )
		{
			return AddOtherCFlags( new PBXList( flag ) ); 
		}
		
		public bool AddOtherCFlags( PBXList flags )
		{
			foreach( KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations ) {
				buildConfig.Value.AddOtherCFlags( flags );
			}
			modified = true;
			return modified;	
		}
		
		public bool AddLibrarySearchPaths( string path )
		{
			return AddLibrarySearchPaths (new PBXList(path));
		}
		
		public bool AddLibrarySearchPaths( PBXList paths)
		{
			foreach( KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations ) {
				buildConfig.Value.AddLibrarySearchPaths( paths, false );
			}
			modified = true;
			return modified;
		}
		
		public bool AddHeaderSearchPaths( string path )
		{
			return AddHeaderSearchPaths( new PBXList( path ) );
		}
		
		public bool AddHeaderSearchPaths( PBXList paths )
		{
			foreach( KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations ) {
				buildConfig.Value.AddHeaderSearchPaths( paths, false );
			}
			modified = true;
			return modified;
		}
		
		public bool AddFrameworkSearchPaths( string path )
		{
			return AddFrameworkSearchPaths( new PBXList( path ) );
		}
		
		public bool AddFrameworkSearchPaths( PBXList paths )
		{
			foreach( KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations ) {
				buildConfig.Value.AddFrameworkSearchPaths( paths, false );
			}
			modified = true;
			return modified;
		}
		
		
		
		public object GetObject( string guid )
		{
			return _objects[guid];
		}
		
		public PBXDictionary AddFile( string filePath, PBXGroup parent = null, string tree = "SOURCE_ROOT", bool createBuildFiles = true, bool weak = false )
		{
			PBXDictionary results = new PBXDictionary();
			string absPath = string.Empty;
			
			if( Path.IsPathRooted( filePath ) ) {
				absPath = filePath;
			}
			else if( tree.CompareTo( "SDKROOT" ) != 0) {
				absPath = Path.Combine( Application.dataPath, filePath );
			}
			
			if( tree.CompareTo( "SOURCE_ROOT" ) == 0 ) {
				System.Uri fileURI = new System.Uri( absPath );
				System.Uri rootURI = new System.Uri( ( projectRootPath + "/." ) );
				filePath = rootURI.MakeRelativeUri( fileURI ).ToString();
			}
			
			if( parent == null ) {
				parent = _rootGroup;
			}
			
			// TODO: Aggiungere controllo se file già presente
			PBXFileReference fileReference = GetFile( System.IO.Path.GetFileName( filePath ) ); 
			if( fileReference != null ) {
				return null;
			}
			
			fileReference = new PBXFileReference( filePath, (TreeEnum)System.Enum.Parse( typeof(TreeEnum), tree ) );
			parent.AddChild( fileReference );
			fileReferences.Add( fileReference );
			results.Add( fileReference.guid, fileReference );
			
			//Create a build file for reference
			if( !string.IsNullOrEmpty( fileReference.buildPhase ) && createBuildFiles ) {
				PBXBuildFile buildFile;
				switch( fileReference.buildPhase ) {
					case "PBXFrameworksBuildPhase":
						foreach( KeyValuePair<string, PBXFrameworksBuildPhase> currentObject in frameworkBuildPhases ) {
							buildFile = new PBXBuildFile( fileReference, weak );
							buildFiles.Add( buildFile );
							currentObject.Value.AddBuildFile( buildFile );
						}
						if ( !string.IsNullOrEmpty( absPath ) && ( tree.CompareTo( "SOURCE_ROOT" ) == 0 ) && File.Exists( absPath ) ) {
							string libraryPath = Path.Combine( "$(SRCROOT)", Path.GetDirectoryName( filePath ) );
							this.AddLibrarySearchPaths( new PBXList( libraryPath ) ); 
						}
						break;
					case "PBXResourcesBuildPhase":
						foreach( KeyValuePair<string, PBXResourcesBuildPhase> currentObject in resourcesBuildPhases ) {
							buildFile = new PBXBuildFile( fileReference, weak );
							buildFiles.Add( buildFile );
							currentObject.Value.AddBuildFile( buildFile );
						}
						break;
					case "PBXShellScriptBuildPhase":
						foreach( KeyValuePair<string, PBXShellScriptBuildPhase> currentObject in shellScriptBuildPhases ) {
							buildFile = new PBXBuildFile( fileReference, weak );
							buildFiles.Add( buildFile );
							currentObject.Value.AddBuildFile( buildFile );
						}
						break;
					case "PBXSourcesBuildPhase":
						foreach( KeyValuePair<string, PBXSourcesBuildPhase> currentObject in sourcesBuildPhases ) {
							buildFile = new PBXBuildFile( fileReference, weak );
							buildFiles.Add( buildFile );
							currentObject.Value.AddBuildFile( buildFile );
						}
						break;
					case "PBXCopyFilesBuildPhase":
						foreach( KeyValuePair<string, PBXCopyFilesBuildPhase> currentObject in copyBuildPhases ) {
							buildFile = new PBXBuildFile( fileReference, weak );
							buildFiles.Add( buildFile );
							currentObject.Value.AddBuildFile( buildFile );
						}
						break;
					case null:
						Debug.LogWarning( "fase non supportata null" );
						break;
					default:
						Debug.LogWarning( "fase non supportata def" );
						return null;
				}
			}
			
			return results;
			
		}
		
		public bool AddFolder( string folderPath, PBXGroup parent = null, string[] exclude = null, bool recursive = true, bool createBuildFile = true )
		{
			if( !Directory.Exists( folderPath ) )
				return false;
			DirectoryInfo sourceDirectoryInfo = new DirectoryInfo( folderPath );
			
			if( exclude == null )
				exclude = new string[] {};
			
			
			if( parent == null )
				parent = rootGroup;
			
			// Create group
			PBXGroup newGroup = GetGroup( sourceDirectoryInfo.Name, null /*relative path*/, parent );
			
			foreach( string directory in Directory.GetDirectories( folderPath ) )
			{
				Debug.Log( "DIR: " + directory );
				if( directory.EndsWith( ".bundle" ) ) {
					// Treath it like a file and copy even if not recursive
					Debug.LogWarning( "This is a special folder: " + directory );
					AddFile( directory, newGroup, "SOURCE_ROOT", createBuildFile );
					Debug.Log( "fatto" );
					continue;
				}
				
				if( recursive ) {
					Debug.Log( "recursive" );
					AddFolder( directory, newGroup, exclude, recursive, createBuildFile );
				}
			}
			
			// Adding files.
			string regexExclude = string.Format( @"{0}", string.Join( "|", exclude ) );
			foreach( string file in Directory.GetFiles( folderPath ) ) {
				if( Regex.IsMatch( file, regexExclude ) ) {
					continue;
				}
				AddFile( file, newGroup, "SOURCE_ROOT", createBuildFile );
			}
			
			
			modified = true;
			return modified;
		}
		
		#endregion
		#region Getters
		public PBXFileReference GetFile( string name )
		{
			if( string.IsNullOrEmpty( name ) ) {
				return null;
			}
			
			foreach( KeyValuePair<string, PBXFileReference> current in fileReferences ) {
				if( !string.IsNullOrEmpty( current.Value.name ) && current.Value.name.CompareTo( name ) == 0 ) {
					return current.Value;
				}
			}
			
			return null;
		}
		
		
		public PBXGroup GetGroup( string name, string path = null, PBXGroup parent = null )
		{
			if( string.IsNullOrEmpty( name ) )
				return null;
			
			if( parent == null )
				parent = rootGroup;
			
			foreach( KeyValuePair<string, PBXGroup> current in groups ) {
				
				if( string.IsNullOrEmpty( current.Value.name ) ) { 
					if( current.Value.path.CompareTo( name ) == 0 ) {
						return current.Value;
					}
				}
				else if( current.Value.name.CompareTo( name ) == 0 ) {
					return current.Value;
				}
			}
			
			PBXGroup result = new PBXGroup( name, path );
			groups.Add( result );
			parent.AddChild( result );
			
			modified = true;
			return result;
			
		}
			
		#endregion
		#region Mods
		
		public void ApplyMod( string rootPath, string pbxmod )
		{
			XCMod mod = new XCMod( rootPath, pbxmod );
			ApplyMod( mod );
		}
		
		internal static string AddXcodeQuotes(string path)
		{
			return "\"\\\"" + path + "\\\"\"";
		}

		public void ApplyMod( XCMod mod )
		{
			PBXGroup modGroup = this.GetGroup( mod.group );
			
			foreach( XCModFile libRef in mod.libs ) {
				string completeLibPath;
				if(libRef.sourceTree.Equals("SDKROOT")) {
					completeLibPath = System.IO.Path.Combine( "usr/lib", libRef.filePath );
				}
				else {
					completeLibPath = System.IO.Path.Combine( mod.path, libRef.filePath );
				}
					
				this.AddFile( completeLibPath, modGroup, libRef.sourceTree, true, libRef.isWeak );
			}
			
			PBXGroup frameworkGroup = this.GetGroup( "Frameworks" );
			foreach( string framework in mod.frameworks ) {
				string[] filename = framework.Split( ':' );
				bool isWeak = ( filename.Length > 1 ) ? true : false;
				string completePath = System.IO.Path.Combine( "System/Library/Frameworks", filename[0] );
				this.AddFile( completePath, frameworkGroup, "SDKROOT", true, isWeak );
			}
			
			foreach( string filePath in mod.files ) {
				string absoluteFilePath = System.IO.Path.Combine( mod.path, filePath );
				this.AddFile( absoluteFilePath, modGroup );
			}
			
			foreach( string folderPath in mod.folders ) {
				string absoluteFolderPath = AddXcodeQuotes(System.IO.Path.Combine( mod.path, folderPath ));
				this.AddFolder( absoluteFolderPath, modGroup, (string[])mod.excludes.ToArray(  ) );
			}
			
			
			foreach( string headerpath in mod.headerpaths ) {
				string absoluteHeaderPath = AddXcodeQuotes( System.IO.Path.Combine( mod.path, headerpath ) );
				this.AddHeaderSearchPaths( absoluteHeaderPath );
			}
			
			foreach( string librarypath in mod.librarysearchpaths ) {
				string absolutePath = AddXcodeQuotes(System.IO.Path.Combine( mod.path, librarypath ));
				this.AddLibrarySearchPaths( absolutePath );
			}
			
			if(mod.frameworksearchpath != null)
			{
				foreach( string frameworksearchpath in mod.frameworksearchpath ) {
					string absoluteHeaderPath = AddXcodeQuotes(System.IO.Path.Combine( mod.path, frameworksearchpath ));
					this.AddFrameworkSearchPaths( absoluteHeaderPath );
				}
			}
			
			this.Consolidate();
		}
		
		#endregion
		#region Savings
			
		public void Consolidate()
		{
			PBXDictionary consolidated = new PBXDictionary();
			consolidated.internalNewlines = true;
			consolidated.Append<PBXBuildFile>( this.buildFiles );
			consolidated.Append<PBXCopyFilesBuildPhase>( this.copyBuildPhases );
			consolidated.Append<PBXFileReference>( this.fileReferences );
			consolidated.Append<PBXFrameworksBuildPhase>( this.frameworkBuildPhases );
			consolidated.Append<PBXGroup>( this.groups );
			consolidated.Append<PBXNativeTarget>( this.nativeTargets );
			consolidated.Add( project.guid, project.data );
			consolidated.Append<PBXResourcesBuildPhase>( this.resourcesBuildPhases );
			consolidated.Append<PBXShellScriptBuildPhase>( this.shellScriptBuildPhases );
			consolidated.Append<PBXSourcesBuildPhase>( this.sourcesBuildPhases );
			consolidated.Append<XCBuildConfiguration>( this.buildConfigurations );
			consolidated.Append<XCConfigurationList>( this.configurationLists );
			
			_objects = consolidated;
			consolidated = null;
		}
		
		
		public void Backup()
		{
			string backupPath = Path.Combine( this.filePath, "project.backup.pbxproj" );
			
			// Delete previous backup file
			if( File.Exists( backupPath ) )
				File.Delete( backupPath );
			
			// Backup original pbxproj file first
			File.Copy( System.IO.Path.Combine( this.filePath, "project.pbxproj" ), backupPath );
		}
		
		/// <summary>
		/// Saves a project after editing.
		/// </summary>
		public void Save()
		{
			PBXDictionary result = new PBXDictionary();
			result.internalNewlines = true;
			result.Add( "archiveVersion", 1 );
			result.Add( "classes", new PBXDictionary() );
			result.Add( "objectVersion", 46 );
			
			Consolidate();
			result.Add( "objects", _objects );
			
			result.Add( "rootObject", _rootObjectKey );
			
			Backup();
			
			PBXParser parser = new PBXParser();
			StreamWriter saveFile = File.CreateText( System.IO.Path.Combine( this.filePath, "project.pbxproj" ) );
			saveFile.Write( parser.Encode( result ) );
			saveFile.Close();
		}
		
		/**
		* Raw project data.
		*/
		public Dictionary<string, object> objects {
			get {
				return null;
			}
		}
		
		
		#endregion
		
		public void Dispose()
		{
			
		}
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class XCSourceFile : System.IDisposable
	{

		public void Dispose()
		{
			
		}
		
	}
}

namespace UnityEditor.KabamXCodeEditor
{
	public class XCTarget : System.IDisposable
	{

		public void Dispose()
		{
			
		}
		
	}
}

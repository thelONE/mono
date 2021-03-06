// project created on 09/05/2003 at 18:07
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.IO;
using NUnit.Framework;
using System.Collections.Generic;

namespace MonoTests.System.Runtime.Serialization.Formatters.Soap {
	
	internal class NonSerializableObject {
		
	}
	
	public delegate void TrucDlg(string s);
	
	[Serializable]
	public class MoreComplexObject {
		public event TrucDlg TrucEvent;
		private string _string;
		private string[] _strings = new string[]{};
		private Queue _queue = new Queue();
		public Dictionary<object, object> _table = new Dictionary<object, object> ();

		public string ObjString {
			get { return _string; }
		}

		public MoreComplexObject() {
			TrucEvent += new TrucDlg(WriteString);
			_queue.Enqueue(1);
			_queue.Enqueue(null);
			_queue.Enqueue("foo");
			_table["foo"]="barr";
			_table[1]="foo";
			_table['c'] = "barr";
			_table["barr"] = 1234567890;
		}

		public void OnTrucEvent(string s) {
			TrucEvent(s);
		}

		public void WriteString(string s) {
			_string = s;
		}

		public override bool Equals(object obj) {
			MoreComplexObject objReturn = obj as MoreComplexObject;
			if(objReturn == null) return false;
			if(objReturn._string != this._string) return false;

			Assert.AreEqual (_table.Count, objReturn._table.Count, "#1");
			foreach(var e in objReturn._table) {
				Assert.AreEqual (e.Value, _table[e.Key], e.Key.ToString ());
			}
			return SoapFormatterTest.CheckArray(this._queue.ToArray(), objReturn._queue.ToArray());
		}
		
	}
	
	[Serializable]
	internal class MarshalObject: MarshalByRefObject {
		private string _name;
		private long _id;
		
		public MarshalObject() {
			
		}
		
		public MarshalObject(string name, long id) {
			_name = name;
			_id = id;
		}
	}
	
	[Serializable]
	internal class SimpleObject {
		private string _name;
		private int _id;
		
		public SimpleObject(string name, int id) {
			_name = name;
			_id = id;
		}
		
		public override bool Equals(object obj) {
			SimpleObject objCmp = obj as SimpleObject;
			if(objCmp == null) return false;
			if(objCmp._name != this._name) return false;
			if(objCmp._id != this._id) return false;
			return true;
		}
	}

	[Serializable]
	internal class Version1 {
		public int _value;
		
		public Version1(int value) {
			_value = value;
		}
	}

	[Serializable]
	internal class Version2: ISerializable {
	   	public int _value;
		public string _foo;

		public Version2(int value, string foo) {
		   	_value = value;
		   	_foo = foo;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
		   	info.AddValue("_value", _value);
			info.AddValue("_foo", _foo);
		}

		private Version2(SerializationInfo info, StreamingContext context) {
		    	_value = info.GetInt32("_value");
			try{
				_foo = info.GetString("_foo");
			}
			catch(SerializationException) {
			    _foo = "Default value";
			}
		}
	}
	
	public class Version1ToVersion2Binder: SerializationBinder {
		public override Type BindToType (string assemblyName, string typeName) {
			Type returnType = null;
			string typeVersion1 = "MonoTests.System.Runtime.Serialization.Formatters.Soap.Version1";
			string assemName = Assembly.GetExecutingAssembly().FullName;

			if(typeName == typeVersion1) {
				typeName = "MonoTests.System.Runtime.Serialization.Formatters.Soap.Version2";
			}

			string typeFormat = String.Format("{0}, {1}", typeName, assemName);
			returnType = Type.GetType( typeFormat);

			return returnType;
		}
	}

	[TestFixture]
	public class SoapFormatterTest
	{
		private SoapFormatter _soapFormatter;
		private SoapFormatter _soapFormatterDeserializer;
		private RemotingSurrogateSelector _surrogate;

#if DEBUG
		private void Out(MemoryStream stream, object objGraph) {
			Console.WriteLine("\n---------------------\n{0}\n", objGraph.ToString());
			stream.Position = 0;
			StreamReader r = new StreamReader(stream);
			Console.WriteLine(r.ReadToEnd());
		}			
#endif
		
		private object Serialize(object objGraph) {
			MemoryStream stream = new MemoryStream();
			Assertion.Assert(objGraph != null);
			Assertion.Assert(stream != null);
			_soapFormatter.SurrogateSelector = _surrogate;
			_soapFormatter.Serialize(stream, objGraph);
			
#if DEBUG
			Out(stream, objGraph);
#endif
			stream.Position = 0;
			
			object objReturn = _soapFormatterDeserializer.Deserialize(stream);
			Assertion.Assert(objReturn != null);
			Assertion.AssertEquals("#Tests "+objGraph.GetType(), objGraph.GetType(), objReturn.GetType());
			stream = new MemoryStream();
			_soapFormatter.Serialize(stream, objReturn);
			stream.Position = 0;
			return objReturn;
			
		}
		
		[SetUp]
		public void GetReady() {
			StreamingContext context = new StreamingContext(StreamingContextStates.All);
			_surrogate = new RemotingSurrogateSelector();
			_soapFormatter = new SoapFormatter(_surrogate, context);
			_soapFormatterDeserializer = new SoapFormatter(null, context);
		}
		
		[TearDown]
		public void Clean() {
			
		}
		
		
		[Test]
		public void TestValueTypes() {
			object objReturn;
			objReturn = Serialize((short)1);
			Assertion.AssertEquals("#int16", objReturn, 1);
			objReturn = Serialize(1);
			Assertion.AssertEquals("#int32", objReturn, 1);
			objReturn = Serialize((Single)0.1234);
			Assertion.AssertEquals("#Single", objReturn, 0.123400003f);
			objReturn = Serialize((Double)1234567890.0987654321);
			Assertion.AssertEquals("#iDouble", objReturn, 1234567890.0987654321);
			objReturn = Serialize(true);
			Assertion.AssertEquals("#Bool", objReturn, true);
			objReturn = Serialize((Int64) 1234567890);
			Assertion.AssertEquals("#Int64", objReturn, 1234567890);
			objReturn = Serialize('c');
			Assertion.AssertEquals("#Char", objReturn, 'c');
		}
		
		[Test]
		public void TestObjects() {
			object objReturn;
			objReturn = Serialize("");
			objReturn = Serialize("hello world!");
			Assertion.AssertEquals("#string", "hello world!", objReturn);
			SoapMessage soapMsg = new SoapMessage();
			soapMsg.Headers = new Header[0];
			soapMsg.MethodName = "Equals";
			soapMsg.ParamNames = new String[0];
			soapMsg.ParamTypes = new Type[0];
			soapMsg.ParamValues = new object[0];
			soapMsg.XmlNameSpace = SoapServices.CodeXmlNamespaceForClrTypeNamespace("String", "System");
			_soapFormatterDeserializer.TopObject = new SoapMessage();
			objReturn = Serialize(soapMsg);
			_soapFormatterDeserializer.TopObject = null;
			SimpleObject obj = new SimpleObject("simple object", 1);
			objReturn = Serialize(obj);
			Assertion.AssertEquals("#SimpleObject", obj, objReturn);
			objReturn = Serialize(typeof(SimpleObject));
			Assertion.AssertEquals("#Type", typeof(SimpleObject), (Type)objReturn);
			objReturn = Serialize(obj.GetType().Assembly);
			Assertion.AssertEquals("#Assembly", obj.GetType().Assembly, objReturn);
		}
		
		public static bool CheckArray(object objTest, object objReturn) {
			Array objTestAsArray = objTest as Array;
			Array objReturnAsArray = objReturn as Array;
			
			Assertion.Assert("#Not an Array "+objTest, objReturnAsArray != null);
			Assertion.AssertEquals("#Different lengths "+objTest, objTestAsArray.Length, objReturnAsArray.Length);
			
			IEnumerator iEnum = objReturnAsArray.GetEnumerator();
			iEnum.Reset();
			object obj2;
			foreach(object obj1 in objTestAsArray) {
				iEnum.MoveNext();
				obj2 = iEnum.Current;
				Assertion.AssertEquals("#The content of the 2 arrays is different", obj1, obj2);
			}
			
			return true;
		}
		
		[Test]
		public void TestArray() {
			object objReturn;
			object objTest;
			objReturn = Serialize(new int[]{});
			objTest = new int[]{1, 2, 3, 4};
			objReturn = Serialize(objTest);
			CheckArray(objTest, objReturn);
			objReturn = Serialize(new long[]{1, 2, 3, 4});
			objTest = new object[]{1, null, ":-)", 1234567890};
			objReturn = Serialize(objTest);
			objTest = new int[,]{{0, 1}, {2, 3}, {123, 4}};
			objReturn = Serialize(objTest);
			CheckArray(objTest, objReturn);
			objTest = new string[]{};
			objReturn = Serialize(objTest);
			CheckArray(objTest, objReturn);
			object[,,] objArray = new object[3,2,1];
			objArray[0,0,0] = 1;
			objArray[2,1,0] = "end";
			objReturn = Serialize(objArray);
			CheckArray(objArray, objReturn);
		}
		
		[Test]
		public void TestMarshalByRefObject() {
			Serialize(new MarshalObject("thing", 1234567890));
		}
		
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullObject() {
			MemoryStream stream = new MemoryStream();
			_soapFormatter.Serialize(stream, null);
		}
		
		[Test]
		[ExpectedException(typeof(SerializationException))]
		public void TestNonSerialisable() {
			Serialize(new NonSerializableObject());
		}

		[Test]
		public void TestMoreComplexObject() {
			MoreComplexObject objReturn;
			MoreComplexObject objTest = new MoreComplexObject();
			objReturn = (MoreComplexObject) Serialize(objTest);
			Assertion.AssertEquals("#Equals", objTest, objReturn);
			objReturn.OnTrucEvent("bidule");
			Assertion.AssertEquals("#dlg", "bidule", objReturn.ObjString);
		}

		[Test]
		public void TestSerializationbinder() {
		    	Object objReturn;
			MemoryStream stream = new MemoryStream();
			Version1 objVer1 = new Version1(123);

			_soapFormatter.SurrogateSelector = _surrogate;
			_soapFormatter.Serialize(stream, objVer1);

			stream.Position = 0;
			_soapFormatterDeserializer.Binder = new Version1ToVersion2Binder();
			objReturn = _soapFormatterDeserializer.Deserialize(stream);

			Assertion.AssertEquals("#Version1 Version2", "Version2", objReturn.GetType().Name);
			Assertion.AssertEquals("#_value", 123, ((Version2) objReturn)._value);
			Assertion.AssertEquals("#_foo", "Default value", ((Version2) objReturn)._foo);
		}
		
		[Test]
		public void TestMethodSignatureSerialization ()
		{
			Header h = new Header ("__MethodSignature", new Type [] { typeof(string),typeof(SignatureTest[]) }, false, "http://schemas.microsoft.com/clr/soap/messageProperties");

			SoapMessage msg = new SoapMessage ();
			msg.MethodName = "Run";
			msg.ParamNames = new string [] { "nom" };
			msg.ParamTypes = new Type [] { typeof(SignatureTest) };
			msg.ParamValues = new object[] { new SignatureTest () };
			msg.Headers = new Header[] { h};

			MemoryStream ms = new MemoryStream ();
			SoapFormatter sf = new SoapFormatter ();
			sf.Serialize (ms, msg);

			ms.Position = 0;

			SoapMessage t = new SoapMessage ();
			sf.TopObject = t;
			t = (SoapMessage) sf.Deserialize (ms);
			
			Assertion.AssertNotNull ("#1", t.Headers[0].Value);
			Assertion.AssertEquals ("#2", t.Headers[0].Value.GetType (), typeof(Type[]));
			
			Type[] ts = (Type[]) t.Headers[0].Value;
			
			Assertion.AssertEquals ("#3", 2, ts.Length);
			Assertion.AssertNotNull ("#4", ts[0]);
			Assertion.AssertNotNull ("#5", ts[1]);
			Console.WriteLine ("PPP:" + ts[0].GetType());
			Assertion.AssertEquals ("#6", typeof(string), ts[0]);
			Assertion.AssertEquals ("#7", typeof(SignatureTest[]), ts[1]);
		}

		[Test]
		public void TestCulture ()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			try {
				Thread.CurrentThread.CurrentCulture = new CultureInfo ("de-DE");

				var ms = new MemoryStream ();
				var test = new CultureTest ();

				_soapFormatter.Serialize(ms, test);
				ms.Position = 0;
				_soapFormatter.Deserialize(ms);
			} finally {
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}

		[Serializable]
		public class CultureTest
		{
			[OnDeserialized]
			public void OnDeserialization (StreamingContext context)
			{
				var ci = Thread.CurrentThread.CurrentCulture;
				Assertion.AssertEquals("#1", "German (Germany)", ci.EnglishName);
			}
			
			[OnSerialized]
			public void OnSerialized (StreamingContext context)
			{
				var ci = Thread.CurrentThread.CurrentCulture;
				Assertion.AssertEquals("#2", "German (Germany)", ci.EnglishName);
			}
		}
	}
	
	[Serializable]
	public class SignatureTest
	{
		public SoapQName qn = new SoapQName ("e", "name", "espai");
	}	
}

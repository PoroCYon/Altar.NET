#region Header
/**
 * JsonData.cs
 *   Generic type to hold JSON data (objects, arrays, and so on). This is
 *   the default type returned by JsonMapper.ToObject().
 *
 * The authors disclaim copyright to this source code. For more details, see
 * the COPYING file included with this distribution.
 **/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LitJson
{
    public class JsonData : IJsonWrapper, IEquatable<JsonData>, IConvertible
    {
        #region Fields
        IList<JsonData> inst_array;
        bool inst_boolean;
        double inst_double;
        int inst_int;
        long inst_long;
        IDictionary<string, JsonData> inst_object;
        string inst_string;
        string json;
        JsonType type;

        // Used to implement the IOrderedDictionary interface
        IList<KeyValuePair<string, JsonData>> object_list;
        #endregion

        #region Properties
        public int Count => EnsureCollection().Count;

        public bool IsArray => type == JsonType.Array;

        public bool IsBoolean => type == JsonType.Boolean;

        public bool IsDouble => type == JsonType.Double;

        public bool IsInt => type == JsonType.Int;

        public bool IsLong => type == JsonType.Long;

        public bool IsObject => type == JsonType.Object;

        public bool IsString => type == JsonType.String;

        public JsonType JsonType => type;
        #endregion

        #region ICollection Properties
        int ICollection.Count => Count;

        bool ICollection.IsSynchronized => EnsureCollection().IsSynchronized;

        object ICollection.SyncRoot => EnsureCollection().SyncRoot;
        #endregion
        #region IDictionary Properties
        bool IDictionary.IsFixedSize => EnsureDictionary().IsFixedSize;

        bool IDictionary.IsReadOnly => EnsureDictionary().IsReadOnly;

        ICollection IDictionary.Keys
        {
            get
            {
                EnsureDictionary();
                var keys = new List<string>();

                foreach (KeyValuePair<string, JsonData> entry in
                         object_list)
                {
                    keys.Add(entry.Key);
                }

                return (ICollection)keys;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                EnsureDictionary();
                var values = new List<JsonData>();

                foreach (KeyValuePair<string, JsonData> entry in
                         object_list)
                {
                    values.Add(entry.Value);
                }

                return (ICollection)values;
            }
        }
        #endregion
        #region IJsonWrapper Properties
        bool IJsonWrapper.IsArray => IsArray;

        bool IJsonWrapper.IsBoolean => IsBoolean;

        bool IJsonWrapper.IsDouble => IsDouble;

        bool IJsonWrapper.IsInt => IsInt;

        bool IJsonWrapper.IsLong => IsLong;

        bool IJsonWrapper.IsObject => IsObject;

        bool IJsonWrapper.IsString => IsString;
        #endregion
        #region IList Properties
        bool IList.IsFixedSize => EnsureList().IsFixedSize;

        bool IList.IsReadOnly => EnsureList().IsReadOnly;
        #endregion

        #region IDictionary Indexer
        object IDictionary.this[object key]
        {
            get
            {
                IDictionary dictionary = EnsureDictionary();
                return dictionary.Contains(key) ? dictionary[key] : null;
            }
            set
            {
                if (!(key is string))
                    throw new ArgumentException("The key has to be a string");

                JsonData data = ToJsonData(value);

                this[(string)key] = data;
            }
        }
        #endregion
        #region IOrderedDictionary Indexer
        object IOrderedDictionary.this[int idx]
        {
            get
            {
                EnsureDictionary();
                return object_list[idx].Value;
            }
            set
            {
                EnsureDictionary();
                JsonData data = ToJsonData(value);

                KeyValuePair<string, JsonData> old_entry = object_list[idx];

                inst_object[old_entry.Key] = data;

                var entry =
                    new KeyValuePair<string, JsonData>(old_entry.Key, data);

                object_list[idx] = entry;
            }
        }
        #endregion
        #region IList Indexer
        object IList.this[int index]
        {
            get
            {
                return EnsureList()[index];
            }
            set
            {
                EnsureList();
                JsonData data = ToJsonData(value);

                this[index] = data;
            }
        }
        #endregion

        #region Public Indexers
        public JsonData this[string prop_name]
        {
            get
            {
                EnsureDictionary();
                return inst_object.ContainsKey(prop_name) ? inst_object[prop_name] : null;
            }
            set
            {
                EnsureDictionary();

                var entry =
                    new KeyValuePair<string, JsonData>(prop_name, value);

                if (inst_object.ContainsKey(prop_name))
                {
                    for (int i = 0; i < object_list.Count; i++)
                    {
                        if (object_list[i].Key == prop_name)
                        {
                            object_list[i] = entry;
                            break;
                        }
                    }
                }
                else
                    object_list.Add(entry);

                inst_object[prop_name] = value;

                json = null;
            }
        }

        public JsonData this[int index]
        {
            get
            {
                EnsureCollection();

                if (type == JsonType.Array)
                    return inst_array[index];

                return object_list[index].Value;
            }
            set
            {
                EnsureCollection();

                if (type == JsonType.Array)
                    inst_array[index] = value;
                else
                {
                    KeyValuePair<string, JsonData> entry = object_list[index];
                    var new_entry =
                        new KeyValuePair<string, JsonData>(entry.Key, value);

                    object_list[index] = new_entry;
                    inst_object[entry.Key] = value;
                }

                json = null;
            }
        }
        #endregion

        #region Constructors
        public JsonData()
        {
        }

        public JsonData(bool boolean)
        {
            type = JsonType.Boolean;
            inst_boolean = boolean;
        }

        public JsonData(double number)
        {
            type = JsonType.Double;
            inst_double = number;
        }
        public JsonData(float number)
        {
            type = JsonType.Double;
            inst_double = number;
        }

        public JsonData(int number)
        {
            type = JsonType.Int;
            inst_int = number;
        }

        public JsonData(long number)
        {
            type = JsonType.Long;
            inst_long = number;
        }

        public JsonData(object obj)
        {
            if (obj is bool)
            {
                type = JsonType.Boolean;
                inst_boolean = (bool)obj;
                return;
            }

            if (obj is float)
            {
                type = JsonType.Double;
                inst_double = Math.Round((float)obj, 8);
                return;
            }
            if (obj is double)
            {
                type = JsonType.Double;
                inst_double = (double)obj;
                return;
            }

            if (obj is int || obj is ushort || obj is short || obj is byte || obj is sbyte)
            {
                type = JsonType.Int;
                inst_int = Convert.ToInt32(obj);
                return;
            }

            if (obj is long || obj is uint)
            {
                type = JsonType.Long;
                inst_long = Convert.ToInt64(obj);
                return;
            }

            if (obj is string)
            {
                type = JsonType.String;
                inst_string = obj.ToString();
                return;
            }

            throw new ArgumentException("Unable to wrap the given object with JsonData");
        }

        public JsonData(string str)
        {
            type = JsonType.String;
            inst_string = str;
        }
        #endregion

        #region Implicit Conversions
        public static implicit operator JsonData(bool   data) => new JsonData(data);
        public static implicit operator JsonData(double data) => new JsonData(data);
        public static implicit operator JsonData(float  data) => new JsonData(data);
        public static implicit operator JsonData(int    data) => new JsonData(data);
        public static implicit operator JsonData(long   data) => new JsonData(data);
        public static implicit operator JsonData(string data) => new JsonData(data);

        public static implicit operator JsonData(byte    data) => new JsonData(data);
        public static implicit operator JsonData(sbyte   data) => new JsonData(data);
        public static implicit operator JsonData(short   data) => new JsonData(data);
        public static implicit operator JsonData(ushort  data) => new JsonData(data);
        public static implicit operator JsonData(uint    data) => new JsonData(data);
        public static implicit operator JsonData(ulong   data) => new JsonData(data);
        public static implicit operator JsonData(decimal data) => new JsonData(data);

        public static implicit operator JsonData(char     data) => new JsonData(data.ToString());
        public static implicit operator JsonData(DateTime data) => new JsonData(data.ToString(DATETIME_FORMAT));
        #endregion
        #region Explicit Conversions
        public static explicit operator bool  (JsonData   data)
        {
            if (data == null) return default(bool);
            if (data.type != JsonType.Boolean) throw new InvalidCastException("Instance of JsonData doesn't hold a bool");
            return data.inst_boolean;
        }
        public static explicit operator float (JsonData data) => (float)(double)data;
        public static explicit operator double(JsonData data)
        {
            if (data == null) return default(double);
            if (data.type != JsonType.Double && data.type != JsonType.Int) throw new InvalidCastException("Instance of JsonData doesn't hold a double");
            return data.type == JsonType.Double ? data.inst_double : data.inst_int;
        }
        public static explicit operator int   (JsonData data)
        {
            if (data == null) return default(int);
            if (data.type != JsonType.Int) throw new InvalidCastException("Instance of JsonData doesn't hold an int");
            return data.inst_int;
        }
        public static explicit operator long  (JsonData data)
        {
            if (data == null) return default(long);
            if (data.type != JsonType.Long && data.type != JsonType.Int) throw new InvalidCastException("Instance of JsonData doesn't hold a long");
            return data.type == JsonType.Long ? data.inst_long : data.inst_int;
        }
        public static explicit operator string(JsonData data)
        {
            if (data == null) return default(string);
            if (data.type != JsonType.String) throw new InvalidCastException("Instance of JsonData doesn't hold a string");
            return data.inst_string;
        }

        public static explicit operator byte   (JsonData data) => unchecked((byte   )(int   )data);
        public static explicit operator sbyte  (JsonData data) => unchecked((sbyte  )(int   )data);
        public static explicit operator short  (JsonData data) => unchecked((short  )(int   )data);
        public static explicit operator ushort (JsonData data) => unchecked((ushort )(int   )data);
        public static explicit operator uint   (JsonData data) => unchecked((uint   )(long  )data);
        public static explicit operator ulong  (JsonData data) => unchecked((ulong  )(long  )data);
        public static explicit operator decimal(JsonData data) => unchecked((decimal)(double)data);

        public static explicit operator char    (JsonData data) => ((string)data)[0];
        public static explicit operator DateTime(JsonData data) => DateTime.Parse((string)data, CultureInfo.InvariantCulture);
        #endregion

        #region ICollection Methods
        void ICollection.CopyTo(Array array, int index)
        {
            EnsureCollection().CopyTo(array, index);
        }
        #endregion
        #region IDictionary Methods
        void IDictionary.Add(object key, object value)
        {
            JsonData data = ToJsonData(value);

            EnsureDictionary().Add(key, data);

            var entry =
                new KeyValuePair<string, JsonData>((string)key, data);
            object_list.Add(entry);

            json = null;
        }

        void IDictionary.Clear()
        {
            EnsureDictionary().Clear();
            object_list.Clear();
            json = null;
        }

        bool IDictionary.Contains(object key) => EnsureDictionary().Contains(key);

        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IOrderedDictionary)this).GetEnumerator();

        void IDictionary.Remove(object key)
        {
            EnsureDictionary().Remove(key);

            for (int i = 0; i < object_list.Count; i++)
            {
                if (object_list[i].Key == (string)key)
                {
                    object_list.RemoveAt(i);
                    break;
                }
            }

            json = null;
        }
        #endregion
        #region IEnumerable Methods
        IEnumerator IEnumerable.GetEnumerator() => EnsureCollection().GetEnumerator();
        #endregion
        #region IJsonWrapper Methods
        bool IJsonWrapper.GetBoolean()
        {
            if (type != JsonType.Boolean)
                throw new InvalidOperationException(
                    "JsonData instance doesn't hold a boolean");

            return inst_boolean;
        }

        double IJsonWrapper.GetDouble()
        {
            if (type != JsonType.Double)
                throw new InvalidOperationException(
                    "JsonData instance doesn't hold a double");

            return inst_double;
        }

        int IJsonWrapper.GetInt()
        {
            if (type != JsonType.Int)
                throw new InvalidOperationException(
                    "JsonData instance doesn't hold an int");

            return inst_int;
        }

        long IJsonWrapper.GetLong()
        {
            if (type != JsonType.Long)
                throw new InvalidOperationException(
                    "JsonData instance doesn't hold a long");

            return inst_long;
        }

        string IJsonWrapper.GetString()
        {
            if (type != JsonType.String)
                throw new InvalidOperationException(
                    "JsonData instance doesn't hold a string");

            return inst_string;
        }

        void IJsonWrapper.SetBoolean(bool val)
        {
            type = JsonType.Boolean;
            inst_boolean = val;
            json = null;
        }

        void IJsonWrapper.SetDouble(double val)
        {
            type = JsonType.Double;
            inst_double = val;
            json = null;
        }

        void IJsonWrapper.SetInt(int val)
        {
            type = JsonType.Int;
            inst_int = val;
            json = null;
        }

        void IJsonWrapper.SetLong(long val)
        {
            type = JsonType.Long;
            inst_long = val;
            json = null;
        }

        void IJsonWrapper.SetString(string val)
        {
            type = JsonType.String;
            inst_string = val;
            json = null;
        }

        string IJsonWrapper.ToJson() => ToJson();

        void IJsonWrapper.ToJson(JsonWriter writer)
        {
            ToJson(writer);
        }
        #endregion
        #region IList Methods
        int IList.Add(object value) => Add(value);

        void IList.Clear()
        {
            EnsureList().Clear();
            json = null;
        }

        bool IList.Contains(object value) => EnsureList().Contains(value);

        int IList.IndexOf(object value) => EnsureList().IndexOf(value);

        void IList.Insert(int index, object value)
        {
            EnsureList().Insert(index, value);
            json = null;
        }

        void IList.Remove(object value)
        {
            EnsureList().Remove(value);
            json = null;
        }

        void IList.RemoveAt(int index)
        {
            EnsureList().RemoveAt(index);
            json = null;
        }
        #endregion
        #region IOrderedDictionary Methods
        IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
        {
            EnsureDictionary();

            return new OrderedDictionaryEnumerator(
                object_list.GetEnumerator());
        }

        void IOrderedDictionary.Insert(int idx, object key, object value)
        {
            var property = (string)key;
            JsonData data = ToJsonData(value);

            this[property] = data;

            var entry =
                new KeyValuePair<string, JsonData>(property, data);

            object_list.Insert(idx, entry);
        }

        void IOrderedDictionary.RemoveAt(int idx)
        {
            EnsureDictionary();

            inst_object.Remove(object_list[idx].Key);
            object_list.RemoveAt(idx);
        }
        #endregion

        #region Private Methods
        ICollection EnsureCollection()
        {
            if (type == JsonType.Array)
                return (ICollection)inst_array;

            if (type == JsonType.Object)
                return (ICollection)inst_object;

            throw new InvalidOperationException("This JsonData is not a collection -or- is not initialised.");
                //"The JsonData instance has to be initialized first");
        }
        IDictionary EnsureDictionary()
        {
            if (type == JsonType.Object)
                return (IDictionary)inst_object;

            if (type != JsonType.None)
                throw new InvalidOperationException(
                    "Instance of JsonData is not a dictionary");

            type = JsonType.Object;
            inst_object = new Dictionary<string, JsonData>();
            object_list = new List<KeyValuePair<string, JsonData>>();

            return (IDictionary)inst_object;
        }
        IList EnsureList()
        {
            if (type == JsonType.Array)
                return (IList)inst_array;

            if (type != JsonType.None)
                throw new InvalidOperationException(
                    "Instance of JsonData is not a list");

            type = JsonType.Array;
            inst_array = new List<JsonData>();

            return (IList)inst_array;
        }

        JsonData ToJsonData(object obj)
        {
            if (obj == null)
                return null;

            if (obj is JsonData)
                return (JsonData)obj;

            return new JsonData(obj);
        }

        static void WriteJson(IJsonWrapper obj, JsonWriter writer)
        {
            if (obj == null)
            {
                writer.Write(null);
                return;
            }

            if (obj.IsString)
            {
                writer.Write(obj.GetString());
                return;
            }

            if (obj.IsBoolean)
            {
                writer.Write(obj.GetBoolean());
                return;
            }

            if (obj.IsDouble)
            {
                writer.Write(obj.GetDouble());
                return;
            }

            if (obj.IsInt)
            {
                writer.Write(obj.GetInt());
                return;
            }

            if (obj.IsLong)
            {
                writer.Write(obj.GetLong());
                return;
            }

            if (obj.IsArray)
            {
                writer.WriteArrayStart();
                foreach (object elem in (IList)obj)
                    WriteJson((JsonData)elem, writer);
                writer.WriteArrayEnd();

                return;
            }

            if (obj.IsObject)
            {
                writer.WriteObjectStart();

                foreach (DictionaryEntry entry in ((IDictionary)obj))
                {
                    writer.WritePropertyName((string)entry.Key);
                    WriteJson((JsonData)entry.Value, writer);
                }
                writer.WriteObjectEnd();

                return;
            }
        }
        #endregion

        public int Add(object value)
        {
            JsonData data = ToJsonData(value);

            json = null;

            return EnsureList().Add(data);
        }

        public bool Has(string key)
        {
            foreach (KeyValuePair<string, JsonData> kvp in object_list) if (kvp.Key == key) return true;
            return false;
        }

        public bool Remove(string key)
        {
            for (int i = 0; i < object_list.Count; i++)
            {
                if (object_list[i].Key == key)
                {
                    object_list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            if (IsObject)
            {
                ((IDictionary)this).Clear();
                return;
            }

            if (IsArray)
            {
                ((IList)this).Clear();
                return;
            }
        }

        public bool Equals(JsonData x)
        {
            if (x == null)
                return false;

            if (x.type != this.type)
                return false;

            switch (this.type)
            {
                case JsonType.None:
                    return true;

                case JsonType.Object:
                    return this.inst_object.Equals(x.inst_object);

                case JsonType.Array:
                    return this.inst_array.Equals(x.inst_array);

                case JsonType.String:
                    return this.inst_string.Equals(x.inst_string);

                case JsonType.Int:
                    return this.inst_int.Equals(x.inst_int);

                case JsonType.Long:
                    return this.inst_long.Equals(x.inst_long);

                case JsonType.Double:
                    return this.inst_double.Equals(x.inst_double);

                case JsonType.Boolean:
                    return this.inst_boolean.Equals(x.inst_boolean);
            }

            return false;
        }

        public JsonType GetJsonType() => type;

        public void SetJsonType(JsonType type)
        {
            if (this.type == type)
                return;

            switch (type)
            {
                case JsonType.None:
                    break;

                case JsonType.Object:
                    inst_object = new Dictionary<string, JsonData>();
                    object_list = new List<KeyValuePair<string, JsonData>>();
                    break;

                case JsonType.Array:
                    inst_array = new List<JsonData>();
                    break;

                case JsonType.String:
                    inst_string = default(string);
                    break;

                case JsonType.Int:
                    inst_int = default(int);
                    break;

                case JsonType.Long:
                    inst_long = default(long);
                    break;

                case JsonType.Double:
                    inst_double = default(double);
                    break;

                case JsonType.Boolean:
                    inst_boolean = default(bool);
                    break;
            }

            this.type = type;
        }

        public string ToJson()
        {
            if (json != null)
                return json;

            var sw = new StringWriter();
            var writer = new JsonWriter(sw);
            writer.Validate = false;

            WriteJson(this, writer);
            json = sw.ToString();

            return json;
        }

        public void ToJson(JsonWriter writer)
        {
            bool old_validate = writer.Validate;

            writer.Validate = false;

            WriteJson(this, writer);

            writer.Validate = old_validate;
        }

        public override string ToString()
        {
            switch (type)
            {
                case JsonType.Array:
                    return "JsonData array";

                case JsonType.Boolean:
                    return inst_boolean.ToString();

                case JsonType.Double:
                    return inst_double.ToString();

                case JsonType.Int:
                    return inst_int.ToString();

                case JsonType.Long:
                    return inst_long.ToString();

                case JsonType.Object:
                    return "JsonData object";

                case JsonType.String:
                    return inst_string;
            }

            return "Uninitialized JsonData";
        }

        #region IConvertible Methods
        TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

        bool    IConvertible.ToBoolean(IFormatProvider provider) => (bool)this;
        char    IConvertible.ToChar   (IFormatProvider provider) => ((string)this)[0];
        sbyte   IConvertible.ToSByte  (IFormatProvider provider) => unchecked((sbyte )(int)this);
        byte    IConvertible.ToByte   (IFormatProvider provider) => unchecked((byte  )(int)this);
        short   IConvertible.ToInt16  (IFormatProvider provider) => unchecked((short )(int)this);
        ushort  IConvertible.ToUInt16 (IFormatProvider provider) => unchecked((ushort)(int)this);
        int     IConvertible.ToInt32  (IFormatProvider provider) => (int)this;
        uint    IConvertible.ToUInt32 (IFormatProvider provider) => unchecked((uint)(ulong)this);
        long    IConvertible.ToInt64  (IFormatProvider provider) => (long)this;
        ulong   IConvertible.ToUInt64 (IFormatProvider provider) => unchecked((ulong)(long)this);
        float   IConvertible.ToSingle (IFormatProvider provider) => (float)this;
        double  IConvertible.ToDouble (IFormatProvider provider) => (double)this;
        decimal IConvertible.ToDecimal(IFormatProvider provider) => (decimal)(double)this;

        readonly static string DATETIME_FORMAT = "s";
        // ISO 8601
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => DateTime.Parse((string)this, CultureInfo.InvariantCulture);

        string IConvertible.ToString(IFormatProvider provider) => (string)this;

        object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            unchecked
            {
                if (type == typeof(bool))
                    return (bool)this;
                if (type == typeof(int))
                    return (int)this;
                if (type == typeof(long))
                    return (long)this;
                if (type == typeof(double))
                    return (double)this;
                if (type == typeof(string))
                    return (string)this;

                if (type == typeof(char))
                    return ((string)this)[0];
                if (type == typeof(sbyte))
                    return (sbyte)(int)this;
                if (type == typeof(byte))
                    return (byte)(int)this;
                if (type == typeof(short))
                    return (short)(int)this;
                if (type == typeof(ushort))
                    return (ushort)(int)this;
                if (type == typeof(uint))
                    return (uint)(long)this;
                if (type == typeof(ulong))
                    return (ulong)this;
                if (type == typeof(float))
                    return (float)(double)this;
                if (type == typeof(decimal))
                    return (decimal)(double)this;

                if (type == typeof(DateTime))
                    return ((IConvertible)this).ToDateTime(provider);
            }

            throw new InvalidCastException();
        }
        #endregion

        public JsonData[] ToArray()
        {
            EnsureList();

            return inst_array.ToArray();
        }
        public IList<JsonData> ToList()
        {
            EnsureList();

            return inst_array;
        }
        public IDictionary<string, JsonData> ToDictionary()
        {
            EnsureDictionary();

            return inst_object;
        }
    }

    class OrderedDictionaryEnumerator : IDictionaryEnumerator
    {
        readonly IEnumerator<KeyValuePair<string, JsonData>> list_enumerator;


        public object Current => Entry;

        public DictionaryEntry Entry
        {
            get
            {
                KeyValuePair<string, JsonData> curr = list_enumerator.Current;
                return new DictionaryEntry(curr.Key, curr.Value);
            }
        }

        public object Key => list_enumerator.Current.Key;

        public object Value => list_enumerator.Current.Value;


        public OrderedDictionaryEnumerator(
            IEnumerator<KeyValuePair<string, JsonData>> enumerator)
        {
            list_enumerator = enumerator;
        }


        public bool MoveNext() => list_enumerator.MoveNext();

        public void Reset()
        {
            list_enumerator.Reset();
        }
    }
}

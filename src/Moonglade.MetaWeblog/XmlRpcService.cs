using System;
using System.Reflection;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WilderMinds.MetaWeblog
{
  public class XmlRpcService
  {
    public XmlRpcService(ILogger logger)
    {
      _logger = logger;
    }

    private string _method;
    private ILogger _logger;

    public async Task<string> InvokeAsync(string xml)
    {
      try
      {
        var doc = XDocument.Parse(xml);
        var methodNameElement = doc
          .Descendants("methodName")
          .FirstOrDefault();
        if (methodNameElement != null)
        {
          _method = methodNameElement.Value;

          _logger.LogDebug($"Invoking {_method} on XMLRPC Service");

          var theType = GetType();

          foreach (var typeMethod in theType.GetMethods())
          {
            var attr = typeMethod.GetCustomAttribute<XmlRpcMethodAttribute>();
            if (attr != null && _method.ToLower() == attr.MethodName.ToLower())
            {
              var parameters = GetParameters(doc);
              Task resultTask = (Task)typeMethod.Invoke(this, parameters);
              await resultTask;

              // get result via reflection
              PropertyInfo resultProperty = resultTask.GetType().GetProperty("Result");
              object result = resultProperty.GetValue(resultTask);

              return SerializeResponse(result);
            }
          }
        }
      }
      catch (MetaWeblogException ex)
      {
        return SerializeResponse(ex);
      }
      catch (Exception ex)
      {
        _logger.LogError($"Exception thrown during serialization: Exception: {ex}");
        return SerializeResponse(new MetaWeblogException($"Exception during XmlRpcService call: {ex.Message}"));
      }

      return SerializeResponse(new MetaWeblogException("Failed to handle XmlRpcService call"));
    }

    private string SerializeResponse(object result)
    {
      var doc = new XDocument();
      var response = new XElement("methodResponse");
      doc.Add(response);

      if (result is MetaWeblogException)
      {
        response.Add(SerializeFaultResponse((MetaWeblogException)result));
      }
      else
      {
        var theParams = new XElement("params");
        response.Add(theParams);

        SerializeResponseParameters(theParams, result);
      }

      return doc.ToString(SaveOptions.None);
    }

    private XElement SerializeValue(object result)
    {
      var theType = result.GetType();
      XElement newElement = new XElement("value");

      if (theType == typeof(int))
      {
        newElement.Add(new XElement("i4", result.ToString()));
      }
      else if (theType == typeof(long))
      {
        newElement.Add(new XElement("long", result.ToString()));
      }
      else if (theType == typeof(double))
      {
        newElement.Add(new XElement("double", result.ToString()));
      }
      else if (theType == typeof(bool))
      {
        newElement.Add(new XElement("boolean", ((bool)result) ? 1 : 0));
      }
      else if (theType == typeof(string))
      {
        newElement.Add(new XElement("string", result.ToString()));
      }
      else if (theType == typeof(DateTime))
      {
        var date = (DateTime)result;
        newElement.Add(new XElement("dateTime.iso8601", date.ToString("yyyyMMdd'T'HH':'mm':'ss",
                        DateTimeFormatInfo.InvariantInfo)));
      }
      else if (result is IEnumerable)
      {
        var data = new XElement("data");
        foreach (var item in ((IEnumerable)result))
        {
          data.Add(SerializeValue(item));
        }
        newElement.Add(new XElement("array", data));
      }
      else
      {
        var theStruct = new XElement("struct");
        // Reference Type
        foreach (var field in theType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
          var member = new XElement("member");
          member.Add(new XElement("name", field.Name));
          var value = field.GetValue(result);
          if (value != null)
          {
            member.Add(SerializeValue(value));
            theStruct.Add(member);
          }
        }
        newElement.Add(theStruct);
      }

      return newElement;
    }

    private void SerializeResponseParameters(XElement theParams, object result)
    {
      theParams.Add(new XElement("param", SerializeValue(result)));
    }

    private XElement CreateStringValue(string typeName, string value)
    {
      return new XElement("value", new XElement(typeName, value));
    }

    private XElement SerializeFaultResponse(MetaWeblogException result)
    {
      return new XElement("fault",
        new XElement("value",
          new XElement("struct",
            new XElement("member",
              new XElement("name", "faultCode"),
              CreateStringValue("int", result.Code.ToString())),
            new XElement("member",
              new XElement("name", "faultString"),
              CreateStringValue("string", result.Message)))
            ));
    }

    private object[] GetParameters(XDocument doc)
    {
      var parameters = new List<object>();
      var paramsEle = doc.Descendants("params");

      // Handle no parameters
      if (paramsEle == null)
      {
        return parameters.ToArray();
      }

      foreach (var p in paramsEle.Descendants("param"))
      {
        parameters.AddRange(ParseValue(p.Element("value")));
      }

      return parameters.ToArray();
    }

    private List<object> ParseValue(XElement value)
    {
      var type = value.Descendants().FirstOrDefault();
      if (type != null)
      {
        var typename = type.Name.LocalName;
        switch (typename)
        {
          case "array":
            return ParseArray(type);
          case "struct":
            return ParseStruct(type);
          case "i4":
          case "int":
            return ParseInt(type);
          case "i8":
            return ParseLong(type);
          case "string":
            return ParseString(type);
          case "boolean":
            return ParseBoolean(type);
          case "double":
            return ParseDouble(type);
          case "dateTime.iso8601":
            return ParseDateTime(type);
          case "base64":
            return ParseBase64(type);
        }
      }

      throw new MetaWeblogException("Failed to parse parameters");

    }

    private List<object> ParseBase64(XElement type)
    {
      return new List<object> { type.Value };
    }

    private List<object> ParseLong(XElement type)
    {
      return new List<object> { long.Parse(type.Value) };
    }

    private List<object> ParseDateTime(XElement type)
    {
      DateTime parsed;

      if (DateTime8601.TryParseDateTime8601(type.Value, out parsed))
      {
        return new List<object>() { parsed };
      }

      throw new MetaWeblogException("Failed to parse date");
    }

    private List<object> ParseBoolean(XElement type)
    {
      return new List<object> { type.Value == "1" };
    }

    private List<object> ParseString(XElement type)
    {
      return new List<object> { type.Value };
    }

    private List<object> ParseDouble(XElement type)
    {
      return new List<object> { double.Parse(type.Value) };
    }

    private List<object> ParseInt(XElement type)
    {
      return new List<object> { int.Parse(type.Value) };
    }

    private List<object> ParseStruct(XElement type)
    {
      var dict = new Dictionary<string, object>();
      var members = type.Descendants("member");
      foreach (var member in members)
      {
        var name = member.Element("name").Value;
        var value = ParseValue(member.Element("value"));
        dict[name] = value;
      }

      switch (_method)
      {
        case "metaWeblog.newMediaObject":
          return ConvertToType<MediaObject>(dict);
        case "metaWeblog.newPost":
        case "metaWeblog.editPost":
          return ConvertToType<Post>(dict);
        case "wp.newCategory":
          return ConvertToType<NewCategory>(dict);
        case "wp.newPage":
        case "wp.editPage":
          return ConvertToType<Page>(dict);
        default:
          throw new InvalidOperationException("Unknown type of struct discovered.");
      }

    }

    private List<object> ConvertToType<T>(Dictionary<string, object> dict) where T : new()
    {
      var info = typeof(T).GetTypeInfo();
    
      // Convert it
      var result = new T();
      foreach (var key in dict.Keys)
      {
        var field = info.GetDeclaredField(key);
        if (field != null)
        {
          var container = (List<object>)dict[key];
          object value = container.Count() == 1 ? container.First() : container.ToArray();
          if (field.FieldType != value.GetType())
          {
            if (field.FieldType.IsArray && value.GetType().IsArray)
            {
              var valueArray = (Array)value;
              var newValue = Array.CreateInstance(field.FieldType.GetElementType(), valueArray.Length);
              Array.Copy(valueArray, newValue, valueArray.Length);
              value = newValue;
            }
            else if (value.GetType().IsAssignableFrom(field.FieldType))
            {
              value = Convert.ChangeType(value, field.FieldType);
            }
            else
            {
              _logger.LogWarning($"Skipping conversion to type as not supported: {field.FieldType.Name}");
              continue;
            }
          }
          field.SetValue(result, value);
        }
        else
        {
          _logger.LogWarning($"Skipping field {key} when converting to {typeof(T).Name}");
        }
      }

      Debug.WriteLine(result);

      return new List<object>() { result };
    }

    private List<object> ParseArray(XElement type)
    {
      try
      {
        var result = new List<object>();
        var data = type.Element("data");
        foreach (var ele in data.Elements())
        {
          result.AddRange(ParseValue(ele));
        }
        return new List<object>() { result.ToArray() }; // make an array;
      }
      catch (Exception ex)
      {
        _logger.LogCritical($"Failed to Parse Array: {type}");
        throw ex;
      }
    }
  }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Just.Base.Utils
{
    /// <summary>
    /// 构造Soap请求，访问Webservice并获得返回结果
    /// </summary>
    public class WebServiceCaller
	{

		//缓存xmlNamespace，避免重复调用GetNamespace
		private Hashtable _xmlNamespace;

		#region Porpertity


		/// <summary>
		/// WebService Url地址
		/// </summary>
		/// <remarks></remarks>
		public string WebserviceUrl { get; set; }

		/// <summary>
		/// WSDL Url地址
		/// </summary>
		/// <remarks></remarks>
		public string WsWsdlUrl { get; set; }

		/// <summary>
		/// 用户名
		/// </summary>
		/// <remarks></remarks>
		public string UserName { get; set; }

		/// <summary>
		/// 密码
		/// </summary>
		/// <remarks></remarks>
		public string PassWord { get; set; }

		/// <summary>
		/// 调用服务类型
		/// 0-DotNet(默认);1-Java
		/// </summary>
		public int? WssSvcType { get; set; }

		/// <summary>
		/// WssSoapAction
		/// </summary>
		/// <remarks></remarks>
		public string WssSoapAction { get; set; }

		private CookieContainer CookieContainer { get; }

		#endregion

		#region Constructor

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="wsUrl"></param>
		/// <param name="wsWsdlUrl"></param>
		/// <param name="wssSvcType"></param>
		/// <param name="wssSoapAction"></param>
		/// <param name="userName"></param>
		/// <param name="passWord"></param>
		/// <param name="xmlns"></param>
		public WebServiceCaller(string wsUrl, string wsWsdlUrl = "", int? wssSvcType = 0, string wssSoapAction = "", string userName = "", string passWord = "", string xmlns = "")
		{
			_xmlNamespace = WssNameSpaceCache.Instance.NsHashtable;

			if (!string.IsNullOrEmpty(wsWsdlUrl))
			{
				if (!string.IsNullOrEmpty(wsUrl) && wsUrl.ToLower().Contains("?wsdl"))
				{
					WebserviceUrl = wsUrl.Substring(0, wsUrl.IndexOf("?wsdl", StringComparison.OrdinalIgnoreCase));
				}

				WsWsdlUrl = wsWsdlUrl;
			}
			else
			{
				if (!string.IsNullOrEmpty(wsUrl))
				{
					if (wsUrl.ToLower().Contains("?wsdl"))
					{
						WebserviceUrl = wsUrl.Substring(0, wsUrl.IndexOf("?wsdl", StringComparison.OrdinalIgnoreCase));
						WsWsdlUrl = wsUrl;
					}
					else
					{
						WebserviceUrl = wsUrl;
						WsWsdlUrl = wsUrl + "?WSDL";
					}
				}
			}

			if (!string.IsNullOrEmpty(xmlns))
			{
				if (_xmlNamespace.ContainsKey(WebserviceUrl))
				{
					_xmlNamespace[WebserviceUrl] = xmlns;
				}
				else
				{
					_xmlNamespace.Add(WebserviceUrl, xmlns);
				}
			}

			CookieContainer = new CookieContainer();

			WssSvcType = wssSvcType;
			UserName = userName;
			PassWord = passWord;
			WssSoapAction = wssSoapAction;
		}


		#endregion

		#region Interface

		/// <summary>
		/// 通过SOAP协议动态调用webservice
		/// </summary>
		/// <param name="methodName"> 调用方法名</param>
		/// <param name="soapPars"> 参数xml</param>
		/// <returns> 结果集xml</returns>
		public XmlDocument InvokeWebService(string methodName, List<XElement> soapPars)
		{
			string xmlNs = string.Empty;

			if (_xmlNamespace.ContainsKey(WebserviceUrl))
			{
				//名字空间在缓存中存在时，读取缓存，然后执行调用
				xmlNs = _xmlNamespace[WebserviceUrl].ToString();
			}
			else
			{
				//名字空间不存在时直接从wsdl的请求中读取名字空间，然后执行调用
				xmlNs = GetNamespace();
				_xmlNamespace[WebserviceUrl] = xmlNs;
			}

			return QuerySoapWebService(methodName, soapPars, xmlNs);
		}

		/// <summary>
		/// 通过SOAP协议动态调用webservice
		/// </summary>
		/// <param name="methodName"> 调用方法名</param>
		/// <param name="soapPars"> 参数xml</param>
		/// <returns> 结果集xml</returns>
		public string InvokeWebServiceXml(string methodName, List<XElement> soapPars)
		{
			string xmlNs;

			if (_xmlNamespace.ContainsKey(WebserviceUrl))
			{
				//名字空间在缓存中存在时，读取缓存，然后执行调用
				xmlNs = _xmlNamespace[WebserviceUrl].ToString();
			}
			else
			{
				//名字空间不存在时直接从wsdl的请求中读取名字空间，然后执行调用
				xmlNs = GetNamespace();
				_xmlNamespace[WebserviceUrl] = xmlNs;
			}

			return QuerySoapWebServiceXml(methodName, soapPars, xmlNs);
		}

		public List<WebServiceMethod> GetMethodsInfo()
		{
			List<WebServiceMethod> rel = new List<WebServiceMethod>();

			XmlDocument doc = GetWsdlDocument();

			XNamespace n = doc.DocumentElement.NamespaceURI;
			XElement ee = XElement.Parse(doc.OuterXml);
			XElement wsdlPrtType = ee.Element(n + "portType");
			var wsdlMessages = ee.Elements(n + "message");
			XElement wsdlTypes = ee.Element(n + "types");
			if (wsdlPrtType != null && wsdlTypes != null)
			{
				XNamespace schemanc = wsdlTypes.Descendants().FirstOrDefault().Name.NamespaceName;
				XElement simpleTypeSchema = wsdlTypes.Elements(schemanc + "schema").FirstOrDefault(f => f.Descendants(schemanc + "simpleType").Any());
				XElement complexTypeSchema = wsdlTypes.Elements(schemanc + "schema").FirstOrDefault(f => f.Descendants(schemanc + "complexType").Any());

				var wsdlOperations = wsdlPrtType.Elements(n + "operation");
				if (wsdlOperations != null && wsdlOperations.Any())
				{
					foreach (XElement op in wsdlOperations)
					{
						WebServiceMethod method = new WebServiceMethod() { Name = op.Attribute("name").Value };

						//解析方法的输入参数
						XElement input = op.Element(n + "input");
						string inputMessageName = (string)(input.Attribute("message").Value.Split(':')[1]);
						XElement inputMessage = wsdlMessages.FirstOrDefault(f => f.Attribute("name").ToString() == inputMessageName);
						if (inputMessage != null)
						{
							XElement part = inputMessage.Element(n + "part");
							if (part != null)
							{
								string schemaElementName = (string)(part.Attribute("element").Value.Split(':')[1]);
								XElement schemaElement = complexTypeSchema.Elements(schemanc + "element").FirstOrDefault(f => f.Attribute("name").ToString() == schemaElementName);
								if (schemaElement != null)
								{
									XElement complexType = null;
									if (schemaElement.Attribute("type") != null && !string.IsNullOrEmpty((string)(schemaElement.Attribute("type").Value)))
									{
										string proName = (string)(schemaElement.Attribute("type").Value.Split(':')[1]);
										complexType = complexTypeSchema.Elements(schemanc + "complexType").FirstOrDefault(f => f.Attribute("name").ToString() == proName);
									}
									else
									{
										complexType = schemaElement.Element(schemanc + "complexType");
									}
									if (complexType != null && complexType.HasElements)
									{
										XElement[] paramsElement = complexType.Descendants(schemanc + "element").ToArray();
										method.InputParameters = GetWebServiceMethodParameter(paramsElement, schemanc, simpleTypeSchema, complexTypeSchema);
									}
								}
							}
						}

						//解析方法的输出参数
						XElement output = op.Element(n + "output");
						string outputMessageName = (string)(output.Attribute("message").Value.Split(':')[1]);
						XElement outMessage = wsdlMessages.FirstOrDefault(f => f.Attribute("name").ToString() == outputMessageName);
						if (outMessage != null)
						{
							XElement part = outMessage.Element(n + "part");
							if (part != null)
							{
								string schemaElementName = (part.Attribute("element").Value.Split(':')[1]);
								XElement schemaElement = complexTypeSchema.Elements(schemanc + "element").FirstOrDefault(f => f.Attribute("name").ToString() == schemaElementName);
								if (schemaElement != null)
								{
									XElement complexType = schemaElement.Element(schemanc + "complexType");
									if (complexType != null && complexType.HasElements)
									{
										XElement[] paramsElement = complexType.Descendants(schemanc + "element").ToArray();
										if (paramsElement.Length > 1)
										{
											method.IsOutputParaArrayType = true;
										}
										else
										{
											bool isOutputParaArrayType = System.Convert.ToBoolean(paramsElement.Any(w => w.Attribute("type").Value.ToLower().Contains("arrayof")));
											if (isOutputParaArrayType)
											{
												method.IsOutputParaArrayType = true;
											}
											else
											{
												var outParams = GetWebServiceMethodParameter(paramsElement, schemanc, simpleTypeSchema, complexTypeSchema);
												method.OutputParameter = outParams.FirstOrDefault();
											}
										}
									}
								}
							}
						}

						rel.Add(method);
					}
				}
			}

			return rel;
		}

		#endregion

		#region Sub Methods

		/// <summary>
		/// 通过SOAP协议动态调用webservice
		/// </summary>
		/// <param name="methodName"> 调用方法名</param>
		/// <param name="soapPars"> 参数xml</param>
		/// <param name="xmlNs"> 名字空间</param>
		/// <returns> 结果集</returns>
		private XmlDocument QuerySoapWebService(string methodName, List<XElement> soapPars, string xmlNs)
		{
			//XML_NAMESPACE(WebserviceUrl) = xmlNs  '加入缓存，提高效率

			// 获取请求对象
			HttpWebRequest request = (HttpWebRequest)(WebRequest.Create(WebserviceUrl));

			// 设置请求head
			request.Method = "POST";
			request.ContentType = "text/xml; charset=utf-8";

			if (this.WssSvcType == null || this.WssSvcType == 0)
			{
				request.Headers.Add("SOAPAction", "\"" + xmlNs + (((xmlNs.EndsWith("/")) ? "" : "/")) + methodName + "\"");
			}
			else
			{
				if (!string.IsNullOrEmpty(WssSoapAction) && WssSoapAction.Contains("{method}"))
				{
					request.Headers.Add("SOAPAction", this.WssSoapAction.Replace("{method}", methodName));
				}
				else
				{
					request.Headers.Add("SOAPAction", ((!string.IsNullOrEmpty(this.WssSoapAction)) ? this.WssSoapAction : ""));
				}
			}

			// 设置请求身份
			SetWebRequest(request);

			// 获取soap协议
			byte[] data = EncodeParsToSoap(soapPars, xmlNs, methodName);
			// 将soap协议写入请求
			WriteRequestData(request, data);

			XmlDocument returnValueDoc = new XmlDocument();
			// 读取服务端响应
			XmlDocument returnDoc;

			request.CookieContainer = CookieContainer;
			try
			{
				using (var response = request.GetResponse())
				{
					returnDoc = ReadXmlResponse(response);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("请求失败！报文内容:" + Encoding.UTF8.GetString(data), ex);
			}

			XmlNamespaceManager mgr = new XmlNamespaceManager(returnDoc.NameTable);
			mgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
			// 返回结果
			XmlNode node = returnDoc.SelectSingleNode("//soap:Body/*/*", mgr);
			if (node != null)
			{
				//Dim RetXml As String = returnDoc.SelectSingleNode("//soap:Body/*/*", mgr).InnerXml
				string retXml = (string)node.InnerXml;
				returnValueDoc.LoadXml("<root>" + retXml + "</root>");
				AddDelaration(returnValueDoc);
				return returnValueDoc;
			}
			else
			{
				returnValueDoc.LoadXml("<root>" + XElement.Parse(returnDoc.OuterXml).ToString() + "</root>");
				AddDelaration(returnValueDoc);
				return returnValueDoc;
			}

		}

		/// <summary>
		/// 通过SOAP协议动态调用webservice
		/// </summary>
		/// <param name="methodName"> 调用方法名</param>
		/// <param name="soapPars"> 参数xml</param>
		/// <param name="xmlNs"> 名字空间</param>
		/// <returns> 结果集</returns>
		private string QuerySoapWebServiceXml(string methodName, List<XElement> soapPars, string xmlNs)
		{
			//XML_NAMESPACE(WebserviceUrl) = xmlNs  '加入缓存，提高效率

			// 获取请求对象
			HttpWebRequest request = (HttpWebRequest)(WebRequest.Create(WebserviceUrl));
			// 设置请求head
			request.Method = "POST";
			request.ContentType = "text/xml; charset=utf-8";

			if (this.WssSvcType == null || this.WssSvcType == 0)
			{
				request.Headers.Add("SOAPAction", "\"" + xmlNs + (((xmlNs.EndsWith("/")) ? "" : "/")) + methodName + "\"");
			}
			else
			{
				if (!string.IsNullOrEmpty(WssSoapAction) && WssSoapAction.Contains("{method}"))
				{
					request.Headers.Add("SOAPAction", this.WssSoapAction.Replace("{method}", methodName));
				}
				else
				{
					request.Headers.Add("SOAPAction", ((!string.IsNullOrEmpty(this.WssSoapAction)) ? this.WssSoapAction : ""));
				}
			}

			// 设置请求身份
			SetWebRequest(request);

			// 获取soap协议
			byte[] data = EncodeParsToSoap(soapPars, xmlNs, methodName);
			// 将soap协议写入请求
			WriteRequestData(request, data);

			// 读取服务端响应
			XmlDocument returnDoc;

			request.CookieContainer = CookieContainer;
			try
			{
				using (var response = request.GetResponse())
				{
					returnDoc = ReadXmlResponse(response);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("请求失败！报文内容:" + Encoding.UTF8.GetString(data), ex);
			}

			XmlNamespaceManager mgr = new XmlNamespaceManager(returnDoc.NameTable);
			mgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
			// 返回结果
			XmlNode node = returnDoc.SelectSingleNode("//soap:Body/*/*", mgr);
			string retXml = node?.InnerXml ?? XElement.Parse(returnDoc.OuterXml).ToString();

			return "<root>" + retXml + "</root>";
		}

		/// <summary>
		/// 获取wsdl中的名字空间
		/// </summary>
		/// <returns> 名字空间</returns>
		private string GetNamespace()
		{
			XmlDocument doc = GetWsdlDocument();

			var xmlNode = doc.SelectSingleNode("//@targetNamespace");
			if (xmlNode != null)
				return xmlNode.Value;
			return string.Empty;
		}

		/// <summary>
		/// 加入soapheader节点
		/// </summary>
		/// <param name="doc"> soap文档</param>
		private void InitSoapHeader(XmlDocument doc)
		{
			// 添加soapheader节点
			XmlElement soapHeader = doc.CreateElement("soap", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
			//XmlElement soapId = doc.CreateElement("userid");
			//soapId.InnerText = ID;
			//XmlElement soapPwd = doc.CreateElement("userpwd");
			//soapPwd.InnerText = PWD;
			//soapHeader.AppendChild(soapId);
			//soapHeader.AppendChild(soapPwd);
			doc.ChildNodes[0].AppendChild(soapHeader);
		}

		/// <summary>
		/// 将以字节数组的形式返回soap协议
		/// </summary>
		/// <param name="soapPars"> 参数xml</param>
		/// <param name="xmlNs"> 名字空间</param>
		/// <param name="methodName"> 方法名</param>
		/// <returns> 字节数组</returns>
		private byte[] EncodeParsToSoap(List<XElement> soapPars, string xmlNs, string methodName)
		{
			XmlDocument doc = new XmlDocument();
			// 构建soap文档
			doc.LoadXml("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance/\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"></soap:Envelope>");

			// 加入soapbody节点
			InitSoapHeader(doc);

			// 创建soapbody节点
			XmlElement soapBody = doc.CreateElement("soap", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
			// 根据要调用的方法创建一个方法节点
			XmlElement soapMethod = doc.CreateElement("web", methodName, xmlNs);

			if (soapPars != null)
			{
				foreach (XElement item in soapPars)
				{
					// 根据参数表中的键值对，生成一个参数节点，并加入方法节点内
					XmlElement soapPar = (XmlElement)doc.ReadNode(item.CreateReader());
					//带xmlns-前缀的，不添加命名空间
					if (item.Name.ToString().StartsWith("xmlns-"))
					{
						var real = doc.CreateElement(item.Name.ToString().Substring("xmlns-".Length));
						real.InnerXml = soapPar.InnerXml;
						soapPar = real;
					}
					else
					{
						var real = doc.CreateElement("web", item.Name.ToString(), xmlNs);
						real.InnerXml = soapPar.InnerXml;
						soapPar = real;
					}
					soapMethod.AppendChild(soapPar);
				}
			}

			// soapbody节点中加入方法节点
			soapBody.AppendChild(soapMethod);

			// soap文档中加入soapbody节点
			doc.DocumentElement.AppendChild(soapBody);

			// 添加声明
			AddDelaration(doc);

			// 传入的参数有DataSet类型，必须在序列化后的XML中的diffgr:diffgram/NewDataSet节点加xmlns='' 否则无法取到每行的记录。
			XmlNode node = doc.DocumentElement.SelectSingleNode("//NewDataSet");
			if (node != null)
			{
				XmlAttribute attr = doc.CreateAttribute("xmlns");
				attr.InnerText = "";
				node.Attributes.Append(attr);
			}
			//System.IO.File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "input.xml"), doc.OuterXml);
			// 以字节数组的形式返回soap文档
			return Encoding.UTF8.GetBytes(doc.OuterXml);
		}

		/// <summary>
		/// 将参数对象中的内容取出
		/// </summary>
		/// <param name="o">参数值对象</param>
		/// <returns>字符型值对象</returns>
		private string ObjectToSoapXml(object o)
		{
			XmlSerializer mySerializer = new XmlSerializer(o.GetType());
			MemoryStream ms = new MemoryStream();
			mySerializer.Serialize(ms, o);
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(Encoding.UTF8.GetString(ms.ToArray()));
			if (doc.DocumentElement != null)
			{
				return doc.DocumentElement.InnerXml;
			}
			else
			{
				return o.ToString();
			}
		}

		/// <summary>
		/// 设置请求身份
		/// </summary>
		/// <param name="request">请求</param>
		/// <remarks></remarks>
		private void SetWebRequest(HttpWebRequest request)
		{
			if (string.IsNullOrEmpty(UserName))
			{
				request.Credentials = CredentialCache.DefaultCredentials;
			}
			else
			{
				request.Credentials = new NetworkCredential(UserName, PassWord);
			}
		}

		/// <summary>
		/// 将soap协议写入请求
		/// </summary>
		/// <param name="request"> 请求</param>
		/// <param name="data"> soap协议</param>
		private void WriteRequestData(HttpWebRequest request, byte[] data)
		{
			request.ContentLength = data.Length;
			Stream writer = request.GetRequestStream();
			writer.Write(data, 0, data.Length);
			writer.Close();
		}

		/// <summary>
		/// 将响应对象读取为xml对象
		/// </summary>
		/// <param name="response"> 响应对象</param>
		/// <returns> xml对象</returns>
		private static XmlDocument ReadXmlResponse(WebResponse response)
		{
			StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
			string retXml = sr.ReadToEnd();
			sr.Close();
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(retXml);
			return doc;
		}

		/// <summary>
		/// 给xml文档添加声明
		/// </summary>
		/// <param name="doc"> xml文档</param>
		private static void AddDelaration(XmlDocument doc)
		{
			XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
			doc.InsertBefore(decl, doc.DocumentElement);
		}

		private XmlDocument GetWsdlDocument()
		{
			XmlDocument doc = null;

			// 创建wsdl请求对象，并从中读取名字空间
			HttpWebRequest request = (HttpWebRequest)(WebRequest.Create(WsWsdlUrl));
			//设置身份验证
			SetWebRequest(request);
			WebResponse response = request.GetResponse();
			using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
			{
				XmlDocument xmldoc = new XmlDocument();
				string readStr = sr.ReadToEnd();

				xmldoc.LoadXml(readStr);

				doc = xmldoc;
			}

			return doc;
		}

		/// <summary>
		/// 获取Web Service方法中参数的信息
		/// </summary>
		/// <param name="paramsElement">xml中的参数集合（包含简单类型和复杂类型）</param>
		/// <param name="schemanc"></param>
		/// <param name="simpleTypeSchema"></param>
		/// <param name="complexTypeSchema"></param>
		/// <returns>参数信息集合</returns>
		/// <remarks></remarks>
		private WebServiceParameter[] GetWebServiceMethodParameter(XElement[] paramsElement, XNamespace schemanc, XElement simpleTypeSchema, XElement complexTypeSchema)
		{
			List<WebServiceParameter> parameters = new List<WebServiceParameter>();

			foreach (XElement param in paramsElement)
			{
				WebServiceParameter parameter = new WebServiceParameter { Name = param.Attribute("name").Value };

				string elementType = (string)(param.Attribute("type").Value.Split(':')[1]);
				if (simpleTypeSchema != null)
				{
					XElement isSimpleType = simpleTypeSchema.Elements(schemanc + "simpleType").FirstOrDefault(f => f.Attribute("name").ToString() == elementType);
					if (isSimpleType != null)
					{
						//简单类型默认全部转化为字符串类型（跨平台通讯字符串类型较为通用）
						parameter.Type = typeof(string).FullName;
					}
				}

				if (string.IsNullOrEmpty((string)parameter.Type))
				{
					XElement iscComplexType = complexTypeSchema.Elements(schemanc + "complexType").FirstOrDefault(f => f.Attribute("name").ToString() == elementType);
					if (iscComplexType != null)
					{
						parameter.Type = elementType;
						var parmElements = iscComplexType.Descendants(schemanc + "element").ToArray();
						parameter.Fields = GetWebServiceMethodParameter(parmElements, schemanc, simpleTypeSchema, complexTypeSchema);
					}
					else
					{
						parameter.Type = typeof(string).FullName;
					}
				}

				parameters.Add(parameter);
			}

			return parameters.ToArray();
		}

		#endregion

	}

	/// <summary>
	/// Web Service 方法
	/// </summary>
	public class WebServiceMethod
	{
		#region Propertity

		/// <summary>
		/// 方法名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 输入参数数组
		/// </summary>
		public WebServiceParameter[] InputParameters { get; set; }

		/// <summary>
		/// 输出参数
		/// </summary>
		public WebServiceParameter OutputParameter { get; set; }

		/// <summary>
		/// 输出参数是否是数组/集合类型
		/// </summary>
		public bool IsOutputParaArrayType { get; set; }

		#endregion
	}

	/// <summary>
	/// Web Service 参数
	/// </summary>
	public class WebServiceParameter
	{
		#region Propertity

		/// <summary>
		/// 参数名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 参数类型
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// 参数组成
		/// </summary>
		public WebServiceParameter[] Fields { get; set; }

		#endregion
	}

	public class WssNameSpaceCache
	{

		#region Instance

		/// <summary>
		/// 实例
		/// </summary>
		private static WssNameSpaceCache _mInstance;

		/// <summary>
		/// 获取实例
		/// </summary>
		public static WssNameSpaceCache Instance
		{
			get
			{
				if (_mInstance == null)
				{
					lock (typeof(WssNameSpaceCache))
					{
						if (_mInstance == null)
						{
							_mInstance = new WssNameSpaceCache();
						}
					}
				}

				return _mInstance;
			}
		}

		#region Constructor

		private WssNameSpaceCache()
		{
			NsHashtable = new Hashtable();
		}

		#endregion

		public Hashtable NsHashtable { get; set; }

		#endregion

	}
}

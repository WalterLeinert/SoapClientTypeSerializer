using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Web.Services;
using System.Web.Services.Description;
using System.Web.Services.Protocols;
using System.Xml.Serialization;

namespace NTools.WebServiceSupport {

	/// <remarks/>
	[GeneratedCode("wsdl", "2.0.50727.42")]
	[DebuggerStepThrough()]
	[DesignerCategory("code")]
	[WebServiceBinding(Name = "DummyInfoWebService", Namespace = "http://www.themindelectric.com/wsdl/DummyInfoWebService/")]
	public abstract class DummyInfoWebService : SoapHttpClientProtocol {

		/// <remarks/>
		[SoapDocumentMethod("getString", 
			RequestNamespace = "http://www.themindelectric.com/wsdl/DummyInfoWebService/",
		   ResponseNamespace = "http://www.themindelectric.com/wsdl/DummyInfoWebService/", 
			Use = SoapBindingUse.Literal, 
			ParameterStyle = SoapParameterStyle.Wrapped)]
		[return: XmlElement("Result", IsNullable = true)]
		public string getString() {
			var results = this.Invoke("getString", new object[0]);
			return ((string)(results[0]));
		}
	}
}

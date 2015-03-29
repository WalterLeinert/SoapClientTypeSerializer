namespace NTools.WebServiceSupport {

	/// <remarks/>
#if !NET_1
	[System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
#endif
	[System.Diagnostics.DebuggerStepThroughAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Web.Services.WebServiceBindingAttribute(Name = "DummyInfoWebService", Namespace = "http://www.themindelectric.com/wsdl/DummyInfoWebService/")]
	public abstract class DummyInfoWebService : System.Web.Services.Protocols.SoapHttpClientProtocol {

		/// <remarks/>
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("getString", 
			RequestNamespace = "http://www.themindelectric.com/wsdl/DummyInfoWebService/",
		   ResponseNamespace = "http://www.themindelectric.com/wsdl/DummyInfoWebService/", 
			Use = System.Web.Services.Description.SoapBindingUse.Literal, 
			ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		[return: System.Xml.Serialization.XmlElementAttribute("Result", IsNullable = true)]
		public string getString() {
			object[] results = this.Invoke("getString", new object[0]);
			return ((string)(results[0]));
		}
	}
}

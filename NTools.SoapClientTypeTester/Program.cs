using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;
using NTools.Logging.Log4Net;
using NTools.WebServiceSupport;

namespace NTools.SoapClientTypeTester {
    class Program {
      
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main(string[] args) {
            using (var log = new EnterExitLogger(s_log, Level.Info)) {
                try {
                    var assembly = Assembly.LoadFile(args[0]);

                    if (args.Length > 1) {
                        for (var i = 1; i < args.Length; i++) {
                            if (Directory.Exists(args[i])) {
                                var assemblies = Directory.GetFiles(args[i], "*.dll");
                                foreach (var asm in assemblies) {
                                    Assembly.LoadFile(asm);
                                }
                            } else {
                                Assembly.LoadFile(args[i]);
                            }

                        }
                    }
                    SoapClientTypeSerializer.DeserializeClientTypes(assembly);
                } catch (Exception exc) {
                    s_log.Error(exc);
                }
            }
        }
    }
}

namespace SimpleCalc {
    using System;
    using System.IO;
    using log4net;
    using log4net.Config;

    public class Calculator {
        public Calculator() {
            SetupLog();
        }

        protected virtual void SetupLog() {
            try {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(path, "simpleCalc.log4net.config");
                XmlConfigurator.ConfigureAndWatch(new FileInfo(filePath));
            }
            catch {
                //Do not try this at home
            }
        }

        protected ILog GetLog() {
            return LogManager.GetLogger(GetType());
        }

        public int Add(int x, int y) {
            GetLog().DebugFormat("Adding {0} and {1}", x, y);
            return x + y;
        }
        
        public int Subtract(int x, int y) {
            GetLog().DebugFormat("Subtracting {0} and {1}", x, y);
            return x - y;
        }

        public int Multiply(int x, int y) {
            GetLog().DebugFormat("Multiplying {0} and {1}", x, y);
            return x*y;
        }

        public int Divide(int x, int y) {
            GetLog().DebugFormat("Dividing {0} and {1}", x, y);
            return x/y;
        }
    }
}

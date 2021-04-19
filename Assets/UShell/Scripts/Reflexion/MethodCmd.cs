using System;
using System.Reflection;
using System.Threading.Tasks;

namespace UShell
{
    internal struct MethodCmd
    {
        private MethodInfo _method;
        private ParameterInfo[] _parameters;


        public Type ReturnType { get => _method.ReturnType; }
        public Type DeclaringType { get => _method.DeclaringType; }
        public bool IsStatic { get => _method.IsStatic; }
        public int ParametersCount { get => _parameters.Length; }
        public ParameterInfo[] Parameters { get => _parameters; }
        public bool IsAwaitable { get => _method.IsAwaitable(); }

        public string Name
        {
            get
            {
                CmdAttribute attribute = _method.GetCustomAttribute<CmdAttribute>();
                if (attribute != null && !string.IsNullOrEmpty(attribute.Label))
                    return attribute.Label;

                return _method.Name;
            }
        }
        public string Info
        {
            get
            {
                CmdAttribute attribute = _method.GetCustomAttribute<CmdAttribute>();
                if (attribute != null && !string.IsNullOrEmpty(attribute.Info))
                    return attribute.Info;

                return "";
            }
        }


        public MethodCmd(MethodInfo methodInfo)
        {
            _method = methodInfo;
            _parameters = _method.GetParameters();
        }

        public async Task<object> Invoke(object obj, object[] parameters)
        {
            if (IsAwaitable)
            {
                return await (dynamic)_method.Invoke(obj, parameters);
            }
            else
            {
                object result = _method.Invoke(obj, parameters);
                return await Task.FromResult(result);
            }
        }
    }
}

using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UShell
{
    internal struct Method
    {
        #region FIELDS
        private MethodInfo _method;
        private ParameterInfo[] _parameters;
        #endregion

        #region PROPERTIES
        public Type DeclaringType { get => _method.DeclaringType; }
        public bool IsStatic { get => _method.IsStatic; }
        public ParameterInfo[] Parameters { get => _parameters; }
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
        public bool IsDevOnly
        {
            get
            {
                CmdAttribute attribute = _method.GetCustomAttribute<CmdAttribute>();
                if (attribute != null)
                    return attribute.DevOnly;

                return true;
            }
        }

        private int paramCountMax { get => _parameters.Length; }
        private int paramCountMin
        {
            get
            {
                for (int i = 0; i < _parameters.Length; i++)
                {
                    if (_parameters[i].HasDefaultValue)
                        return i;
                }

                return _parameters.Length;
            }
        }
        #endregion

        #region CONSTRUCTORS
        public Method(MethodInfo methodInfo)
        {
            _method = methodInfo;
            _parameters = _method.GetParameters();
        }
        #endregion

        #region METHODS
        public bool CanInvoke(int paramCount)
        {
            if (paramCount < 0)
                throw new ArgumentOutOfRangeException(nameof(paramCount), $"{nameof(paramCount)} < 0");

            if ((_parameters.Length == 0 && paramCount != 0) || paramCount > _parameters.Length)
                return false;

            for (int i = 0; i < _parameters.Length; i++)
            {
                if (i >= paramCount && !_parameters[i].HasDefaultValue)
                    return false;
            }

            return true;
        }
        public async Task<object> Invoke(object obj, object[] args)
        {
            var exception = new ArgumentException(nameof(args), "wrong args count");

            if ((_parameters.Length == 0 && args.Length != 0) || args.Length > _parameters.Length)
                return Task.FromException(exception);

            object[] providedArgs = args;
            args = new object[_parameters.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (i < providedArgs.Length)
                    args[i] = providedArgs[i];
                else if (_parameters[i].HasDefaultValue)
                    args[i] = _parameters[i].DefaultValue;
                else
                    return Task.FromException(exception);
            }

            object result;
            if (_method.IsAwaitable())
                result = await (dynamic)_method.Invoke(obj, args);
            else
                result = await Task.FromResult(_method.Invoke(obj, args));

            for (int i = 0; i < providedArgs.Length; i++)
                providedArgs[i] = args[i];

            return result;
        }

        public static bool AreCompatible(Method a, Method b)
        {
            if (a.paramCountMin < b.paramCountMin && a.paramCountMax < b.paramCountMin)
                return true;

            if (a.paramCountMin > b.paramCountMax && a.paramCountMax > b.paramCountMax)
                return true;

            return false;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder(Name);

            for (int i = 0; i < _parameters.Length; i++)
            {
                result.Append(" ");
                if (_parameters[i].IsOut)
                    result.Append("[out]");
                else if (_parameters[i].ParameterType.IsByRef && !_parameters[i].IsIn)
                    result.Append("[ref]");

                result.Append(_parameters[i].Name);
                result.Append(":");
                if (_parameters[i].ParameterType.IsByRef)
                    result.Append(_parameters[i].ParameterType.GetElementType().Name);
                else
                    result.Append(_parameters[i].ParameterType.Name);

                if (_parameters[i].HasDefaultValue)
                {
                    result.Append("=");
                    result.Append(_parameters[i].DefaultValue ?? "null");
                }
            }

            string info = Info;
            if (!string.IsNullOrEmpty(info))
                result.Append(": " + info);

            return result.ToString();
        }
        #endregion
    }
}

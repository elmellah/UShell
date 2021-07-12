using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace UShell
{
    internal struct Event
    {
        #region FIELDS
        private EventInfo _event;
        private MethodInfo _invoke;
        private ParameterInfo[] _parameters;
        private MethodInfo _addMethod;

        private List<Tuple<object, Delegate>> _targetsAndHandlers;
        #endregion

        #region PROPERTIES
        public Type ReturnType { get => _invoke.ReturnType; }
        public Type DeclaringType { get => _event.DeclaringType; }
        public Type EventHandlerType { get => _event.EventHandlerType; }
        public bool IsStatic { get => _addMethod.IsStatic; }
        public int ParametersCount { get => _parameters.Length; }
        public Type[] ParametersTypes
        {
            get
            {
                Type[] paramTypes = new Type[_parameters.Length];
                for (int j = 0; j < _parameters.Length; j++)
                    paramTypes[j] = _parameters[j].ParameterType;

                return paramTypes;
            }
        }

        public string Name
        {
            get
            {
                EventAttribute attribute = _event.GetCustomAttribute<EventAttribute>();
                if (attribute != null && !string.IsNullOrEmpty(attribute.Label))
                    return attribute.Label;

                return _event.Name;
            }
        }
        public string Info
        {
            get
            {
                EventAttribute attribute = _event.GetCustomAttribute<EventAttribute>();
                if (attribute != null && !string.IsNullOrEmpty(attribute.Info))
                    return attribute.Info;

                return "";
            }
        }
        public bool IsDevOnly
        {
            get
            {
                EventAttribute attribute = _event.GetCustomAttribute<EventAttribute>();
                if (attribute != null)
                    return attribute.DevOnly;

                return true;
            }
        }
        #endregion

        #region CONSTRUCTORS
        public Event(EventInfo eventInfo)
        {
            _event = eventInfo;
            _invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
            _parameters = _invoke.GetParameters();
            _addMethod = eventInfo.GetAddMethod();

            _targetsAndHandlers = new List<Tuple<object, Delegate>>();
        }
        #endregion

        #region METHODS
        public void AddEventHandler(string cmdLine, AssemblyBuilder assemblyBuilder, Dictionary<Type, List<object>> instances)
        {
            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(this.Name + "_" + Environment.TickCount); //Environment.TickCount: the module name must be unique
            MethodBuilder meb = mb.DefineGlobalMethod(this.Name, MethodAttributes.Public | MethodAttributes.Static, this.ReturnType, this.ParametersTypes);
            ILGenerator il = meb.GetILGenerator();

            #region CIL
            MethodInfo processCmdLineMethod = typeof(Shell).GetMethod(nameof(Shell.ProcessCmdLine), new Type[] { typeof(string), typeof(string) });
            MethodInfo mainPropertyMethod = typeof(Shell).GetProperty(nameof(Shell.Main), typeof(Shell)).GetMethod;
            MethodInfo setVariableValueMethod = typeof(Shell).GetMethod(nameof(Shell.SetVariableValue), BindingFlags.Public | BindingFlags.Instance);
            MethodInfo convertToStringMethod = typeof(Utils).GetMethod(nameof(Utils.ConvertToString), BindingFlags.Public | BindingFlags.Static);

            il.EmitCall(OpCodes.Call, mainPropertyMethod, null);
            il.Emit(OpCodes.Ldstr, "0");
            il.Emit(OpCodes.Ldstr, this.Name);
            il.EmitCall(OpCodes.Call, setVariableValueMethod, null);

            for (int i = 0; i < this.ParametersTypes.Length; i++)
            {
                il.EmitCall(OpCodes.Call, mainPropertyMethod, null);
                il.Emit(OpCodes.Ldstr, (i + 1).ToString());
                il.Emit(OpCodes.Ldarg, i);
                if (this.ParametersTypes[i].IsValueType)
                    il.Emit(OpCodes.Box, this.ParametersTypes[i]);
                il.EmitCall(OpCodes.Call, convertToStringMethod, null);
                il.EmitCall(OpCodes.Call, setVariableValueMethod, null);
            }

            il.EmitCall(OpCodes.Call, mainPropertyMethod, null);
            il.Emit(OpCodes.Ldstr, this.Name);
            il.Emit(OpCodes.Ldstr, cmdLine);
            il.EmitCall(OpCodes.Call, processCmdLineMethod, null);
            if (this.ReturnType != typeof(void))
            {
                //There is currently no way of knowing what a command returns, so we return 0 or null
                if (this.ReturnType.IsValueType) il.Emit(OpCodes.Ldc_I4_0);
                else il.Emit(OpCodes.Ldnull);
            }
            il.Emit(OpCodes.Ret);
            #endregion CIL

            mb.CreateGlobalFunctions();
            MethodInfo eventHandler = mb.GetMethod(this.Name);
            Delegate handler = Delegate.CreateDelegate(this.EventHandlerType, eventHandler);
            if (this.IsStatic)
            {
                _event.AddEventHandler(null, handler);
                _targetsAndHandlers.Add(new Tuple<object, Delegate>(null, handler));
            }
            else
            {
                foreach (var target in instances[this.DeclaringType])
                {
                    _event.AddEventHandler(target, handler);
                    _targetsAndHandlers.Add(new Tuple<object, Delegate>(target, handler));
                }
            }
        }
        public void RemoveAllEventHandlers()
        {
            foreach (var e in _targetsAndHandlers)
                _event.RemoveEventHandler(e.Item1, e.Item2);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder(Name);

            string info = Info;
            if (!string.IsNullOrEmpty(info))
                result.Append(": ").Append(info);

            return result.ToString();
        }
        #endregion
    }
}

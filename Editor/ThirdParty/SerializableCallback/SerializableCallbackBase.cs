// NOTE: This vendored third-party file diverges from upstream: LINQ has been replaced with plain
// loops to keep the package LINQ-free. Preserve that when re-syncing with upstream.
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializableCallback
{
    /// <typeparam name="TReturn"></typeparam>
    [Serializable]
    public abstract class SerializableCallbackBase<TReturn> : SerializableCallbackBase
    {
        public InvokableCallbackBase<TReturn> func;

        public override void ClearCache()
        {
            base.ClearCache();
            func = null;
        }

        /// <returns></returns>
        protected InvokableCallbackBase<TReturn> GetPersistentMethod()
        {
            Type[] types = new Type[ArgTypes.Length + 1];
            Array.Copy(ArgTypes, types, ArgTypes.Length);
            types[types.Length - 1] = typeof(TReturn);

            Type genericType = null;
            switch (types.Length)
            {
                case 1:
                    genericType = typeof(InvokableCallback<>).MakeGenericType(types);
                    break;
                case 2:
                    genericType = typeof(InvokableCallback<,>).MakeGenericType(types);
                    break;
                case 3:
                    genericType = typeof(InvokableCallback<, ,>).MakeGenericType(types);
                    break;
                case 4:
                    genericType = typeof(InvokableCallback<, , ,>).MakeGenericType(types);
                    break;
                case 5:
                    genericType = typeof(InvokableCallback<, , , ,>).MakeGenericType(types);
                    break;
                default:
                    throw new ArgumentException(types.Length + "args");
            }
            return Activator.CreateInstance(genericType, target, methodName) as InvokableCallbackBase<TReturn>;
        }
    }

    /// <summary> An inspector-friendly serializable function </summary>
    [Serializable]
    public abstract class SerializableCallbackBase : ISerializationCallbackReceiver
    {
        /// <summary> Target object </summary>
        public Object target { get => _target;
            set { _target = value; ClearCache(); } }
        /// <summary> Target method name </summary>
        public string methodName { get => _methodName;
            set { _methodName = value; ClearCache(); } }
        public object[] Args
        {
            get
            {
                if (args != null) return args;

                args = new object[_args.Length];
                for (int i = 0; i < _args.Length; i++)
                    args[i] = _args[i].GetValue();

                return args;
            }
        }
        public object[] args;
        public Type[] ArgTypes
        {
            get
            {
                if (argTypes != null) return argTypes;

                argTypes = new Type[_args.Length];
                for (int i = 0; i < _args.Length; i++)
                    argTypes[i] = Arg.RealType(_args[i].argType);

                return argTypes;
            }
        }
        public Type[] argTypes;
        public bool dynamic { get => _dynamic;
            set { _dynamic = value; ClearCache(); } }

        [SerializeField] protected Object _target;
        [SerializeField] protected string _methodName;
        [SerializeField] protected Arg[] _args;
        [SerializeField] protected bool _dynamic;
#pragma warning disable 0414
        [SerializeField] private string _typeName;
#pragma warning restore 0414

        [SerializeField] private bool dirty;

#if UNITY_EDITOR
        protected SerializableCallbackBase()
        {
            _typeName = GetType().AssemblyQualifiedName;
        }

#endif

        public virtual void ClearCache()
        {
            argTypes = null;
            args = null;
        }

        /// <param name="target"></param>
        /// <param name="methodName"></param>
        /// <param name="dynamic"></param>
        /// <param name="args"></param>
        public void SetMethod(Object target, string methodName, bool dynamic, params Arg[] args)
        {
            _target = target;
            _methodName = methodName;
            _dynamic = dynamic;
            _args = args;
            ClearCache();
        }

        protected abstract void Cache();

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (dirty) { ClearCache(); dirty = false; }
#endif
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            _typeName = GetType().AssemblyQualifiedName;
#endif
        }
    }

    [Serializable]
    public struct Arg
    {
        public enum ArgType
        {
            Unsupported,
            Bool,
            Int,
            Float,
            String,
            Object
        }
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public Object objectValue;
        public ArgType argType;

        /// <returns></returns>
        public object GetValue()
        {
            return GetValue(argType);
        }

        /// <param name="type"></param>
        /// <returns></returns>
        public object GetValue(ArgType type)
        {
            switch (type)
            {
                case ArgType.Bool:
                    return boolValue;
                case ArgType.Int:
                    return intValue;
                case ArgType.Float:
                    return floatValue;
                case ArgType.String:
                    return stringValue;
                case ArgType.Object:
                    return objectValue;
                default:
                    return null;
            }
        }

        /// <param name="type"></param>
        /// <returns></returns>
        public static Type RealType(ArgType type)
        {
            switch (type)
            {
                case ArgType.Bool:
                    return typeof(bool);
                case ArgType.Int:
                    return typeof(int);
                case ArgType.Float:
                    return typeof(float);
                case ArgType.String:
                    return typeof(string);
                case ArgType.Object:
                    return typeof(Object);
                default:
                    return null;
            }
        }

        /// <param name="type"></param>
        /// <returns></returns>
        public static ArgType FromRealType(Type type)
        {
            if (type == typeof(bool)) return ArgType.Bool;
            if (type == typeof(int)) return ArgType.Int;
            if (type == typeof(float)) return ArgType.Float;
            if (type == typeof(String)) return ArgType.String;
            if (type == typeof(Object)) return ArgType.Object;
            return ArgType.Unsupported;
        }

        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSupported(Type type)
        {
            return FromRealType(type) != ArgType.Unsupported;
        }
    }
}

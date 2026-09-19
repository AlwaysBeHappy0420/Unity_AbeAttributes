using System;
using System.Collections.Generic;
using System.Reflection;

namespace AbeAttributes.Editor
{
    internal sealed class AbeMethodInvoker
    {
        private readonly AbeProperty _property;
        private readonly IReadOnlyList<UnityEngine.Object> _targets;
        private readonly Func<UnityEngine.Object, object>
            _resolveOwnerForTarget;

        private object[] _parameterValues =
            Array.Empty<object>();

        private object[] _lastResults =
            Array.Empty<object>();

        private bool _hasInvoked;

        public IReadOnlyList<object> LastResults =>
            _lastResults;

        public bool HasInvoked =>
            _hasInvoked;

        public AbeMethodInvoker(
            AbeProperty property,
            IReadOnlyList<UnityEngine.Object> targets,
            Func<UnityEngine.Object, object>
                resolveOwnerForTarget)
        {
            _property =
                property;

            _targets =
                targets;

            _resolveOwnerForTarget =
                resolveOwnerForTarget;
        }

        // ================================================================
        // Parameters
        // ================================================================

        public object[] GetParameterValues()
        {
            MethodInfo method =
                _property.Info?.MethodInfo;

            if (method == null)
            {
                return Array.Empty<object>();
            }

            ParameterInfo[] parameters =
                method.GetParameters();

            EnsureParameterValues(
                parameters);

            return _parameterValues;
        }

        public void SetParameterValue(
            int index,
            object value)
        {
            MethodInfo method =
                _property.Info?.MethodInfo;

            if (method == null)
            {
                return;
            }

            ParameterInfo[] parameters =
                method.GetParameters();

            EnsureParameterValues(
                parameters);

            if (index < 0 ||
                index >= _parameterValues.Length)
            {
                return;
            }

            _parameterValues[index] =
                value;

            _hasInvoked =
                false;

            _lastResults =
                Array.Empty<object>();
        }

        private void EnsureParameterValues(
            ParameterInfo[] parameters)
        {
            if (parameters == null ||
                parameters.Length == 0)
            {
                _parameterValues =
                    Array.Empty<object>();

                return;
            }

            if (_parameterValues.Length ==
                parameters.Length)
            {
                return;
            }

            object[] values =
                new object[parameters.Length];

            for (int i = 0;
                 i < parameters.Length;
                 i++)
            {
                values[i] =
                    GetParameterDefaultValue(
                        parameters[i]);
            }

            _parameterValues =
                values;
        }

        private static object GetParameterDefaultValue(
            ParameterInfo parameter)
        {
            if (parameter == null)
            {
                return null;
            }

            if (parameter.HasDefaultValue)
            {
                object defaultValue =
                    parameter.DefaultValue;

                if (defaultValue !=
                    Missing.Value &&
                    defaultValue !=
                    DBNull.Value)
                {
                    return defaultValue;
                }
            }

            Type type =
                parameter.ParameterType;

            if (type.IsByRef)
            {
                type =
                    type.GetElementType();
            }

            if (type == null)
            {
                return null;
            }

            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (type.IsValueType)
            {
                try
                {
                    return Activator.CreateInstance(
                        type);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        // ================================================================
        // Can Invoke
        // ================================================================

        public bool CanInvoke()
        {
            if (_property.Kind !=
                AbePropertyKind.Method)
            {
                return false;
            }

            MethodInfo method =
                _property.Info?.MethodInfo;

            if (method == null)
            {
                return false;
            }

            ParameterInfo[] parameters =
                method.GetParameters();

            for (int i = 0;
                 i < parameters.Length;
                 i++)
            {
                ParameterInfo parameter =
                    parameters[i];

                if (parameter.ParameterType.IsByRef ||
                    parameter.IsOut)
                {
                    return false;
                }

                if (!AbeValueFieldRegistry.CanDraw(
                        parameter.ParameterType))
                {
                    return false;
                }
            }

            return true;
        }

        // ================================================================
        // Invoke
        // ================================================================

        public object[] Invoke()
        {
            return Invoke(
                GetParameterValues());
        }

        public object[] Invoke(
            object[] arguments)
        {
            if (!CanInvoke())
            {
                return Array.Empty<object>();
            }

            MethodInfo method =
                _property.Info?.MethodInfo;

            if (method == null)
            {
                return Array.Empty<object>();
            }

            ParameterInfo[] parameters =
                method.GetParameters();

            if (arguments == null)
            {
                arguments =
                    Array.Empty<object>();
            }

            if (arguments.Length !=
                parameters.Length)
            {
                return Array.Empty<object>();
            }

            _parameterValues =
                new object[arguments.Length];

            for (int i = 0;
                 i < arguments.Length;
                 i++)
            {
                _parameterValues[i] =
                    arguments[i];
            }

            _lastResults =
                Array.Empty<object>();

            _hasInvoked =
                true;

            // ============================================================
            // Static
            // ============================================================

            if (method.IsStatic)
            {
                try
                {
                    object result =
                        method.Invoke(
                            null,
                            arguments);

                    _lastResults =
                        method.ReturnType == typeof(void)
                            ? Array.Empty<object>()
                            : new[] { result };

                    return _lastResults;
                }
                catch (TargetInvocationException exception)
                {
                    UnityEngine.Debug.LogException(
                        exception.InnerException
                        ?? exception);

                    _hasInvoked =
                        false;

                    return Array.Empty<object>();
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(
                        exception);

                    _hasInvoked =
                        false;

                    return Array.Empty<object>();
                }
            }

            // ============================================================
            // Instance
            // ============================================================

            if (_targets == null ||
                _targets.Count == 0)
            {
                _hasInvoked =
                    false;

                return Array.Empty<object>();
            }

            List<object> results =
                new List<object>();

            for (int i = 0;
                 i < _targets.Count;
                 i++)
            {
                UnityEngine.Object target =
                    _targets[i];

                if (target == null)
                {
                    continue;
                }

                object owner =
                    _resolveOwnerForTarget?.Invoke(
                        target);

                if (owner == null)
                {
                    continue;
                }

                try
                {
                    object result =
                        method.Invoke(
                            owner,
                            arguments);

                    if (method.ReturnType !=
                        typeof(void))
                    {
                        results.Add(
                            result);
                    }
                }
                catch (TargetInvocationException exception)
                {
                    UnityEngine.Debug.LogException(
                        exception.InnerException
                        ?? exception);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(
                        exception);
                }
            }

            _lastResults =
                results.ToArray();

            return _lastResults;
        }
    }
}
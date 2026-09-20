using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace AbeAttributes.Editor
{
    public sealed class PopToConsoleILPostProcessor
        : ILPostProcessor
    {
        private const string AttributeName =
            "AbeAttributes.PopToConsoleAttribute";

        private const string RuntimeTypeName =
            "AbeAttributes.PopToConsoleRuntime";

        private const string RuntimeInvokeMethodName =
            "OnInvoke";

        private const string RuntimeGetMethodName =
            "OnGet";

        private const string RuntimeSetMethodName =
            "OnSet";

        private const string RuntimeBreakMethodName =
            "Break";

        private const int ModeInvoke =
            0;

        private const int ModeGet =
            1;

        private const int ModeSet =
            2;

        private const int ModeManual =
            3;

        private const string BreakPointName =
            "BreakPoint";

        // ================================================================
        // ILPostProcessor
        // ================================================================

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(
            ICompiledAssembly compiledAssembly)
        {
            if (compiledAssembly == null)
            {
                return false;
            }

            string name =
                compiledAssembly.Name;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return
                name == "Assembly-CSharp" ||
                name == "Assembly-CSharp-firstpass";
        }

        public override ILPostProcessResult Process(
            ICompiledAssembly compiledAssembly)
        {
            try
            {
                return ProcessInternal(
                    compiledAssembly);
            }
            catch (Exception exception)
            {
                return CreateErrorResult(
                    compiledAssembly,
                    exception);
            }
        }

        // ================================================================
        // Process
        // ================================================================

        private ILPostProcessResult
            ProcessInternal(
                ICompiledAssembly compiledAssembly)
        {
            DefaultAssemblyResolver resolver =
                CreateResolver(
                    compiledAssembly);

            bool hasSymbols =
                compiledAssembly
                    .InMemoryAssembly
                    .PdbData != null
                &&
                compiledAssembly
                    .InMemoryAssembly
                    .PdbData
                    .Length > 0;

            using MemoryStream peInput =
                new MemoryStream(
                    compiledAssembly
                        .InMemoryAssembly
                        .PeData);

            using MemoryStream pdbInput =
                hasSymbols
                    ? new MemoryStream(
                        compiledAssembly
                            .InMemoryAssembly
                            .PdbData)
                    : null;

            ReaderParameters readerParameters =
                new ReaderParameters
                {
                    AssemblyResolver =
                        resolver,

                    ReadingMode =
                        ReadingMode.Immediate,

                    ReadSymbols =
                        hasSymbols
                };

            if (hasSymbols)
            {
                readerParameters.SymbolReaderProvider =
                    new PortablePdbReaderProvider();

                readerParameters.SymbolStream =
                    pdbInput;
            }

            AssemblyDefinition assembly =
                AssemblyDefinition.ReadAssembly(
                    peInput,
                    readerParameters);

            bool modified =
                false;

            foreach (TypeDefinition type
                     in GetAllTypes(
                         assembly.MainModule))
            {
                // --------------------------------------------------------
                // Methods
                // --------------------------------------------------------

                foreach (MethodDefinition method
                         in type.Methods)
                {
                    if (!ShouldProcess(method))
                    {
                        continue;
                    }

                    PopConfiguration configuration =
                        FindConfiguration(
                            method,
                            ModeInvoke);

                    if (configuration == null)
                    {
                        continue;
                    }

                    if (AlreadyInjected(
                            method,
                            RuntimeInvokeMethodName))
                    {
                        continue;
                    }

                    InjectInvoke(
                        assembly,
                        resolver,
                        type,
                        method,
                        configuration);

                    modified = true;
                }

                // --------------------------------------------------------
                // Properties
                // --------------------------------------------------------

                foreach (PropertyDefinition property
                         in type.Properties)
                {
                    // ----------------------------------------------------
                    // Get
                    // ----------------------------------------------------

                    PopConfiguration getConfiguration =
                        FindConfiguration(
                            property,
                            ModeGet);

                    if (getConfiguration != null)
                    {
                        MethodDefinition getter =
                            property.GetMethod;

                        if (getter != null &&
                            ShouldProcess(getter) &&
                            !AlreadyInjected(
                                getter,
                                RuntimeGetMethodName))
                        {
                            InjectGet(
                                assembly,
                                resolver,
                                type,
                                property,
                                getter,
                                getConfiguration);

                            modified = true;
                        }
                    }

                    // ----------------------------------------------------
                    // Set
                    // ----------------------------------------------------

                    PopConfiguration setConfiguration =
                        FindConfiguration(
                            property,
                            ModeSet);

                    if (setConfiguration != null)
                    {
                        MethodDefinition setter =
                            property.SetMethod;

                        if (setter != null &&
                            ShouldProcess(setter) &&
                            !AlreadyInjected(
                                setter,
                                RuntimeSetMethodName))
                        {
                            InjectSet(
                                assembly,
                                resolver,
                                type,
                                property,
                                setter,
                                setConfiguration);

                            modified = true;
                        }
                    }
                }
            }

            if (!modified)
            {
                return new ILPostProcessResult(
                    compiledAssembly.InMemoryAssembly,
                    new List<DiagnosticMessage>());
            }

            return WriteAssembly(
                assembly,
                compiledAssembly);
        }

        // ================================================================
        // Configuration
        // ================================================================

        private sealed class PopConfiguration
        {
            public int Mode;

            public string BreakPoint;
        }

        private static PopConfiguration
            FindConfiguration(
                MethodDefinition method,
                int targetMode)
        {
            if (!method.HasCustomAttributes)
            {
                return null;
            }

            foreach (CustomAttribute attribute
                     in method.CustomAttributes)
            {
                PopConfiguration configuration =
                    ReadConfiguration(
                        attribute);

                if (configuration == null)
                {
                    continue;
                }

                if (configuration.Mode != targetMode)
                {
                    continue;
                }

                return configuration;
            }

            return null;
        }

        private static PopConfiguration
            FindConfiguration(
                PropertyDefinition property,
                int targetMode)
        {
            if (!property.HasCustomAttributes)
            {
                return null;
            }

            foreach (CustomAttribute attribute
                     in property.CustomAttributes)
            {
                PopConfiguration configuration =
                    ReadConfiguration(
                        attribute);

                if (configuration == null)
                {
                    continue;
                }

                if (configuration.Mode != targetMode)
                {
                    continue;
                }

                return configuration;
            }

            return null;
        }

        private static PopConfiguration
            ReadConfiguration(
                CustomAttribute attribute)
        {
            if (attribute == null)
            {
                return null;
            }

            if (attribute.AttributeType.FullName
                != AttributeName)
            {
                return null;
            }

            if (attribute.ConstructorArguments.Count < 1)
            {
                return null;
            }

            CustomAttributeArgument modeArgument =
                attribute.ConstructorArguments[0];

            if (modeArgument.Value == null)
            {
                return null;
            }

            int mode =
                Convert.ToInt32(
                    modeArgument.Value);

            string breakPoint =
                null;

            // ------------------------------------------------------------
            // Constructor argument
            //
            // [PopToConsole(
            //     PopToConsoleMode.Set,
            //     nameof(BreakPoint))]
            // ------------------------------------------------------------

            if (attribute.ConstructorArguments.Count >= 2)
            {
                breakPoint =
                    attribute
                        .ConstructorArguments[1]
                        .Value as string;
            }

            // ------------------------------------------------------------
            // Named property
            //
            // [PopToConsole(
            //     PopToConsoleMode.Set,
            //     BreakPoint = nameof(BreakPoint))]
            // ------------------------------------------------------------

            if (string.IsNullOrEmpty(breakPoint))
            {
                foreach (CustomAttributeNamedArgument property
                         in attribute.Properties)
                {
                    if (property.Name != BreakPointName)
                    {
                        continue;
                    }

                    breakPoint =
                        property.Argument.Value as string;

                    break;

                }
            }

            return new PopConfiguration
            {
                Mode = mode,
                BreakPoint = breakPoint
            };
        }

        // ================================================================
        // Invoke
        // ================================================================

        private static void InjectInvoke(
            AssemblyDefinition assembly,
            DefaultAssemblyResolver resolver,
            TypeDefinition type,
            MethodDefinition method,
            PopConfiguration configuration)
        {
            MethodReference runtimeMethod =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    RuntimeInvokeMethodName,
                    2);

            ILProcessor il =
                method.Body.GetILProcessor();

            Instruction first =
                method.Body.Instructions[0];

            // ------------------------------------------------------------
            // OnInvoke(typeName, methodName)
            // ------------------------------------------------------------

            il.InsertBefore(
                first,
                il.Create(
                    OpCodes.Ldstr,
                    type.FullName));

            il.InsertBefore(
                first,
                il.Create(
                    OpCodes.Ldstr,
                    method.Name));

            il.InsertBefore(
                first,
                il.Create(
                    OpCodes.Call,
                    runtimeMethod));

            // ------------------------------------------------------------
            // BreakPoint()
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(
                    configuration.BreakPoint))
            {
                InjectBreakPointForInvoke(
                    assembly,
                    resolver,
                    type,
                    method,
                    first,
                    configuration.BreakPoint);
            }
        }

        private static void
            InjectBreakPointForInvoke(
                AssemblyDefinition assembly,
                DefaultAssemblyResolver resolver,
                TypeDefinition type,
                MethodDefinition sourceMethod,
                Instruction before,
                string breakPointName)
        {
            MethodDefinition breakPoint =
                FindBreakPointMethod(
                    sourceMethod,
                    type,
                    breakPointName,
                    null);

            MethodReference breakPointReference =
                assembly.MainModule
                    .ImportReference(
                        breakPoint);

            MethodReference runtimeBreak =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    RuntimeBreakMethodName,
                    0);

            ILProcessor il =
                sourceMethod.Body.GetILProcessor();

            Instruction skip =
                il.Create(
                    OpCodes.Nop);

            // ------------------------------------------------------------
            // this
            // ------------------------------------------------------------

            if (!breakPoint.IsStatic)
            {
                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Ldarg_0));
            }

            // ------------------------------------------------------------
            // BreakPoint()
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    breakPointReference));

            // ------------------------------------------------------------
            // false -> skip
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Brfalse,
                    skip));

            // ------------------------------------------------------------
            // true -> Break()
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    runtimeBreak));

            il.InsertBefore(
                before,
                skip);
        }

        // ================================================================
        // Set
        // ================================================================

        private static void InjectSet(
            AssemblyDefinition assembly,
            DefaultAssemblyResolver resolver,
            TypeDefinition type,
            PropertyDefinition property,
            MethodDefinition setter,
            PopConfiguration configuration)
        {
            MethodReference runtimeMethod =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    RuntimeSetMethodName,
                    3);

            ILProcessor il =
                setter.Body.GetILProcessor();

            Instruction first =
                setter.Body.Instructions[0];

            ParameterDefinition valueParameter =
                setter.Parameters[
                    setter.Parameters.Count - 1];

            // ------------------------------------------------------------
            // OnSet(typeName, propertyName, value)
            // ------------------------------------------------------------

            il.InsertBefore(
                first,
                il.Create(
                    OpCodes.Ldstr,
                    type.FullName));

            il.InsertBefore(
                first,
                il.Create(
                    OpCodes.Ldstr,
                    property.Name));

            il.InsertBefore(
                first,
                CreateLoadArgument(
                    il,
                    setter,
                    valueParameter));

            BoxIfNeeded(
                il,
                first,
                valueParameter.ParameterType,
                setter.Module);

            il.InsertBefore(
                first,
                il.Create(
                    OpCodes.Call,
                    runtimeMethod));

            // ------------------------------------------------------------
            // BreakPoint(value)
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(
                    configuration.BreakPoint))
            {
                InjectBreakPointForValue(
                    assembly,
                    resolver,
                    type,
                    setter,
                    first,
                    configuration.BreakPoint,
                    valueParameter.ParameterType);
            }
        }

        // ================================================================
        // Get
        // ================================================================

        private static void InjectGet(
            AssemblyDefinition assembly,
            DefaultAssemblyResolver resolver,
            TypeDefinition type,
            PropertyDefinition property,
            MethodDefinition getter,
            PopConfiguration configuration)
        {
            MethodReference runtimeMethod =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    RuntimeGetMethodName,
                    3);

            List<Instruction> returns =
                getter.Body.Instructions
                    .Where(
                        instruction =>
                            instruction.OpCode ==
                            OpCodes.Ret)
                    .ToList();

            for (int i = 0;
                 i < returns.Count;
                 i++)
            {
                InjectGetAtReturn(
                    assembly,
                    resolver,
                    type,
                    property,
                    getter,
                    returns[i],
                    runtimeMethod,
                    configuration);
            }
        }

        private static void
            InjectGetAtReturn(
                AssemblyDefinition assembly,
                DefaultAssemblyResolver resolver,
                TypeDefinition type,
                PropertyDefinition property,
                MethodDefinition getter,
                Instruction ret,
                MethodReference runtimeMethod,
                PopConfiguration configuration)
        {
            ILProcessor il =
                getter.Body.GetILProcessor();

            // ------------------------------------------------------------
            // Store return value
            // ------------------------------------------------------------

            VariableDefinition valueLocal =
                new VariableDefinition(
                    getter.ReturnType);

            getter.Body.Variables.Add(
                valueLocal);

            il.InsertBefore(
                ret,
                il.Create(
                    OpCodes.Stloc,
                    valueLocal));

            // ------------------------------------------------------------
            // OnGet(typeName, propertyName, value)
            // ------------------------------------------------------------

            il.InsertBefore(
                ret,
                il.Create(
                    OpCodes.Ldstr,
                    type.FullName));

            il.InsertBefore(
                ret,
                il.Create(
                    OpCodes.Ldstr,
                    property.Name));

            il.InsertBefore(
                ret,
                il.Create(
                    OpCodes.Ldloc,
                    valueLocal));

            BoxIfNeeded(
                il,
                ret,
                getter.ReturnType,
                getter.Module);

            il.InsertBefore(
                ret,
                il.Create(
                    OpCodes.Call,
                    runtimeMethod));

            // ------------------------------------------------------------
            // BreakPoint(value)
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(
                    configuration.BreakPoint))
            {
                InjectBreakPointFromLocal(
                    assembly,
                    resolver,
                    type,
                    getter,
                    ret,
                    configuration.BreakPoint,
                    valueLocal);
            }

            // ------------------------------------------------------------
            // Restore return value
            // ------------------------------------------------------------

            il.InsertBefore(
                ret,
                il.Create(
                    OpCodes.Ldloc,
                    valueLocal));
        }

        // ================================================================
        // BreakPoint(value)
        // ================================================================

        private static void
            InjectBreakPointForValue(
                AssemblyDefinition assembly,
                DefaultAssemblyResolver resolver,
                TypeDefinition type,
                MethodDefinition sourceMethod,
                Instruction before,
                string breakPointName,
                TypeReference valueType)
        {
            MethodDefinition breakPoint =
                FindBreakPointMethod(
                    sourceMethod,
                    type,
                    breakPointName,
                    valueType);

            MethodReference breakPointReference =
                assembly.MainModule
                    .ImportReference(
                        breakPoint);

            MethodReference runtimeBreak =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    RuntimeBreakMethodName,
                    0);

            ILProcessor il =
                sourceMethod.Body.GetILProcessor();

            Instruction skip =
                il.Create(
                    OpCodes.Nop);

            ParameterDefinition valueParameter =
                sourceMethod.Parameters[
                    sourceMethod.Parameters.Count - 1];

            // ------------------------------------------------------------
            // this
            // ------------------------------------------------------------

            if (!breakPoint.IsStatic)
            {
                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Ldarg_0));
            }

            // ------------------------------------------------------------
            // value
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                CreateLoadArgument(
                    il,
                    sourceMethod,
                    valueParameter));

            // ------------------------------------------------------------
            // BreakPoint(value)
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    breakPointReference));

            // ------------------------------------------------------------
            // false -> skip
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Brfalse,
                    skip));

            // ------------------------------------------------------------
            // true -> Break()
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    runtimeBreak));

            il.InsertBefore(
                before,
                skip);
        }

        // ================================================================
        // BreakPoint(local)
        // ================================================================

        private static void
            InjectBreakPointFromLocal(
                AssemblyDefinition assembly,
                DefaultAssemblyResolver resolver,
                TypeDefinition type,
                MethodDefinition sourceMethod,
                Instruction before,
                string breakPointName,
                VariableDefinition valueLocal)
        {
            MethodDefinition breakPoint =
                FindBreakPointMethod(
                    sourceMethod,
                    type,
                    breakPointName,
                    valueLocal.VariableType);

            MethodReference breakPointReference =
                assembly.MainModule
                    .ImportReference(
                        breakPoint);

            MethodReference runtimeBreak =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    RuntimeBreakMethodName,
                    0);

            ILProcessor il =
                sourceMethod.Body.GetILProcessor();

            Instruction skip =
                il.Create(
                    OpCodes.Nop);

            // ------------------------------------------------------------
            // this
            // ------------------------------------------------------------

            if (!breakPoint.IsStatic)
            {
                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Ldarg_0));
            }

            // ------------------------------------------------------------
            // value
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldloc,
                    valueLocal));

            // ------------------------------------------------------------
            // BreakPoint(value)
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    breakPointReference));

            // ------------------------------------------------------------
            // false -> skip
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Brfalse,
                    skip));

            // ------------------------------------------------------------
            // true -> Break()
            // ------------------------------------------------------------

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    runtimeBreak));

            il.InsertBefore(
                before,
                skip);
        }

        // ================================================================
        // Find BreakPoint
        // ================================================================

        private static MethodDefinition
            FindBreakPointMethod(
                MethodDefinition sourceMethod,
                TypeDefinition type,
                string methodName,
                TypeReference valueType)
        {
            List<MethodDefinition> candidates =
                new List<MethodDefinition>();

            foreach (MethodDefinition method
                     in type.Methods)
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                candidates.Add(
                    method);
            }

            // ------------------------------------------------------------
            // Method not found
            // ------------------------------------------------------------

            if (candidates.Count == 0)
            {
                string expectedSignature =
                    valueType == null
                        ? $"bool {methodName}()"
                        : $"bool {methodName}(" +
                          $"{GetTypeDisplayName(valueType)})";

                throw new InvalidOperationException(
                    "[PopToConsole] BreakPoint method " +
                    "was not found.\n" +
                    "Type:\n" +
                    $"    {type.FullName}\n" +
                    "Expected:\n" +
                    $"    {expectedSignature}");
            }

            // ------------------------------------------------------------
            // Exact parameter match
            // ------------------------------------------------------------

            MethodDefinition exactMatch =
                null;

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                MethodDefinition candidate =
                    candidates[i];

                if (valueType == null)
                {
                    if (candidate.Parameters.Count != 0)
                    {
                        continue;
                    }

                    exactMatch =
                        candidate;

                    break;
                }

                if (candidate.Parameters.Count != 1)
                {
                    continue;
                }

                ParameterDefinition parameter =
                    candidate.Parameters[0];

                if (parameter.IsOut ||
                    parameter.ParameterType.IsByReference)
                {
                    continue;
                }

                if (!AreSameType(
                        parameter.ParameterType,
                        valueType))
                {
                    continue;
                }

                exactMatch =
                    candidate;

                break;
            }

            // ------------------------------------------------------------
            // Signature mismatch
            // ------------------------------------------------------------

            if (exactMatch == null)
            {
                string expectedSignature =
                    valueType == null
                        ? $"bool {methodName}()"
                        : $"bool {methodName}(" +
                          $"{GetTypeDisplayName(valueType)})";

                string foundSignatures =
                    string.Join(
                        "\n",
                        candidates.Select(
                            FormatMethodSignature));

                throw new InvalidOperationException(
                    "[PopToConsole] Invalid BreakPoint " +
                    "parameter signature.\n" +
                    "Type:\n" +
                    $"    {type.FullName}\n" +
                    "Expected:\n" +
                    $"    {expectedSignature}\n" +
                    "Found:\n" +
                    foundSignatures);
            }

            // ------------------------------------------------------------
            // Return type
            // ------------------------------------------------------------

            if (!AreSameType(
                    exactMatch.ReturnType,
                    type.Module.TypeSystem.Boolean))
            {
                throw new InvalidOperationException(
                    "[PopToConsole] Invalid BreakPoint " +
                    "return type.\n" +
                    "Method:\n" +
                    $"    {type.FullName}." +
                    $"{FormatMethodSignature(exactMatch)}\n" +
                    "Expected:\n" +
                    "    System.Boolean\n" +
                    "Actual:\n" +
                    $"    {GetTypeDisplayName(exactMatch.ReturnType)}");
            }

            // ------------------------------------------------------------
            // Generic method
            // ------------------------------------------------------------

            if (exactMatch.HasGenericParameters)
            {
                throw new InvalidOperationException(
                    "[PopToConsole] BreakPoint method " +
                    "cannot be generic.\n" +
                    "Method:\n" +
                    $"    {type.FullName}." +
                    $"{FormatMethodSignature(exactMatch)}");
            }

            // ------------------------------------------------------------
            // Generic declaring type
            // ------------------------------------------------------------

            if (type.HasGenericParameters)
            {
                throw new InvalidOperationException(
                    "[PopToConsole] BreakPoint on generic " +
                    "types is not supported.\n" +
                    "Type:\n" +
                    $"    {type.FullName}");
            }

            // ------------------------------------------------------------
            // Executable body
            // ------------------------------------------------------------

            if (!exactMatch.HasBody)
            {
                throw new InvalidOperationException(
                    "[PopToConsole] BreakPoint method " +
                    "has no method body.\n" +
                    "Method:\n" +
                    $"    {type.FullName}." +
                    $"{FormatMethodSignature(exactMatch)}");
            }

            if (exactMatch.IsAbstract)
            {
                throw new InvalidOperationException(
                    "[PopToConsole] BreakPoint method " +
                    "cannot be abstract.\n" +
                    "Method:\n" +
                    $"    {type.FullName}." +
                    $"{FormatMethodSignature(exactMatch)}");
            }

            if (exactMatch.IsPInvokeImpl)
            {
                throw new InvalidOperationException(
                    "[PopToConsole] BreakPoint method " +
                    "cannot be a P/Invoke method.\n" +
                    "Method:\n" +
                    $"    {type.FullName}." +
                    $"{FormatMethodSignature(exactMatch)}");
            }

            // ------------------------------------------------------------
            // Static / instance compatibility
            //
            // Instance source:
            //     instance BreakPoint -> valid
            //     static BreakPoint   -> valid
            //
            // Static source:
            //     static BreakPoint   -> valid
            //     instance BreakPoint -> invalid
            // ------------------------------------------------------------

            if (sourceMethod.IsStatic &&
                !exactMatch.IsStatic)
            {
                throw new InvalidOperationException(
                    "[PopToConsole] Invalid BreakPoint " +
                    "context.\n" +
                    "Source method is static:\n" +
                    $"    {type.FullName}.{sourceMethod.Name}\n" +
                    "BreakPoint is instance:\n" +
                    $"    {type.FullName}." +
                    $"{FormatMethodSignature(exactMatch)}\n" +
                    "A static source cannot call an " +
                    "instance BreakPoint.");
            }

            return exactMatch;
        }

        private static bool
            AreSameType(
                TypeReference a,
                TypeReference b)
        {
            if (a == null ||
                b == null)
            {
                return false;
            }

            return
                a.FullName ==
                b.FullName;
        }

        private static string
            FormatMethodSignature(
                MethodDefinition method)
        {
            string parameters =
                string.Join(
                    ", ",
                    method.Parameters.Select(
                        GetParameterDisplayName));

            return
                $"{GetTypeDisplayName(method.ReturnType)} " +
                $"{method.Name}({parameters})";
        }

        private static string
            GetParameterDisplayName(
                ParameterDefinition parameter)
        {
            if (parameter == null)
            {
                return "<null>";
            }

            string modifier =
                parameter.IsOut
                    ? "out "
                    : parameter.ParameterType.IsByReference
                        ? "ref "
                        : string.Empty;

            TypeReference parameterType =
                parameter.ParameterType.IsByReference
                    ? parameter.ParameterType.GetElementType()
                    : parameter.ParameterType;

            return
                modifier +
                GetTypeDisplayName(parameterType);
        }

        private static string
            GetTypeDisplayName(
                TypeReference type)
        {
            if (type == null)
            {
                return "<null>";
            }

            return type.FullName;
        }

        // ================================================================
        // Runtime Method
        // ================================================================

        private static MethodReference
            FindRuntimeMethod(
                AssemblyDefinition assembly,
                DefaultAssemblyResolver resolver,
                string methodName,
                int parameterCount)
        {
            MethodDefinition localMethod =
                FindRuntimeMethod(
                    assembly.MainModule,
                    methodName,
                    parameterCount);

            if (localMethod != null)
            {
                return localMethod;
            }

            foreach (AssemblyNameReference reference
                     in assembly.MainModule
                         .AssemblyReferences)
            {
                AssemblyDefinition referencedAssembly;

                try
                {
                    referencedAssembly =
                        resolver.Resolve(
                            reference);
                }
                catch
                {
                    continue;
                }

                if (referencedAssembly == null)
                {
                    continue;
                }

                MethodDefinition referencedMethod =
                    FindRuntimeMethod(
                        referencedAssembly.MainModule,
                        methodName,
                        parameterCount);

                if (referencedMethod == null)
                {
                    continue;
                }

                return assembly.MainModule
                    .ImportReference(
                        referencedMethod);
            }

            throw new InvalidOperationException(
                "Cannot find " +
                $"{RuntimeTypeName}.{methodName}.");
        }

        private static MethodDefinition
            FindRuntimeMethod(
                ModuleDefinition module,
                string methodName,
                int parameterCount)
        {
            foreach (TypeDefinition type
                     in GetAllTypes(module))
            {
                if (type.FullName
                    != RuntimeTypeName)
                {
                    continue;
                }

                foreach (MethodDefinition method
                         in type.Methods)
                {
                    if (method.Name != methodName)
                    {
                        continue;
                    }

                    if (!method.IsStatic)
                    {
                        continue;
                    }

                    if (method.ReturnType.FullName
                        != module
                            .TypeSystem
                            .Void
                            .FullName)
                    {
                        continue;
                    }

                    if (method.Parameters.Count
                        != parameterCount)
                    {
                        continue;
                    }

                    return method;
                }
            }

            return null;
        }

        // ================================================================
        // Already Injected
        // ================================================================

        private static bool AlreadyInjected(
            MethodDefinition method,
            string runtimeMethodName)
        {
            if (!method.HasBody)
            {
                return false;
            }

            foreach (Instruction instruction
                     in method.Body.Instructions)
            {
                if (instruction.OpCode
                    != OpCodes.Call)
                {
                    continue;
                }

                if (!(instruction.Operand
                      is MethodReference reference))
                {
                    continue;
                }

                if (reference.Name
                    != runtimeMethodName)
                {
                    continue;
                }

                if (reference.DeclaringType
                    .FullName != RuntimeTypeName)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        // ================================================================
        // IL Helpers
        // ================================================================

        private static Instruction
            CreateLoadArgument(
                ILProcessor il,
                MethodDefinition method,
                ParameterDefinition parameter)
        {
            int index =
                parameter.Index;

            if (!method.IsStatic)
            {
                index++;
            }

            switch (index)
            {
                case 0:
                    return il.Create(
                        OpCodes.Ldarg_0);

                case 1:
                    return il.Create(
                        OpCodes.Ldarg_1);

                case 2:
                    return il.Create(
                        OpCodes.Ldarg_2);

                case 3:
                    return il.Create(
                        OpCodes.Ldarg_3);

                default:
                    return il.Create(
                        OpCodes.Ldarg,
                        parameter);
            }
        }

        private static void BoxIfNeeded(
            ILProcessor il,
            Instruction before,
            TypeReference type,
            ModuleDefinition module)
        {
            if (type.IsByReference)
            {
                type =
                    type.GetElementType();
            }

            if (!type.IsValueType)
            {
                return;
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Box,
                    module.ImportReference(type)));
        }

        // ================================================================
        // Should Process
        // ================================================================

        private static bool
            ShouldProcess(
                MethodDefinition method)
        {
            if (method == null)
            {
                return false;
            }

            if (!method.HasBody)
            {
                return false;
            }

            if (method.IsAbstract)
            {
                return false;
            }

            if (method.IsPInvokeImpl)
            {
                return false;
            }

            if (method.IsConstructor)
            {
                return false;
            }

            return true;
        }

        // ================================================================
        // Types
        // ================================================================

        private static IEnumerable<TypeDefinition>
            GetAllTypes(
                ModuleDefinition module)
        {
            foreach (TypeDefinition type
                     in module.Types)
            {
                foreach (TypeDefinition result
                         in GetAllTypes(type))
                {
                    yield return result;
                }
            }
        }

        private static IEnumerable<TypeDefinition>
            GetAllTypes(
                TypeDefinition type)
        {
            yield return type;

            foreach (TypeDefinition nested
                     in type.NestedTypes)
            {
                foreach (TypeDefinition result
                         in GetAllTypes(nested))
                {
                    yield return result;
                }
            }
        }

        // ================================================================
        // Resolver
        // ================================================================

        private static DefaultAssemblyResolver
            CreateResolver(
                ICompiledAssembly compiledAssembly)
        {
            DefaultAssemblyResolver resolver =
                new DefaultAssemblyResolver();

            foreach (string reference
                     in compiledAssembly.References)
            {
                if (string.IsNullOrEmpty(reference))
                {
                    continue;
                }

                string directory =
                    Path.GetDirectoryName(
                        reference);

                if (string.IsNullOrEmpty(directory))
                {
                    continue;
                }

                if (!resolver
                    .GetSearchDirectories()
                    .Contains(directory))
                {
                    resolver.AddSearchDirectory(
                        directory);
                }
            }

            return resolver;
        }

        // ================================================================
        // Write
        // ================================================================

        private static ILPostProcessResult
            WriteAssembly(
                AssemblyDefinition assembly,
                ICompiledAssembly compiledAssembly)
        {
            using MemoryStream peOutput =
                new MemoryStream();

            using MemoryStream pdbOutput =
                new MemoryStream();

            bool writeSymbols =
                compiledAssembly
                    .InMemoryAssembly
                    .PdbData != null
                &&
                compiledAssembly
                    .InMemoryAssembly
                    .PdbData
                    .Length > 0;

            WriterParameters writerParameters =
                new WriterParameters
                {
                    WriteSymbols =
                        writeSymbols
                };

            if (writeSymbols)
            {
                writerParameters.SymbolWriterProvider =
                    new PortablePdbWriterProvider();

                writerParameters.SymbolStream =
                    pdbOutput;
            }

            assembly.Write(
                peOutput,
                writerParameters);

            return new ILPostProcessResult(
                new InMemoryAssembly(
                    peOutput.ToArray(),
                    writeSymbols
                        ? pdbOutput.ToArray()
                        : compiledAssembly
                            .InMemoryAssembly
                            .PdbData),
                new List<DiagnosticMessage>());
        }

        // ================================================================
        // Error
        // ================================================================

        private static ILPostProcessResult
    CreateErrorResult(
        ICompiledAssembly compiledAssembly,
        Exception exception)
        {
            DiagnosticMessage diagnostic =
                new DiagnosticMessage
                {
                    DiagnosticType =
                        DiagnosticType.Error,

                    MessageData =
    "\n[PopToConsole]\n" +
    "================ BREAKPOINT ERROR ================\n" +
    exception.Message +
    "\n===================================================="
                };

            return new ILPostProcessResult(
                compiledAssembly.InMemoryAssembly,
                new List<DiagnosticMessage>
                {
            diagnostic
                });
        }
    }
}
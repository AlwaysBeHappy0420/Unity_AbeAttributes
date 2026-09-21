using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace AbeAttributes.Editor
{
    public sealed class PopToConsoleILPostProcessor : ILPostProcessor
    {
        public override ILPostProcessor GetInstance()
        {
            return this;
        }

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

        private const string BreakPointName =
            "BreakPoint";

        private const string ValuePathName =
            "ValuePath";

        private const string CustomDebugMethodName =
            "CustomDebugMethod";

        private const int InvokeMode = 0;
        private const int GetMode = 1;
        private const int SetMode = 2;
        private const int ManualMode = 3;

        private sealed class PopConfiguration
        {
            public int Mode;
            public string BreakPoint;
            public string ValuePath;
            public string CustomDebugMethod;
        }

        private sealed class BreakPointMember
        {
            public FieldDefinition Field;
            public PropertyDefinition Property;

            public bool IsStatic
            {
                get
                {
                    if (Field != null)
                    {
                        return Field.IsStatic;
                    }

                    if (Property != null &&
                        Property.GetMethod != null)
                    {
                        return Property.GetMethod.IsStatic;
                    }

                    return false;
                }
            }
        }

        public override bool WillProcess(
            ICompiledAssembly compiledAssembly)
        {
            return true;
        }

        public override ILPostProcessResult Process(
            ICompiledAssembly compiledAssembly)
        {
            var diagnostics =
                new List<DiagnosticMessage>();

            if (compiledAssembly == null ||
                compiledAssembly.InMemoryAssembly == null)
            {
                return null;
            }

            try
            {
                DefaultAssemblyResolver resolver =
                    CreateResolver(compiledAssembly);

                byte[] peData =
                    compiledAssembly.InMemoryAssembly.PeData;

                byte[] pdbData =
                    compiledAssembly.InMemoryAssembly.PdbData;

                bool hasSymbols =
                    pdbData != null &&
                    pdbData.Length > 0;

                using var peStream =
                    new MemoryStream(peData);

                using var pdbStream =
                    hasSymbols
                        ? new MemoryStream(pdbData)
                        : null;

                var readerParameters =
                    new ReaderParameters
                    {
                        AssemblyResolver = resolver,
                        ReadSymbols = hasSymbols,
                        SymbolStream = pdbStream,
                        SymbolReaderProvider =
                            hasSymbols
                                ? new PortablePdbReaderProvider()
                                : null
                    };

                AssemblyDefinition assembly =
                    AssemblyDefinition.ReadAssembly(
                        peStream,
                        readerParameters);

                int changes =
                    ProcessAssembly(
                        assembly,
                        resolver);

                // Instrumentation can insert a substantial amount of IL between
                // existing branch instructions and their targets. Short branch
                // opcodes (br.s, brtrue.s, leave.s, etc.) only have an 8-bit
                // displacement and can become invalid after instrumentation.
                // Expand every short branch to its long form before Cecil writes
                // the method body so the branch target remains valid.
                if (changes > 0)
                {
                    ExpandShortBranches(assembly.MainModule);
                }

                if (changes == 0)
                {
                    return new ILPostProcessResult(
                        new InMemoryAssembly(
                            peData,
                            pdbData),
                        diagnostics);
                }

                using var outputPe =
                    new MemoryStream();

                using var outputPdb =
                    hasSymbols
                        ? new MemoryStream()
                        : null;

                var writerParameters =
                    new WriterParameters
                    {
                        WriteSymbols = hasSymbols,
                        SymbolStream = outputPdb,
                        SymbolWriterProvider =
                            hasSymbols
                                ? new PortablePdbWriterProvider()
                                : null
                    };

                assembly.Write(
                    outputPe,
                    writerParameters);

                byte[] outputPdbData =
                    hasSymbols
                        ? outputPdb.ToArray()
                        : null;

                return new ILPostProcessResult(
                    new InMemoryAssembly(
                        outputPe.ToArray(),
                        outputPdbData),
                    diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Add(
                    new DiagnosticMessage
                    {
                        DiagnosticType =
                            DiagnosticType.Error,
                        MessageData =
                            "PopToConsole IL post-processing failed:\n" +
                            exception
                    });

                return new ILPostProcessResult(
                    null,
                    diagnostics);
            }
        }

        private static DefaultAssemblyResolver CreateResolver(
            ICompiledAssembly compiledAssembly)
        {
            var resolver =
                new DefaultAssemblyResolver();

            foreach (string reference
                     in compiledAssembly.References)
            {
                if (string.IsNullOrEmpty(reference))
                {
                    continue;
                }

                string directory =
                    Path.GetDirectoryName(reference);

                if (string.IsNullOrEmpty(directory))
                {
                    continue;
                }

                resolver.AddSearchDirectory(directory);
            }

            return resolver;
        }

        private static void ExpandShortBranches(
            ModuleDefinition module)
        {
            if (module == null)
            {
                return;
            }

            foreach (TypeDefinition type in module.Types)
            {
                ExpandShortBranchesRecursive(type);
            }
        }

        private static void ExpandShortBranchesRecursive(
            TypeDefinition type)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                foreach (Instruction instruction in method.Body.Instructions)
                {
                    instruction.OpCode =
                        ExpandShortBranchOpCode(
                            instruction.OpCode);
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                ExpandShortBranchesRecursive(nested);
            }
        }

        private static OpCode ExpandShortBranchOpCode(
            OpCode opcode)
        {
            if (opcode == OpCodes.Br_S)
                return OpCodes.Br;

            if (opcode == OpCodes.Brfalse_S)
                return OpCodes.Brfalse;

            if (opcode == OpCodes.Brtrue_S)
                return OpCodes.Brtrue;

            if (opcode == OpCodes.Beq_S)
                return OpCodes.Beq;

            if (opcode == OpCodes.Bge_S)
                return OpCodes.Bge;

            if (opcode == OpCodes.Bge_Un_S)
                return OpCodes.Bge_Un;

            if (opcode == OpCodes.Bgt_S)
                return OpCodes.Bgt;

            if (opcode == OpCodes.Bgt_Un_S)
                return OpCodes.Bgt_Un;

            if (opcode == OpCodes.Ble_S)
                return OpCodes.Ble;

            if (opcode == OpCodes.Ble_Un_S)
                return OpCodes.Ble_Un;

            if (opcode == OpCodes.Blt_S)
                return OpCodes.Blt;

            if (opcode == OpCodes.Blt_Un_S)
                return OpCodes.Blt_Un;

            if (opcode == OpCodes.Bne_Un_S)
                return OpCodes.Bne_Un;

            if (opcode == OpCodes.Leave_S)
                return OpCodes.Leave;

            return opcode;
        }

        private static int ProcessAssembly(
            AssemblyDefinition assembly,
            IAssemblyResolver resolver)
        {
            if (assembly == null ||
                assembly.MainModule == null)
            {
                return 0;
            }

            TypeDefinition runtimeType =
                FindType(
                    assembly,
                    resolver,
                    RuntimeTypeName);

            if (runtimeType == null)
            {
                // This assembly does not reference AbeAttributes runtime code.
                return 0;
            }

            MethodReference runtimeInvoke =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    runtimeType,
                    RuntimeInvokeMethodName,
                    2);

            MethodReference runtimeGet3 =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    runtimeType,
                    RuntimeGetMethodName,
                    3);

            MethodReference runtimeGet4 =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    runtimeType,
                    RuntimeGetMethodName,
                    4);

            MethodReference runtimeSet3 =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    runtimeType,
                    RuntimeSetMethodName,
                    3);

            MethodReference runtimeSet4 =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    runtimeType,
                    RuntimeSetMethodName,
                    4);

            MethodReference runtimeBreak =
                FindRuntimeMethod(
                    assembly,
                    resolver,
                    runtimeType,
                    RuntimeBreakMethodName,
                    0);

            if (runtimeInvoke == null ||
                runtimeGet3 == null ||
                runtimeGet4 == null ||
                runtimeSet3 == null ||
                runtimeSet4 == null ||
                runtimeBreak == null)
            {
                throw new InvalidOperationException(
                    "PopToConsoleRuntime methods could not be resolved.");
            }

            int changes = 0;

            foreach (TypeDefinition type
                     in assembly.MainModule.Types.ToList())
            {
                changes +=
                    ProcessType(
                        assembly.MainModule,
                        resolver,
                        type,
                        runtimeInvoke,
                        runtimeGet3,
                        runtimeGet4,
                        runtimeSet3,
                        runtimeSet4,
                        runtimeBreak);
            }

            return changes;
        }

        private static int ProcessType(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            TypeDefinition type,
            MethodReference runtimeInvoke,
            MethodReference runtimeGet3,
            MethodReference runtimeGet4,
            MethodReference runtimeSet3,
            MethodReference runtimeSet4,
            MethodReference runtimeBreak)
        {
            if (type == null)
            {
                return 0;
            }

            int changes = 0;

            foreach (MethodDefinition method
                     in type.Methods.ToList())
            {
                changes +=
                    ProcessMethod(
                        module,
                        resolver,
                        method,
                        runtimeInvoke,
                        runtimeBreak);
            }

            foreach (PropertyDefinition property
                     in type.Properties.ToList())
            {
                changes +=
                    ProcessProperty(
                        module,
                        resolver,
                        property,
                        runtimeGet3,
                        runtimeGet4,
                        runtimeSet3,
                        runtimeSet4,
                        runtimeBreak);
            }

            foreach (FieldDefinition field
                     in type.Fields.ToList())
            {
                changes +=
                    ProcessField(
                        module,
                        resolver,
                        field,
                        runtimeGet3,
                        runtimeGet4,
                        runtimeSet3,
                        runtimeSet4,
                        runtimeBreak);
            }

            foreach (TypeDefinition nestedType
                     in type.NestedTypes.ToList())
            {
                changes +=
                    ProcessType(
                        module,
                        resolver,
                        nestedType,
                        runtimeInvoke,
                        runtimeGet3,
                        runtimeGet4,
                        runtimeSet3,
                        runtimeSet4,
                        runtimeBreak);
            }

            return changes;
        }

        private static int ProcessMethod(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            MethodDefinition method,
            MethodReference runtimeInvoke,
            MethodReference runtimeBreak)
        {
            if (method == null ||
                method.IsConstructor ||
                !method.HasBody)
            {
                return 0;
            }

            PopConfiguration configuration =
                GetConfiguration(
                    method.CustomAttributes,
                    InvokeMode);

            if (configuration == null)
            {
                return 0;
            }

            if (method.IsAbstract ||
                method.IsPInvokeImpl)
            {
                return 0;
            }

            ILProcessor il =
                method.Body.GetILProcessor();

            Instruction first =
                method.Body.Instructions.FirstOrDefault();

            if (first == null)
            {
                return 0;
            }

            MethodReference debugMethod =
                ResolveDebugMethod(
                    module,
                    method.DeclaringType,
                    method.DeclaringType,
                    configuration,
                    2,
                    method.IsStatic,
                    runtimeInvoke);

            EmitInvokeDebugCall(
                il,
                first,
                method.DeclaringType.FullName,
                method.Name,
                debugMethod);

            if (!string.IsNullOrEmpty(
                    configuration.BreakPoint) &&
                !method.IsStatic)
            {
                EmitBreakPointFromArgument(
                    module,
                    resolver,
                    method,
                    il,
                    new ArgumentSource(0),
                    method.DeclaringType,
                    configuration.BreakPoint,
                    runtimeBreak,
                    first);
            }

            return 1;
        }

        private static int ProcessProperty(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            PropertyDefinition property,
            MethodReference runtimeGet3,
            MethodReference runtimeGet4,
            MethodReference runtimeSet3,
            MethodReference runtimeSet4,
            MethodReference runtimeBreak)
        {
            if (property == null)
            {
                return 0;
            }

            int changes = 0;

            PopConfiguration getConfiguration =
                GetConfiguration(
                    property.CustomAttributes,
                    GetMode);

            if (getConfiguration != null &&
                property.GetMethod != null)
            {
                changes +=
                    InstrumentPropertyGetter(
                        module,
                        resolver,
                        property,
                        property.GetMethod,
                        getConfiguration,
                        runtimeGet3,
                        runtimeGet4,
                        runtimeBreak);
            }

            PopConfiguration setConfiguration =
                GetConfiguration(
                    property.CustomAttributes,
                    SetMode);

            if (setConfiguration != null &&
                property.SetMethod != null)
            {
                changes +=
                    InstrumentPropertySetter(
                        module,
                        resolver,
                        property,
                        property.SetMethod,
                        setConfiguration,
                        runtimeSet3,
                        runtimeSet4,
                        runtimeBreak);
            }

            return changes;
        }

        private static int InstrumentPropertyGetter(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            PropertyDefinition property,
            MethodDefinition getter,
            PopConfiguration configuration,
            MethodReference runtimeGet3,
            MethodReference runtimeGet4,
            MethodReference runtimeBreak)
        {
            if (getter.IsAbstract ||
                getter.IsPInvokeImpl ||
                !getter.HasBody ||
                getter.ReturnType.IsByReference)
            {
                return 0;
            }

            MethodBody body = getter.Body;
            ILProcessor il = body.GetILProcessor();

            VariableDefinition valueLocal =
                new VariableDefinition(
                    getter.ReturnType);

            body.Variables.Add(valueLocal);
            body.InitLocals = true;

            MethodReference runtimeMethod =
                ResolveDebugMethod(
                    module,
                    property.DeclaringType,
                    getter.DeclaringType,
                    configuration,
                    GetValueRuntimeParameterCount(
                        configuration.ValuePath),
                    getter.IsStatic,
                    FindRuntimeMethodByConfiguration(
                        configuration.ValuePath,
                        runtimeGet3,
                        runtimeGet4));

            List<Instruction> returns =
                body.Instructions
                    .Where(x => x.OpCode == OpCodes.Ret)
                    .ToList();

            foreach (Instruction ret in returns)
            {
                il.InsertBefore(
                    ret,
                    il.Create(
                        OpCodes.Stloc,
                        valueLocal));

                EmitGetOrSetCall(
                    module,
                    il,
                    ret,
                    property.DeclaringType.FullName,
                    property.Name,
                    valueLocal,
                    getter.ReturnType,
                    configuration.ValuePath,
                    runtimeMethod);

                if (!string.IsNullOrEmpty(
                        configuration.BreakPoint))
                {
                    EmitBreakPointFromLocal(
                        module,
                        resolver,
                        getter,
                        il,
                        ret,
                        valueLocal,
                        getter.ReturnType,
                        configuration.BreakPoint,
                        runtimeBreak);
                }

                il.InsertBefore(
                    ret,
                    il.Create(
                        OpCodes.Ldloc,
                        valueLocal));
            }

            return returns.Count > 0 ? 1 : 0;
        }

        private static int InstrumentPropertySetter(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            PropertyDefinition property,
            MethodDefinition setter,
            PopConfiguration configuration,
            MethodReference runtimeSet3,
            MethodReference runtimeSet4,
            MethodReference runtimeBreak)
        {
            if (setter.IsAbstract ||
                setter.IsPInvokeImpl ||
                !setter.HasBody ||
                setter.Parameters.Count == 0 ||
                setter.Parameters.Last().ParameterType.IsByReference)
            {
                return 0;
            }

            ILProcessor il =
                setter.Body.GetILProcessor();

            Instruction first =
                setter.Body.Instructions.FirstOrDefault();

            if (first == null)
            {
                return 0;
            }

            ParameterDefinition valueParameter =
                setter.Parameters.Last();

            MethodReference runtimeMethod =
                ResolveDebugMethod(
                    module,
                    property.DeclaringType,
                    setter.DeclaringType,
                    configuration,
                    GetValueRuntimeParameterCount(
                        configuration.ValuePath),
                    setter.IsStatic,
                    FindRuntimeMethodByConfiguration(
                        configuration.ValuePath,
                        runtimeSet3,
                        runtimeSet4));

            EmitGetOrSetCallFromArgument(
                module,
                il,
                first,
                property.DeclaringType.FullName,
                property.Name,
                valueParameter,
                property.PropertyType,
                configuration.ValuePath,
                runtimeMethod);

            if (!string.IsNullOrEmpty(
                    configuration.BreakPoint))
            {
                EmitBreakPointFromArgument(
                    module,
                    resolver,
                    setter,
                    il,
                    new ArgumentSource(
                        valueParameter),
                    property.PropertyType,
                    configuration.BreakPoint,
                    runtimeBreak,
                    first);
            }

            return 1;
        }

        private static int ProcessField(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            FieldDefinition field,
            MethodReference runtimeGet3,
            MethodReference runtimeGet4,
            MethodReference runtimeSet3,
            MethodReference runtimeSet4,
            MethodReference runtimeBreak)
        {
            IList<MethodDefinition> methods =
                GetAllMethods(module);

            int changes = 0;

            foreach (MethodDefinition method
                     in methods)
            {
                if (method == null ||
                    !method.HasBody ||
                    method.IsAbstract ||
                    method.IsPInvokeImpl)
                {
                    continue;
                }

                if (method.DeclaringType == null)
                {
                    continue;
                }

                changes +=
                    InstrumentFieldUsages(
                        module,
                        resolver,
                        method,
                        field,
                        runtimeGet3,
                        runtimeGet4,
                        runtimeSet3,
                        runtimeSet4,
                        runtimeBreak);
            }

            // Field processing is based on actual IL field access. Processing each
            // field from each declaring type is intentionally simple and preserves
            // support for accesses emitted from other types in the same assembly.
            return changes;
        }

        private static IList<MethodDefinition> GetAllMethods(
            ModuleDefinition module)
        {
            var result =
                new List<MethodDefinition>();

            foreach (TypeDefinition type
                     in module.Types)
            {
                CollectMethodsRecursive(
                    type,
                    result);
            }

            return result;
        }

        private static void CollectMethodsRecursive(
            TypeDefinition type,
            List<MethodDefinition> result)
        {
            result.AddRange(type.Methods);

            foreach (TypeDefinition nested
                     in type.NestedTypes)
            {
                CollectMethodsRecursive(
                    nested,
                    result);
            }
        }

        private static int InstrumentFieldUsages(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            MethodDefinition method,
            FieldDefinition targetField,
            MethodReference runtimeGet3,
            MethodReference runtimeGet4,
            MethodReference runtimeSet3,
            MethodReference runtimeSet4,
            MethodReference runtimeBreak)
        {
            List<Instruction> instructions =
                method.Body.Instructions.ToList();

            int changes = 0;

            foreach (Instruction instruction in instructions)
            {
                if (instruction.OpCode == OpCodes.Ldfld ||
                    instruction.OpCode == OpCodes.Ldsfld)
                {
                    FieldReference fieldReference =
                        instruction.Operand as FieldReference;

                    if (!IsTargetField(
                            fieldReference,
                            targetField))
                    {
                        continue;
                    }

                    PopConfiguration configuration =
                        GetConfiguration(
                            targetField.CustomAttributes,
                            GetMode);

                    if (configuration == null)
                    {
                        continue;
                    }

                    if (targetField.FieldType.IsByReference)
                    {
                        continue;
                    }

                    VariableDefinition valueLocal =
                        new VariableDefinition(
                            targetField.FieldType);

                    method.Body.Variables.Add(valueLocal);
                    method.Body.InitLocals = true;

                    ILProcessor il =
                        method.Body.GetILProcessor();

                    // The value produced by ldfld/ldsfld must be captured first.
                    // Keep the anchor on the newly inserted stloc so every injected
                    // instruction is emitted immediately after the captured value.
                    Instruction valueStore =
                        il.Create(
                            OpCodes.Stloc,
                            valueLocal);

                    il.InsertAfter(
                        instruction,
                        valueStore);

                    Instruction anchor =
                        valueStore;

                    MethodReference runtimeMethod =
                        ResolveDebugMethod(
                            module,
                            targetField.DeclaringType,
                            method.DeclaringType,
                            configuration,
                            GetValueRuntimeParameterCount(
                                configuration.ValuePath),
                            method.IsStatic,
                            FindRuntimeMethodByConfiguration(
                                configuration.ValuePath,
                                runtimeGet3,
                                runtimeGet4));

                    EmitGetOrSetCallAfterAnchor(
                        module,
                        il,
                        ref anchor,
                        targetField.DeclaringType.FullName,
                        targetField.Name,
                        valueLocal,
                        targetField.FieldType,
                        configuration.ValuePath,
                        runtimeMethod);

                    if (!string.IsNullOrEmpty(
                            configuration.BreakPoint))
                    {
                        EmitBreakPointAfterAnchor(
                            module,
                            resolver,
                            method,
                            il,
                            ref anchor,
                            valueLocal,
                            targetField.FieldType,
                            configuration.BreakPoint,
                            runtimeBreak);
                    }

                    il.InsertAfter(
                        anchor,
                        il.Create(
                            OpCodes.Ldloc,
                            valueLocal));

                    changes++;
                    continue;
                }

                if (instruction.OpCode == OpCodes.Stfld ||
                    instruction.OpCode == OpCodes.Stsfld)
                {
                    FieldReference fieldReference =
                        instruction.Operand as FieldReference;

                    if (!IsTargetField(
                            fieldReference,
                            targetField))
                    {
                        continue;
                    }

                    PopConfiguration configuration =
                        GetConfiguration(
                            targetField.CustomAttributes,
                            SetMode);

                    if (configuration == null)
                    {
                        continue;
                    }

                    if (targetField.FieldType.IsByReference)
                    {
                        continue;
                    }

                    VariableDefinition valueLocal =
                        new VariableDefinition(
                            targetField.FieldType);

                    method.Body.Variables.Add(valueLocal);
                    method.Body.InitLocals = true;

                    VariableDefinition ownerLocal =
                        null;

                    bool isStatic =
                        instruction.OpCode == OpCodes.Stsfld;

                    if (!isStatic)
                    {
                        TypeReference ownerType =
                            targetField.DeclaringType;

                        if (ownerType.IsValueType)
                        {
                            ownerType =
                                new ByReferenceType(
                                    ownerType);
                        }

                        ownerLocal =
                            new VariableDefinition(
                                ownerType);

                        method.Body.Variables.Add(
                            ownerLocal);
                    }

                    ILProcessor il =
                        method.Body.GetILProcessor();

                    Instruction insertionPoint =
                        GetPrefixStart(
                            instruction);

                    Instruction anchor =
                        il.Create(OpCodes.Nop);

                    il.InsertBefore(
                        insertionPoint,
                        anchor);

                    InsertAfter(
                        il,
                        ref anchor,
                        il.Create(
                            OpCodes.Stloc,
                            valueLocal));

                    if (!isStatic)
                    {
                        InsertAfter(
                            il,
                            ref anchor,
                            il.Create(
                                OpCodes.Stloc,
                                ownerLocal));
                    }

                    MethodReference runtimeMethod =
                        ResolveDebugMethod(
                            module,
                            targetField.DeclaringType,
                            method.DeclaringType,
                            configuration,
                            GetValueRuntimeParameterCount(
                                configuration.ValuePath),
                            method.IsStatic,
                            FindRuntimeMethodByConfiguration(
                                configuration.ValuePath,
                                runtimeSet3,
                                runtimeSet4));

                    // When the field is instance-based, the owner has already been
                    // moved to ownerLocal above. The runtime callback only needs the
                    // field value, so the owner does not need to be kept on the stack.
                    EmitGetOrSetCallAfterAnchor(
                        module,
                        il,
                        ref anchor,
                        targetField.DeclaringType.FullName,
                        targetField.Name,
                        valueLocal,
                        targetField.FieldType,
                        configuration.ValuePath,
                        runtimeMethod);

                    if (!string.IsNullOrEmpty(
                            configuration.BreakPoint))
                    {
                        EmitBreakPointAfterAnchor(
                            module,
                            resolver,
                            method,
                            il,
                            ref anchor,
                            valueLocal,
                            targetField.FieldType,
                            configuration.BreakPoint,
                            runtimeBreak);
                    }

                    if (!isStatic)
                    {
                        InsertAfter(
                            il,
                            ref anchor,
                            il.Create(
                                OpCodes.Ldloc,
                                ownerLocal));

                        InsertAfter(
                            il,
                            ref anchor,
                            il.Create(
                                OpCodes.Ldloc,
                                valueLocal));
                    }
                    else
                    {
                        InsertAfter(
                            il,
                            ref anchor,
                            il.Create(
                                OpCodes.Ldloc,
                                valueLocal));
                    }

                    changes++;
                }
            }

            return changes;
        }

        private static bool IsTargetField(
            FieldReference fieldReference,
            FieldDefinition targetField)
        {
            if (fieldReference == null ||
                targetField == null)
            {
                return false;
            }

            try
            {
                FieldDefinition resolved =
                    fieldReference.Resolve();

                if (resolved != null)
                {
                    return
                        resolved.MetadataToken ==
                        targetField.MetadataToken &&
                        resolved.DeclaringType.FullName ==
                        targetField.DeclaringType.FullName;
                }
            }
            catch
            {
                // Fall back to a metadata-name comparison below.
            }

            return
                fieldReference.Name == targetField.Name &&
                fieldReference.DeclaringType.FullName ==
                targetField.DeclaringType.FullName;
        }

        private static void EmitGetOrSetCall(
            ModuleDefinition module,
            ILProcessor il,
            Instruction before,
            string typeName,
            string memberName,
            VariableDefinition valueLocal,
            TypeReference valueType,
            string valuePath,
            MethodReference runtimeMethod)
        {
            if (runtimeMethod.Parameters.Count == 0)
            {
                EmitLoadCustomInstanceIfNeeded(
                    il,
                    before,
                    runtimeMethod);

                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Call,
                        runtimeMethod));

                return;
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldstr,
                    typeName));

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldstr,
                    memberName));

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldloc,
                    valueLocal));

            BoxIfNeeded(
                il,
                before,
                valueType);

            if (!string.IsNullOrEmpty(valuePath))
            {
                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Ldstr,
                        valuePath));
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    runtimeMethod));
        }

        private static void EmitGetOrSetCallAfterAnchor(
            ModuleDefinition module,
            ILProcessor il,
            ref Instruction anchor,
            string typeName,
            string memberName,
            VariableDefinition valueLocal,
            TypeReference valueType,
            string valuePath,
            MethodReference runtimeMethod)
        {
            if (runtimeMethod.Parameters.Count == 0)
            {
                EmitLoadCustomInstanceAfterAnchor(
                    il,
                    ref anchor,
                    runtimeMethod);

                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        OpCodes.Call,
                        runtimeMethod));

                return;
            }

            InsertAfter(
                il,
                ref anchor,
                il.Create(
                    OpCodes.Ldstr,
                    typeName));

            InsertAfter(
                il,
                ref anchor,
                il.Create(
                    OpCodes.Ldstr,
                    memberName));

            InsertAfter(
                il,
                ref anchor,
                il.Create(
                    OpCodes.Ldloc,
                    valueLocal));

            if (valueType.IsValueType)
            {
                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        OpCodes.Box,
                        module.ImportReference(valueType)));
            }

            if (!string.IsNullOrEmpty(valuePath))
            {
                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        OpCodes.Ldstr,
                        valuePath));
            }

            InsertAfter(
                il,
                ref anchor,
                il.Create(
                    OpCodes.Call,
                    runtimeMethod));
        }

        private static void EmitGetOrSetCallFromArgument(
            ModuleDefinition module,
            ILProcessor il,
            Instruction before,
            string typeName,
            string memberName,
            ParameterDefinition valueParameter,
            TypeReference valueType,
            string valuePath,
            MethodReference runtimeMethod)
        {
            if (runtimeMethod.Parameters.Count == 0)
            {
                EmitLoadCustomInstanceIfNeeded(
                    il,
                    before,
                    runtimeMethod);

                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Call,
                        runtimeMethod));

                return;
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldstr,
                    typeName));

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldstr,
                    memberName));

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldarg,
                    valueParameter));

            BoxIfNeeded(
                il,
                before,
                valueType);

            if (!string.IsNullOrEmpty(valuePath))
            {
                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Ldstr,
                        valuePath));
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    runtimeMethod));
        }

        private static void EmitInvokeDebugCall(
            ILProcessor il,
            Instruction before,
            string typeName,
            string memberName,
            MethodReference debugMethod)
        {
            if (debugMethod.Parameters.Count == 0)
            {
                EmitLoadCustomInstanceIfNeeded(
                    il,
                    before,
                    debugMethod);

                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Call,
                        debugMethod));

                return;
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldstr,
                    typeName));

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldstr,
                    memberName));

            EmitLoadCustomInstanceIfNeeded(
                il,
                before,
                debugMethod);

            // Static/default Invoke receives string/string. For an instance
            // custom method with parameters, the current design does not allow
            // that form; custom methods are intentionally parameterless.
            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    debugMethod));
        }

        private static void EmitLoadCustomInstanceAfterAnchor(
            ILProcessor il,
            ref Instruction anchor,
            MethodReference method)
        {
            MethodDefinition resolved =
                method?.Resolve();

            if (resolved == null ||
                resolved.IsStatic)
            {
                return;
            }

            InsertAfter(
                il,
                ref anchor,
                il.Create(
                    OpCodes.Ldarg_0));
        }

        private static void EmitLoadCustomInstanceIfNeeded(
            ILProcessor il,
            Instruction before,
            MethodReference method)
        {
            MethodDefinition resolved =
                method?.Resolve();

            if (resolved == null ||
                resolved.IsStatic)
            {
                return;
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Ldarg_0));
        }

        private static void BoxIfNeeded(
            ILProcessor il,
            Instruction before,
            TypeReference type)
        {
            if (type == null)
            {
                return;
            }

            if (!type.IsValueType &&
                !type.IsGenericParameter)
            {
                return;
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Box,
                    type));
        }

        private static int GetValueRuntimeParameterCount(
            string valuePath)
        {
            return string.IsNullOrEmpty(valuePath)
                ? 3
                : 4;
        }

        private static MethodReference
            FindRuntimeMethodByConfiguration(
                string valuePath,
                MethodReference runtime3,
                MethodReference runtime4)
        {
            return GetValueRuntimeParameterCount(valuePath) == 3
                ? runtime3
                : runtime4;
        }

        private static void EmitBreakPointFromArgument(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            MethodDefinition method,
            ILProcessor il,
            ArgumentSource source,
            TypeReference valueType,
            string breakPoint,
            MethodReference runtimeBreak,
            Instruction before)
        {
            BreakPointMember member =
                FindBreakPointMember(
                    valueType,
                    breakPoint,
                    resolver);

            if (member == null)
            {
                return;
            }

            var end =
                il.Create(OpCodes.Nop);

            EmitBreakPointMemberTest(
                module,
                il,
                before,
                source,
                valueType,
                member,
                runtimeBreak,
                end);

            il.InsertBefore(
                before,
                end);
        }

        private static void EmitBreakPointFromLocal(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            MethodDefinition method,
            ILProcessor il,
            Instruction before,
            VariableDefinition local,
            TypeReference valueType,
            string breakPoint,
            MethodReference runtimeBreak)
        {
            BreakPointMember member =
                FindBreakPointMember(
                    valueType,
                    breakPoint,
                    resolver);

            if (member == null)
            {
                return;
            }

            var end =
                il.Create(OpCodes.Nop);

            EmitBreakPointMemberTest(
                module,
                il,
                before,
                new LocalSource(local),
                valueType,
                member,
                runtimeBreak,
                end);

            il.InsertBefore(
                before,
                end);
        }

        private static void EmitBreakPointAfterAnchor(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            MethodDefinition method,
            ILProcessor il,
            ref Instruction anchor,
            VariableDefinition local,
            TypeReference valueType,
            string breakPoint,
            MethodReference runtimeBreak)
        {
            BreakPointMember member =
                FindBreakPointMember(
                    valueType,
                    breakPoint,
                    resolver);

            if (member == null)
            {
                return;
            }

            var end =
                il.Create(OpCodes.Nop);

            InsertBreakPointAfterAnchor(
                module,
                il,
                ref anchor,
                new LocalSource(local),
                valueType,
                member,
                runtimeBreak,
                end);

            InsertAfter(
                il,
                ref anchor,
                end);
        }

        private static void EmitBreakPointMemberTest(
            ModuleDefinition module,
            ILProcessor il,
            Instruction before,
            ValueSource source,
            TypeReference valueType,
            BreakPointMember member,
            MethodReference runtimeBreak,
            Instruction end)
        {
            if (member.IsStatic)
            {
                EmitBreakPointMemberLoadOnly(
                    module,
                    il,
                    before,
                    member);
            }
            else if (valueType.IsValueType)
            {
                EmitLoadAddress(
                    il,
                    before,
                    source);

                EmitBreakPointMemberLoadOnly(
                    module,
                    il,
                    before,
                    member);
            }
            else
            {
                Instruction hasValue =
                    il.Create(OpCodes.Nop);

                EmitLoadValue(
                    il,
                    before,
                    source);

                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Dup));

                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Brtrue,
                        hasValue));

                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Pop));

                il.InsertBefore(
                    before,
                    il.Create(
                        OpCodes.Br,
                        end));

                il.InsertBefore(
                    before,
                    hasValue);

                EmitBreakPointMemberLoadOnly(
                    module,
                    il,
                    before,
                    member);
            }

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Brfalse,
                    end));

            il.InsertBefore(
                before,
                il.Create(
                    OpCodes.Call,
                    runtimeBreak));
        }

        private static void EmitBreakPointMemberLoadOnly(
            ModuleDefinition module,
            ILProcessor il,
            Instruction before,
            BreakPointMember member)
        {
            if (member == null)
            {
                return;
            }

            if (member.Field != null)
            {
                FieldReference field =
                    module.ImportReference(
                        member.Field);

                il.InsertBefore(
                    before,
                    il.Create(
                        member.Field.IsStatic
                            ? OpCodes.Ldsfld
                            : OpCodes.Ldfld,
                        field));

                return;
            }

            if (member.Property != null &&
                member.Property.GetMethod != null)
            {
                MethodDefinition getter =
                    member.Property.GetMethod;

                MethodReference getterReference =
                    module.ImportReference(
                        getter);

                il.InsertBefore(
                    before,
                    il.Create(
                        getter.IsStatic
                            ? OpCodes.Call
                            : getter.IsVirtual
                                ? OpCodes.Callvirt
                                : OpCodes.Call,
                        getterReference));
            }
        }

        private static void InsertBreakPointMemberLoadAfterAnchor(
            ModuleDefinition module,
            ILProcessor il,
            ref Instruction anchor,
            BreakPointMember member)
        {
            if (member == null)
            {
                return;
            }

            if (member.Field != null)
            {
                FieldReference field =
                    module.ImportReference(
                        member.Field);

                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        member.Field.IsStatic
                            ? OpCodes.Ldsfld
                            : OpCodes.Ldfld,
                        field));

                return;
            }

            if (member.Property != null &&
                member.Property.GetMethod != null)
            {
                MethodDefinition getter =
                    member.Property.GetMethod;

                MethodReference getterReference =
                    module.ImportReference(
                        getter);

                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        getter.IsStatic
                            ? OpCodes.Call
                            : getter.IsVirtual
                                ? OpCodes.Callvirt
                                : OpCodes.Call,
                        getterReference));
            }
        }

        private static void InsertBreakPointAfterAnchor(
            ModuleDefinition module,
            ILProcessor il,
            ref Instruction anchor,
            ValueSource source,
            TypeReference valueType,
            BreakPointMember member,
            MethodReference runtimeBreak,
            Instruction end)
        {
            if (member.IsStatic)
            {
                // No receiver required.
            }
            else if (valueType.IsValueType)
            {
                InsertAfter(
                    il,
                    ref anchor,
                    CreateLoadAddressInstruction(
                        source));
            }
            else
            {
                Instruction hasValue =
                    il.Create(OpCodes.Nop);

                InsertAfter(
                    il,
                    ref anchor,
                    CreateLoadValueInstruction(
                        source));

                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        OpCodes.Dup));

                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        OpCodes.Brtrue,
                        hasValue));

                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        OpCodes.Pop));

                InsertAfter(
                    il,
                    ref anchor,
                    il.Create(
                        OpCodes.Br,
                        end));

                InsertAfter(
                    il,
                    ref anchor,
                    hasValue);
            }

            InsertBreakPointMemberLoadAfterAnchor(
                module,
                il,
                ref anchor,
                member);

            InsertAfter(
                il,
                ref anchor,
                il.Create(
                    OpCodes.Brfalse,
                    end));

            InsertAfter(
                il,
                ref anchor,
                il.Create(
                    OpCodes.Call,
                    runtimeBreak));
        }

        private static void EmitLoadValue(
            ILProcessor il,
            Instruction before,
            ValueSource source)
        {
            il.InsertBefore(
                before,
                source.CreateLoad());
        }

        private static void EmitLoadAddress(
            ILProcessor il,
            Instruction before,
            ValueSource source)
        {
            il.InsertBefore(
                before,
                source.CreateLoadAddress());
        }

        private static Instruction CreateLoadValueInstruction(
            ValueSource source)
        {
            return source.CreateLoad();
        }

        private static Instruction CreateLoadAddressInstruction(
            ValueSource source)
        {
            return source.CreateLoadAddress();
        }

        private static BreakPointMember FindBreakPointMember(
            TypeReference valueType,
            string breakPoint,
            IAssemblyResolver resolver)
        {
            if (valueType == null ||
                string.IsNullOrEmpty(breakPoint))
            {
                return null;
            }

            TypeDefinition type =
                SafeResolve(valueType);

            if (type == null)
            {
                return null;
            }

            TypeDefinition current =
                type;

            while (current != null)
            {
                FieldDefinition field =
                    current.Fields.FirstOrDefault(
                        x =>
                            x.Name == breakPoint &&
                            x.FieldType.MetadataType ==
                                MetadataType.Boolean);

                if (field != null)
                {
                    return new BreakPointMember
                    {
                        Field = field
                    };
                }

                PropertyDefinition property =
                    current.Properties.FirstOrDefault(
                        x =>
                            x.Name == breakPoint &&
                            x.GetMethod != null &&
                            x.GetMethod.ReturnType.MetadataType ==
                                MetadataType.Boolean &&
                            x.Parameters.Count == 0);

                if (property != null)
                {
                    return new BreakPointMember
                    {
                        Property = property
                    };
                }

                if (current.BaseType == null)
                {
                    break;
                }

                current =
                    SafeResolve(current.BaseType);
            }

            return null;
        }

        private static TypeDefinition SafeResolve(
            TypeReference type)
        {
            try
            {
                return type?.Resolve();
            }
            catch
            {
                return null;
            }
        }

        private static PopConfiguration GetConfiguration(
            IEnumerable<CustomAttribute> attributes,
            int mode)
        {
            if (attributes == null)
            {
                return null;
            }

            foreach (CustomAttribute attribute in attributes)
            {
                PopConfiguration configuration =
                    ReadConfiguration(attribute);

                if (configuration == null ||
                    configuration.Mode != mode)
                {
                    continue;
                }

                return configuration;
            }

            return null;
        }

        private static PopConfiguration ReadConfiguration(
            CustomAttribute attribute)
        {
            if (attribute == null)
            {
                return null;
            }

            if (attribute.AttributeType.FullName !=
                AttributeName)
            {
                return null;
            }

            if (attribute.ConstructorArguments.Count == 0)
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

            string valuePath =
                null;

            string customDebugMethod =
                null;

            if (attribute.ConstructorArguments.Count >= 2)
            {
                breakPoint =
                    attribute
                        .ConstructorArguments[1]
                        .Value as string;
            }

            foreach (CustomAttributeNamedArgument property
                     in attribute.Properties)
            {
                if (property.Name ==
                    BreakPointName)
                {
                    breakPoint =
                        property.Argument.Value as string;

                    continue;
                }

                if (property.Name ==
                    ValuePathName)
                {
                    valuePath =
                        property.Argument.Value as string;

                    continue;
                }

                if (property.Name ==
                    CustomDebugMethodName)
                {
                    customDebugMethod =
                        property.Argument.Value as string;
                }
            }

            return new PopConfiguration
            {
                Mode = mode,
                BreakPoint = breakPoint,
                ValuePath = valuePath,
                CustomDebugMethod = customDebugMethod
            };
        }

        private static MethodReference ResolveDebugMethod(
            ModuleDefinition module,
            TypeDefinition declaringType,
            TypeDefinition callerType,
            PopConfiguration configuration,
            int parameterCount,
            bool callerIsStatic,
            MethodReference defaultMethod)
        {
            if (configuration == null ||
                string.IsNullOrEmpty(
                    configuration.CustomDebugMethod))
            {
                return defaultMethod;
            }

            MethodDefinition customMethod =
                FindCustomDebugMethod(
                    declaringType,
                    callerType,
                    configuration.CustomDebugMethod,
                    callerIsStatic,
                    module);

            if (customMethod == null)
            {
                throw new InvalidOperationException(
                    "PopToConsole custom debug method not found or has an invalid signature: " +
                    declaringType.FullName + "." +
                    configuration.CustomDebugMethod +
                    " (expected void()).");
            }

            return module.ImportReference(customMethod);
        }

        private static MethodDefinition FindCustomDebugMethod(
            TypeDefinition declaringType,
            TypeDefinition callerType,
            string methodName,
            bool callerIsStatic,
            ModuleDefinition module)
        {
            if (declaringType == null ||
                string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            MethodDefinition match = null;

            foreach (MethodDefinition method
                     in declaringType.Methods)
            {
                if (method.Name != methodName ||
                    method.IsConstructor ||
                    method.HasGenericParameters ||
                    method.ReturnType.MetadataType !=
                        MetadataType.Void ||
                    method.Parameters.Count != 0)
                {
                    continue;
                }

                if (!method.IsStatic &&
                    (callerIsStatic ||
                     callerType != declaringType))
                {
                    continue;
                }

                if (callerType != declaringType &&
                    !method.IsPublic &&
                    !method.IsAssembly)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "PopToConsole custom debug method is ambiguous: " +
                        declaringType.FullName + "." +
                        methodName +
                        " with zero parameters.");
                }

                match = method;
            }

            return match;
        }

        private static MethodReference FindRuntimeMethod(
            AssemblyDefinition currentAssembly,
            IAssemblyResolver resolver,
            TypeDefinition runtimeType,
            string methodName,
            int parameterCount)
        {
            MethodDefinition method =
                runtimeType.Methods.FirstOrDefault(
                    x =>
                        x.Name == methodName &&
                        x.Parameters.Count == parameterCount &&
                        x.IsStatic);

            if (method != null)
            {
                return
                    currentAssembly.MainModule
                        .ImportReference(method);
            }

            return null;
        }

        private static TypeDefinition FindType(
            AssemblyDefinition currentAssembly,
            IAssemblyResolver resolver,
            string fullName)
        {
            TypeDefinition type =
                FindTypeInAssembly(
                    currentAssembly,
                    fullName);

            if (type != null)
            {
                return type;
            }

            foreach (AssemblyNameReference reference
                     in currentAssembly.MainModule.AssemblyReferences)
            {
                try
                {
                    AssemblyDefinition assembly =
                        resolver.Resolve(reference);

                    type =
                        FindTypeInAssembly(
                            assembly,
                            fullName);

                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Ignore individual unresolved references and continue.
                }
            }

            return null;
        }

        private static TypeDefinition FindTypeInAssembly(
            AssemblyDefinition assembly,
            string fullName)
        {
            if (assembly == null)
            {
                return null;
            }

            foreach (TypeDefinition type
                     in assembly.MainModule.Types)
            {
                TypeDefinition result =
                    FindTypeRecursive(
                        type,
                        fullName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static TypeDefinition FindTypeRecursive(
            TypeDefinition type,
            string fullName)
        {
            if (type.FullName == fullName)
            {
                return type;
            }

            foreach (TypeDefinition nested
                     in type.NestedTypes)
            {
                TypeDefinition result =
                    FindTypeRecursive(
                        nested,
                        fullName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Instruction GetPrefixStart(
            Instruction instruction)
        {
            Instruction start =
                instruction;

            Instruction previous =
                start.Previous;

            while (previous != null &&
                   previous.OpCode.OpCodeType ==
                       OpCodeType.Prefix)
            {
                start = previous;
                previous = previous.Previous;
            }

            return start;
        }

        private static void InsertAfter(
            ILProcessor il,
            ref Instruction anchor,
            Instruction instruction)
        {
            il.InsertAfter(
                anchor,
                instruction);

            anchor = instruction;
        }

        private abstract class ValueSource
        {
            public abstract Instruction CreateLoad();
            public abstract Instruction CreateLoadAddress();
        }

        private sealed class LocalSource : ValueSource
        {
            private readonly VariableDefinition _local;

            public LocalSource(
                VariableDefinition local)
            {
                _local = local;
            }

            public override Instruction CreateLoad()
            {
                return Instruction.Create(
                    OpCodes.Ldloc,
                    _local);
            }

            public override Instruction CreateLoadAddress()
            {
                return Instruction.Create(
                    OpCodes.Ldloca,
                    _local);
            }
        }

        private sealed class ArgumentSource : ValueSource
        {
            private readonly int _index;
            private readonly ParameterDefinition _parameter;

            public ArgumentSource(int index)
            {
                _index = index;
            }

            public ArgumentSource(
                ParameterDefinition parameter)
            {
                _parameter = parameter;
                _index = parameter?.Index ?? 0;
            }

            public override Instruction CreateLoad()
            {
                if (_parameter != null)
                {
                    return Instruction.Create(
                        OpCodes.Ldarg,
                        _parameter);
                }

                return _index switch
                {
                    0 => Instruction.Create(
                        OpCodes.Ldarg_0),

                    1 => Instruction.Create(
                        OpCodes.Ldarg_1),

                    2 => Instruction.Create(
                        OpCodes.Ldarg_2),

                    3 => Instruction.Create(
                        OpCodes.Ldarg_3),

                    _ => Instruction.Create(
                        OpCodes.Ldarg,
                        _index)
                };
            }

            public override Instruction CreateLoadAddress()
            {
                if (_parameter != null)
                {
                    return Instruction.Create(
                        OpCodes.Ldarga,
                        _parameter);
                }

                return Instruction.Create(
                    OpCodes.Ldarga,
                    _index);
            }
        }
    }
}

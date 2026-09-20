using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace AbeAtributes.Editor
{
    public sealed class PopToConsoleILPostProcessor
        : ILPostProcessor
    {
        private const string AttributeName =
            "AbeAttributes.PopToConsoleAttribute";

        private const string RuntimeTypeName =
            "AbeAttributes.PopToConsoleRuntime";

        private const string RuntimeMethodName =
            "OnInvoke";

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

            // Plugins 目录中的普通 Runtime Script
            // 会进入 Assembly-CSharp-firstpass。
            //
            // Assets 根目录下的普通 Runtime Script
            // 会进入 Assembly-CSharp。
            //
            // 第一版先处理这两个 Assembly。
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

        private ILPostProcessResult ProcessInternal(
            ICompiledAssembly compiledAssembly)
        {
            DefaultAssemblyResolver resolver =
                CreateResolver(
                    compiledAssembly);

            ReaderParameters readerParameters =
                new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Immediate
                };

            using MemoryStream peInput =
                new MemoryStream(
                    compiledAssembly
                        .InMemoryAssembly
                        .PeData);

            AssemblyDefinition assembly =
                AssemblyDefinition.ReadAssembly(
                    peInput,
                    readerParameters);

            bool modified =
                false;

            foreach (TypeDefinition type
                     in GetAllTypes(assembly.MainModule))
            {
                foreach (MethodDefinition method
                         in type.Methods)
                {
                    if (!ShouldProcess(method))
                    {
                        continue;
                    }

                    if (!HasInvokeAttribute(method))
                    {
                        continue;
                    }

                    if (AlreadyInjected(method))
                    {
                        continue;
                    }

                    InjectInvoke(
                        assembly,
                        resolver,
                        type,
                        method);

                    modified = true;
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

        private static ILPostProcessResult WriteAssembly(
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

        private static bool ShouldProcess(
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

        private static bool HasInvokeAttribute(
            MethodDefinition method)
        {
            if (!method.HasCustomAttributes)
            {
                return false;
            }

            foreach (CustomAttribute attribute
                     in method.CustomAttributes)
            {
                if (attribute.AttributeType.FullName
                    != AttributeName)
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Count
                    != 1)
                {
                    continue;
                }

                CustomAttributeArgument argument =
                    attribute
                        .ConstructorArguments[0];

                if (argument.Value == null)
                {
                    continue;
                }

                int mode =
                    Convert.ToInt32(
                        argument.Value);

                // Invoke = 0
                // Manual = 1
                if (mode == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AlreadyInjected(
            MethodDefinition method)
        {
            foreach (Instruction instruction
                     in method.Body.Instructions)
            {
                if (instruction.OpCode
                    != OpCodes.Call)
                {
                    continue;
                }

                if (!(instruction.Operand
                      is MethodReference methodReference))
                {
                    continue;
                }

                if (methodReference.Name
                    != RuntimeMethodName)
                {
                    continue;
                }

                if (methodReference.DeclaringType
                    .FullName != RuntimeTypeName)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void InjectInvoke(
            AssemblyDefinition assembly,
            DefaultAssemblyResolver resolver,
            TypeDefinition type,
            MethodDefinition method)
        {
            MethodReference runtimeMethod =
                FindRuntimeMethod(
                    assembly,
                    resolver);

            ILProcessor il =
                method.Body.GetILProcessor();

            Instruction first =
                method.Body.Instructions[0];

            string typeName =
                type.FullName;

            string methodName =
                method.Name;

            Instruction loadTypeName =
                il.Create(
                    OpCodes.Ldstr,
                    typeName);

            Instruction loadMethodName =
                il.Create(
                    OpCodes.Ldstr,
                    methodName);

            Instruction call =
                il.Create(
                    OpCodes.Call,
                    runtimeMethod);

            il.InsertBefore(
                first,
                loadTypeName);

            il.InsertBefore(
                first,
                loadMethodName);

            il.InsertBefore(
                first,
                call);
        }

        private static MethodReference
            FindRuntimeMethod(
                AssemblyDefinition assembly,
                DefaultAssemblyResolver resolver)
        {
            // -------------------------------------------------
            // 1. 先检查当前 Assembly
            // -------------------------------------------------

            MethodDefinition localMethod =
                FindRuntimeMethod(
                    assembly.MainModule);

            if (localMethod != null)
            {
                return localMethod;
            }

            // -------------------------------------------------
            // 2. 再检查当前 Assembly 引用的 Assembly
            // -------------------------------------------------

            foreach (AssemblyNameReference reference
                     in assembly.MainModule
                         .AssemblyReferences)
            {
                AssemblyDefinition referencedAssembly;

                try
                {
                    referencedAssembly =
                        resolver.Resolve(reference);
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
                        referencedAssembly.MainModule);

                if (referencedMethod == null)
                {
                    continue;
                }

                return assembly.MainModule
                    .ImportReference(
                        referencedMethod);
            }

            throw new InvalidOperationException(
                $"Cannot find " +
                $"{RuntimeTypeName}.{RuntimeMethodName}" +
                "(string, string).");
        }

        private static MethodDefinition
            FindRuntimeMethod(
                ModuleDefinition module)
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
                    if (method.Name
                        != RuntimeMethodName)
                    {
                        continue;
                    }

                    if (!method.IsStatic)
                    {
                        continue;
                    }

                    if (method.ReturnType.FullName
                        != module.TypeSystem.Void.FullName)
                    {
                        continue;
                    }

                    if (method.Parameters.Count != 2)
                    {
                        continue;
                    }

                    if (method.Parameters[0]
                            .ParameterType.FullName
                        != module.TypeSystem.String.FullName)
                    {
                        continue;
                    }

                    if (method.Parameters[1]
                            .ParameterType.FullName
                        != module.TypeSystem.String.FullName)
                    {
                        continue;
                    }

                    return method;
                }
            }

            return null;
        }

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
                        "[PopToConsole] " +
                        exception
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
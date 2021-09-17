using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

using Debug = Padoru.Diagnostics.Debug;

namespace Padoru.Networking
{
	public static class CompilationFinishedHook
    {
        [InitializeOnLoadMethod]
        public static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (CompilerMessagesContainError(messages))
            {
                Debug.Log("Weaver: stop because compile errors on target");
                return;
            }

            if (IsEditorAssembly(assemblyPath)) return;

            /*
            // don't weave mirror files
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            if (assemblyName == MirrorRuntimeAssemblyName || assemblyName == MirrorWeaverAssemblyName)
            {
                return;
            }
            */

            HashSet<string> dependencyPaths = GetDependecyPaths(assemblyPath);
            //dependencyPaths.Add(Path.GetDirectoryName(mirrorRuntimeDll));
            //dependencyPaths.Add(Path.GetDirectoryName(unityEngineCoreModuleDLL));
        }

        private static bool CompilerMessagesContainError(CompilerMessage[] messages)
        {
            return messages.Any(msg => msg.type == CompilerMessageType.Error);
        }

        private static bool IsEditorAssembly(string assemblyPath)
        {
            return assemblyPath.Contains("-Editor") || assemblyPath.Contains(".Editor");
        }

        private static HashSet<string> GetDependecyPaths(string assemblyPath)
        {
            // build directory list for later asm/symbol resolving using CompilationPipeline refs
            HashSet<string> dependencyPaths = new HashSet<string>
            {
                Path.GetDirectoryName(assemblyPath)
            };
            foreach (Assembly unityAsm in CompilationPipeline.GetAssemblies())
            {
                if (unityAsm.outputPath == assemblyPath)
                {
                    foreach (string unityAsmRef in unityAsm.compiledAssemblyReferences)
                    {
                        dependencyPaths.Add(Path.GetDirectoryName(unityAsmRef));
                    }
                }
            }

            return dependencyPaths;
        }
    }
}

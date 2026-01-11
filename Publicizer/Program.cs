using System;
using System.Linq;
using Mono.Cecil;

namespace Publicizer {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Usage: Publicizer <input> <output>");
                return;
            }
            string input = args[0];
            string output = args[1];
            
            try {
                Console.WriteLine($"Reading {input}...");
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(input));
                
                var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters { AssemblyResolver = resolver });
                
                int typesChanged = 0;
                int methodsChanged = 0;
                int fieldsChanged = 0;

                foreach (var type in assembly.MainModule.Types) {
                    if (type.IsNested) {
                        if (!type.IsNestedPublic) {
                            type.IsNestedPublic = true;
                            typesChanged++;
                        }
                    } else {
                        if (!type.IsPublic) {
                            type.IsPublic = true;
                            typesChanged++;
                        }
                    }

                    var eventNames = new System.Collections.Generic.HashSet<string>(type.Events.Select(e => e.Name));

                    foreach (var field in type.Fields) {
                        if (eventNames.Contains(field.Name)) {
                            Console.WriteLine($"Renaming colliding field {field.Name} to {field.Name}_Field in {type.Name}");
                            field.Name = field.Name + "_Field";
                        }
                        if (!field.IsPublic) {
                            field.IsPublic = true;
                            fieldsChanged++;
                        }
                    }

                    foreach (var method in type.Methods) {
                        if (!method.IsPublic) {
                            method.IsPublic = true;
                            methodsChanged++;
                        }
                    }
                }

                Console.WriteLine($"Publicized: {typesChanged} types, {fieldsChanged} fields, {methodsChanged} methods.");
                Console.WriteLine($"Saving to {output}...");
                assembly.Write(output);
                Console.WriteLine("Done.");
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null) {
                    Console.WriteLine("Inner: " + ex.InnerException.ToString());
                }
            }
        }
    }
}

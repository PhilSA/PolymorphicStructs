using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;

namespace PolymorphicStructsSourceGenerators
{
    [Generator]
    public class PolymorphicStructsSourceGenerator : ISourceGenerator
    {
        private const string typeEnumName = "TypeId";
        private const string typeEnumVarName = "CurrentTypeId";

        public class IndividialStructData
        {
            public string Namespace = "";
            public string StructName = "";
            public List<StructFieldData> Fields = new List<StructFieldData>();
        }

        public class StructFieldData
        {
            public string TypeName = "";
            public string FieldName = "";
            public string MergedFieldName = "";
        }

        public class MergedFieldData
        {
            public string TypeName = "";
            public string FieldName = "";
            public Dictionary<string, string> FieldNameForStructName = new Dictionary<string, string>();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new PolymorphicStructSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();
            System.Console.WriteLine(
                $"PolymorphicStructs sourceGenerator execute  on assembly {context.Compilation.AssemblyName}");

            try
            {
                PolymorphicStructSyntaxReceiver systemReceiver = (PolymorphicStructSyntaxReceiver)context.SyntaxReceiver;

                // Find all polymorphic struct interfaces, based on attribute
                IEnumerable<InterfaceDeclarationSyntax> polymorphicInterfaces = systemReceiver.PolymorphicInterfaces;

                foreach (var polymorphicInterface in polymorphicInterfaces)
                {
                    GenerateInterfacesCode(context, systemReceiver, polymorphicInterface);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"SourceGenerators ERROR: {ex.Message}");
                var diagnosticDescriptor = new DiagnosticDescriptor("PolymorphicStructsError", "PolymorphicStructsError", $"Generation failed with {ex.Message}", "PolymorphicStructsError", DiagnosticSeverity.Error, true);
                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, Location.None, DiagnosticSeverity.Error));
            }
        }
        

        public class StructDef
        {
            public string MergedStructName;
            public string InterfaceName;
            public string Namespace;
            public List<string> UsingDirectives;
        }

        private void GenerateInterfacesCode(GeneratorExecutionContext context,
            PolymorphicStructSyntaxReceiver systemReceiver,
            InterfaceDeclarationSyntax polymorphicInterface)
        {
            // For each polymorphic struct interface, generate a polymorphic struct
            var mergetStructDef = CollectStructHeaderDef(context, polymorphicInterface);
            var allMemberSymbols = SourceGenUtils.GetAllMemberSymbols(context, polymorphicInterface);
            // Build list of all structs implementing this interface, as well as the fields for each of them
            List<IndividialStructData> structDatas =
                BuildIndividualStructsData(context, systemReceiver, mergetStructDef, polymorphicInterface);

            if (structDatas.Count == 0)
                return; // maybe we still should generate a struct in this case

            // Build list of merged fields across all structs
            List<MergedFieldData> mergedFields = BuildMergedFields(structDatas);

            var mergedStructSourceText = GenerateMergedStruct(mergetStructDef, allMemberSymbols, mergedFields, structDatas);
            System.Console.WriteLine($"Generating PolyInterface {mergetStructDef.MergedStructName}");

            context.AddSource(mergetStructDef.MergedStructName, mergedStructSourceText);

            // For each individual struct, generate From/To converter methods
            foreach (IndividialStructData structData in structDatas)
            {
                GeneratePartialIndividualStruct(context, mergetStructDef.UsingDirectives, mergetStructDef.MergedStructName, structData, mergedFields);
            }
        }

        private SourceText GenerateMergedStruct(StructDef structDef, List<ISymbol> allMemberSymbols,
            List<MergedFieldData> mergedFieldDatas, List<IndividialStructData> individialStructDatas)
        {
            FileWriter structWriter = new FileWriter();
            // Generate usings
            GenerateUsingDirectives(structWriter, structDef);

            structWriter.WriteLine("");

            // Open namespace
            var hasNamespace = !string.IsNullOrEmpty(structDef.Namespace);
            if (hasNamespace)
            {
                structWriter.WriteLine($"namespace {structDef.Namespace}");
                structWriter.BeginScope();
            }

            GenerateStructHeader(structWriter, structDef);
            structWriter.BeginScope();
            GenerateTypeEnum(structWriter, individialStructDatas);
            structWriter.WriteLine("");
            GenerateFields(structWriter, mergedFieldDatas);
            structWriter.WriteLine("");
            // GenerateProperties
            GenerateMethods(structWriter, structDef, allMemberSymbols, individialStructDatas);
            structWriter.WriteLine("");
            structWriter.EndScope();
            if (hasNamespace)
            {
                structWriter.EndScope();
            }

            return SourceText.From(structWriter.FileContents, Encoding.UTF8);
        }

        private void GenerateMethods(FileWriter structWriter, StructDef structDef, List<ISymbol> allMemberSymbols,
            List<IndividialStructData> individialStructDatas)
        {
            foreach (var memberSymbol in allMemberSymbols)
            {
                if (memberSymbol is IMethodSymbol methodSymbol)
                {
                    // if (!Debugger.IsAttached)
                    //     Debugger.Launch();
                    // Debugger.Break();
                    var type = methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.ToDisplayString();
                    var parameters = string.Join(", ", methodSymbol.Parameters.Select(it => $"{MapRefKind(it.RefKind)}{it.Type} {it.Name}"));
                    var parametersWithoutType = string.Join(", ", methodSymbol.Parameters.Select(it => $"{MapRefKind(it.RefKind)}{it.Name}"));
                    structWriter.WriteLine($"public {type} {methodSymbol.Name}({parameters})");
                    GenerateMethodBody(structWriter, structDef, methodSymbol, individialStructDatas, $"{methodSymbol.Name}({parametersWithoutType})");
                } else if (memberSymbol is IPropertySymbol propertySymbol)
                {
                    structWriter.WriteLine($"public {propertySymbol.Type} {propertySymbol.Name}");
                    structWriter.BeginScope();
                    if (propertySymbol.GetMethod != null)
                        GeneratePropertyGetMethod(structWriter, structDef, propertySymbol.GetMethod, individialStructDatas, propertySymbol.Name);
                    if (propertySymbol.SetMethod != null)
                        GeneratePropertySetMethod(structWriter, structDef, propertySymbol.SetMethod, individialStructDatas, propertySymbol.Name);
                    structWriter.EndScope();
                }
            }
        }

        private void GeneratePropertyGetMethod(FileWriter structWriter, StructDef structDef, IMethodSymbol methodSymbol,
            List<IndividialStructData> structDatas, string propertyName)
        {
            structWriter.WriteLine("get");
            GenerateMethodBody(structWriter, structDef, methodSymbol, structDatas, $"{propertyName}");
        }

        private void GeneratePropertySetMethod(FileWriter structWriter, StructDef structDef, IMethodSymbol methodSymbol,
            List<IndividialStructData> structDatas, string propertyName)
        {
            structWriter.WriteLine("set");
            GenerateMethodBody(structWriter, structDef, methodSymbol, structDatas, $"{propertyName} = value");
        }

        private void GenerateMethodBody(FileWriter structWriter, StructDef structDef, IMethodSymbol methodSymbol,
            List<IndividialStructData> structDatas, string callClause)
        {
            structWriter.BeginScope();
            structWriter.WriteLine($"switch({typeEnumVarName})");
            structWriter.BeginScope();
            var returnsVoid = methodSymbol.ReturnsVoid;
            foreach (IndividialStructData structData in structDatas)
            {
                structWriter.WriteLine($"case {typeEnumName}.{structData.StructName}:");
                structWriter.BeginScope();
                string structVarName = $"instance_{structData.StructName}";
                structWriter.WriteLine($"{structData.StructName} {structVarName} = new {structData.StructName}(this);");
                structWriter.WriteLine($"{(returnsVoid ? "" : "var r = ")}{structVarName}.{callClause};");
                structWriter.WriteLine($"{structVarName}.To{structDef.MergedStructName}(ref this);");
                structWriter.WriteLine(returnsVoid ? "break;" : "return r;");
                structWriter.EndScope();
            }

            structWriter.WriteLine("default:");
            structWriter.BeginScope();
            foreach (var param in methodSymbol.Parameters)
            {
                if (param.RefKind == RefKind.Out)
                    structWriter.WriteLine($"{param.Name} = default;");
            }
            structWriter.WriteLine($"return{(returnsVoid ? "" : " default")};");
            structWriter.EndScope();
            structWriter.EndScope();
            structWriter.EndScope();
        }

        private string MapRefKind(RefKind argRefKind)
        {
            switch(argRefKind)
            {
                case RefKind.None:
                    return "";
                case RefKind.Ref:
                    return "ref ";
                case RefKind.Out:
                    return "out ";
                case RefKind.In:
                    return "in ";
                default:
                    throw new ArgumentOutOfRangeException(nameof(argRefKind), argRefKind, null);
            }
        }

        private void GenerateFields(FileWriter structWriter, List<MergedFieldData> mergedFields)
        {
            structWriter.WriteLine($"public {typeEnumName} {typeEnumVarName};");

            for (int i = 0; i < mergedFields.Count; i++)
            {
                MergedFieldData mergedField = mergedFields[i];
                structWriter.WriteLine($"public {mergedField.TypeName} {mergedField.FieldName};");
            }
        }

        private void GenerateTypeEnum(FileWriter structWriter, List<IndividialStructData> structDatas)
        {
            structWriter.WriteLine($"public enum {typeEnumName}");
            structWriter.BeginScope();
            {
                foreach (IndividialStructData structData in structDatas)
                {
                    structWriter.WriteLine($"{structData.StructName},");
                }
            }
            structWriter.EndScope();
        }

        private void GenerateStructHeader(FileWriter structWriter, StructDef structDef)
        {
            structWriter.WriteLine("[Serializable]");
            structWriter.WriteLine($"public partial struct {structDef.MergedStructName} : {structDef.InterfaceName}"); // TODO: how to define custom interfaces it can implement
        }

        private static void GenerateUsingDirectives(FileWriter mergedStructWriter, StructDef structDef)
        {
            foreach (string u in structDef.UsingDirectives)
            {
                mergedStructWriter.WriteLine($"using {u};");
            }
        }

        private static StructDef CollectStructHeaderDef(GeneratorExecutionContext context, InterfaceDeclarationSyntax polymorphicInterface)
        {
            string mergedStructName = polymorphicInterface.Identifier.Text.Substring(1); // TODO; must find better
            // Get interface namespace
            string interfaceNamespace = SourceGenUtils.GetNamespace(polymorphicInterface);
            // Build a list of all usings
            List<string> allUsings = new List<string>();
            TryAddUniqueUsing(allUsings, "System");
            if (!string.IsNullOrEmpty(interfaceNamespace))
            {
                TryAddUniqueUsing(allUsings, interfaceNamespace);
            }

            foreach (var u in polymorphicInterface.SyntaxTree.GetCompilationUnitRoot(context.CancellationToken).Usings)
            {
                TryAddUniqueUsing(allUsings, u.Name.ToString());
            }
            return new StructDef
            {
                InterfaceName = polymorphicInterface.Identifier.ToString(),
                MergedStructName = mergedStructName,
                Namespace = interfaceNamespace,
                UsingDirectives = allUsings,
            };
        }

        private static List<MergedFieldData> BuildMergedFields(List<IndividialStructData> structDatas)
        {
            List<MergedFieldData> mergedFields = new List<MergedFieldData>();
            List<int> usedIndexesInMergedFields = new List<int>();
            foreach (IndividialStructData structData in structDatas)
            {
                usedIndexesInMergedFields.Clear();

                // For each field of the struct
                for (int fieldIndex = 0; fieldIndex < structData.Fields.Count; fieldIndex++)
                {
                    StructFieldData fieldData = structData.Fields[fieldIndex];

                    // Try find match in merged fields
                    int matchingMergedFieldIndex = -1;
                    for (int mergedFieldIndex = 0; mergedFieldIndex < mergedFields.Count; mergedFieldIndex++)
                    {
                        if (!usedIndexesInMergedFields.Contains(mergedFieldIndex))
                        {
                            if (string.Equals(fieldData.TypeName, mergedFields[mergedFieldIndex].TypeName))
                            {
                                matchingMergedFieldIndex = mergedFieldIndex;
                                break;
                            }
                        }
                    }

                    // Add new merged field if didn't find match
                    if (matchingMergedFieldIndex < 0)
                    {
                        int indexOfAddedMergedField = mergedFields.Count;

                        MergedFieldData mergedFieldData = new MergedFieldData();
                        mergedFieldData.TypeName = fieldData.TypeName;
                        mergedFieldData.FieldName = $"{fieldData.TypeName}_{mergedFields.Count}";
                        mergedFieldData.FieldNameForStructName.Add(structData.StructName, fieldData.FieldName);
                        mergedFields.Add(mergedFieldData);

                        fieldData.MergedFieldName = mergedFieldData.FieldName;
                        usedIndexesInMergedFields.Add(indexOfAddedMergedField);
                    }
                    else
                    {
                        MergedFieldData mergedFieldData = mergedFields[matchingMergedFieldIndex];
                        mergedFieldData.FieldNameForStructName.Add(structData.StructName, fieldData.FieldName);

                        fieldData.MergedFieldName = mergedFieldData.FieldName;
                        usedIndexesInMergedFields.Add(matchingMergedFieldIndex);
                    }
                }
            }

            return mergedFields;
        }

        private static List<IndividialStructData> BuildIndividualStructsData(GeneratorExecutionContext context, PolymorphicStructSyntaxReceiver systemReceiver, StructDef structDef, InterfaceDeclarationSyntax polymorphicInterface)
        {
            List<IndividialStructData> structDatas = new List<IndividialStructData>();

            foreach (StructDeclarationSyntax individualStruct in systemReceiver.AllStructs)
            {
                if(SourceGenUtils.ImplementsInterface(individualStruct, polymorphicInterface.Identifier.Text))
                {
                    if (individualStruct.Identifier.Text.Equals(structDef.MergedStructName))
                        continue; // skip partial struct, that is extending generated one

                    IndividialStructData structData = new IndividialStructData();
                    structData.Namespace = SourceGenUtils.GetNamespace(individualStruct);
                    structData.StructName = individualStruct.Identifier.ToString();
                     
                    // Add usings
                    foreach (var u in individualStruct.SyntaxTree.GetCompilationUnitRoot(context.CancellationToken).Usings)
                    {
                        TryAddUniqueUsing(structDef.UsingDirectives, u.Name.ToString());
                    }

                    var semanticModel = context.Compilation.GetSemanticModel(individualStruct.SyntaxTree);
                    var structSymbol = semanticModel.GetDeclaredSymbol(individualStruct, context.CancellationToken);
                    var allFields = structSymbol?.GetMembers().Where(it => it.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();

                    if (allFields != null)
                    {
                        foreach (var field in allFields)
                        {
                            structData.Fields.Add(new StructFieldData
                            {
                                TypeName = field.Type.Name,
                                FieldName = MapFieldNameToProperty(field),
                            });
                        }
                    }

                    structDatas.Add(structData);
                }
            }    

            return structDatas;
        }

        private static string MapFieldNameToProperty(IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.AssociatedSymbol is IPropertySymbol propertySymbol)
                return propertySymbol.Name;

            return fieldSymbol.Name;
        }

        private static void TryAddUniqueUsing(List<string> allUsings, string newUsing)
        {
            if (!allUsings.Contains(newUsing))
            {
                allUsings.Add(newUsing);
            }
        }

        private static void GeneratePartialIndividualStruct(
            GeneratorExecutionContext context,
            List<string> allUsings,
            string mergedStructName,
            IndividialStructData structData,
            List<MergedFieldData> mergedFields)
        {
            FileWriter individualStructWriter = new FileWriter();

            // Generate usings
            {
                foreach (string u in allUsings)
                {
                    individualStructWriter.WriteLine($"using {u};");
                }
            }

            individualStructWriter.WriteLine("");

            // Open namespace
            if (!string.IsNullOrEmpty(structData.Namespace))
            {
                individualStructWriter.WriteLine($"namespace {structData.Namespace}");
                individualStructWriter.BeginScope();
            }

            {
                // Generate struct declaration
                individualStructWriter.WriteLine($"public partial struct {structData.StructName}");
                individualStructWriter.BeginScope();
                {
                    // From merged constructor
                    {
                        individualStructWriter.WriteLine($"public {structData.StructName}({mergedStructName} s)");
                        individualStructWriter.BeginScope();
                        {
                            foreach (StructFieldData field in structData.Fields)
                            {
                                individualStructWriter.WriteLine($"{field.FieldName} = s.{field.MergedFieldName};");
                            }
                        }
                        individualStructWriter.EndScope();
                    }

                    individualStructWriter.WriteLine("");

                    // To merged
                    {
                        individualStructWriter.WriteLine($"public {mergedStructName} To{mergedStructName}()");
                        individualStructWriter.BeginScope();
                        {
                            individualStructWriter.WriteLine($"return new {mergedStructName}");
                            individualStructWriter.BeginScope();
                            {
                                individualStructWriter.WriteLine(
                                    $"{typeEnumVarName} = {mergedStructName}.{typeEnumName}.{structData.StructName},");
                                foreach (MergedFieldData mergedField in mergedFields)
                                {
                                    if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
                                    {
                                        individualStructWriter.WriteLine(
                                            $"{mergedField.FieldName} = {mergedField.FieldNameForStructName[structData.StructName]},");
                                    }
                                }
                            }
                            individualStructWriter.EndScope(";");
                        }
                        individualStructWriter.EndScope();
                    }

                    individualStructWriter.WriteLine("");

                    // To merged (by ref)
                    {
                        individualStructWriter.WriteLine($"public void To{mergedStructName}(ref {mergedStructName} s)");
                        individualStructWriter.BeginScope();
                        {
                            individualStructWriter.WriteLine(
                                $"s.{typeEnumVarName} = {mergedStructName}.{typeEnumName}.{structData.StructName};");
                            foreach (MergedFieldData mergedField in mergedFields)
                            {
                                if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
                                {
                                    individualStructWriter.WriteLine(
                                        $"s.{mergedField.FieldName} = {mergedField.FieldNameForStructName[structData.StructName]};");
                                }
                            }
                        }
                        individualStructWriter.EndScope();
                    }
                }
                individualStructWriter.EndScope();
            }

            // Close namespace
            if (!string.IsNullOrEmpty(structData.Namespace))
            {
                individualStructWriter.EndScope();
            }

            System.Console.WriteLine($"Generating IndividualStruct {structData.StructName}");

            context.AddSource(structData.StructName, SourceText.From(individualStructWriter.FileContents, Encoding.UTF8));
        }
    }
}

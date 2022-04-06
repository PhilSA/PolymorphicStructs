using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

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

        public void Execute(GeneratorExecutionContext context)
        {
            // Find all polymorphic struct interfaces, based on attribute
            IEnumerable<InterfaceDeclarationSyntax> polymorphicInterfaces = FindAllInterfacesWithAttribute(context, "PolymorphicStruct");

            // For each polymorphic struct interface, generate a polymorphic struct
            foreach (InterfaceDeclarationSyntax polymorphicInterface in polymorphicInterfaces)
            {
                // Get interface namespace
                string interfaceNamespace = GetNamespace(polymorphicInterface);

                // Build a list of all usings
                List<string> allUsings = new List<string>();
                {
                    TryAddUniqueUsing(allUsings, "System");
                    if (!string.IsNullOrEmpty(interfaceNamespace))
                    {
                        TryAddUniqueUsing(allUsings, interfaceNamespace);
                    }
                    foreach (var u in polymorphicInterface.SyntaxTree.GetCompilationUnitRoot().Usings)
                    {
                        TryAddUniqueUsing(allUsings, u.Name.ToString());
                    }
                }

                // Build list of interface methods
                IEnumerable<MethodDeclarationSyntax> methods = GetAllMethodsOfInterface(polymorphicInterface);

                // Build list of all structs implementing this interface, as well as the fields for each of them
                List<IndividialStructData> structDatas = BuildIndividualStructsData(context, allUsings, polymorphicInterface);

                if (structDatas.Count == 0)
                    continue;

                // Build list of merged fields across all structs
                List<MergedFieldData> mergedFields = BuildMergedFields(structDatas);

                FileWriter mergedStructWriter = new FileWriter();
                string mergedStructName = polymorphicInterface.Identifier.ToString().Substring(1); // TODO; must find better

                // Generate usings
                {
                    foreach (string u in allUsings)
                    {
                        mergedStructWriter.WriteLine("using " + u + ";");
                    }
                }

                mergedStructWriter.WriteLine("");

                // Open namespace
                if (!string.IsNullOrEmpty(interfaceNamespace))
                {
                    mergedStructWriter.WriteLine("namespace " + interfaceNamespace);
                    mergedStructWriter.BeginScope();
                }

                {
                    // Generate struct declaration
                    mergedStructWriter.WriteLine("[Serializable]");
                    mergedStructWriter.WriteLine("public partial struct " + mergedStructName); // TODO: how to define custom interfaces it can implement
                    mergedStructWriter.BeginScope();
                    {
                        // Generate types enum
                        {
                            mergedStructWriter.WriteLine("public enum " + typeEnumName);
                            mergedStructWriter.BeginScope();
                            {
                                foreach (IndividialStructData structData in structDatas)
                                {
                                    mergedStructWriter.WriteLine(structData.StructName + ",");
                                }
                            }
                            mergedStructWriter.EndScope();
                        }

                        mergedStructWriter.WriteLine("");

                        // Generate fields
                        {
                            mergedStructWriter.WriteLine("public " + typeEnumName + " " + typeEnumVarName + ";");

                            for (int i = 0; i < mergedFields.Count; i++)
                            {
                                MergedFieldData mergedField = mergedFields[i];
                                mergedStructWriter.WriteLine("public " + mergedField.TypeName + " " + mergedField.FieldName + ";");
                            }
                        }

                        mergedStructWriter.WriteLine("");

                        // Generate polymorphic methods
                        {
                            foreach (MethodDeclarationSyntax method in methods)
                            {
                                // Generate parameters
                                string parametersString = "";
                                string parametersStringWithoutType = "";
                                for (int i = 0; i < method.ParameterList.Parameters.Count; i++)
                                {
                                    ParameterSyntax param = method.ParameterList.Parameters[i];

                                    if(param.Modifiers.Count > 0)
                                    {
                                        parametersString += param.Modifiers.ToString() + " ";
                                        parametersStringWithoutType += param.Modifiers.ToString() + " ";
                                    }

                                    parametersString += param.Type.ToString() + " " + param.Identifier.ToString();
                                    parametersStringWithoutType += param.Identifier.ToString();

                                    if (i < method.ParameterList.Parameters.Count - 1)
                                    {
                                        parametersString += ", ";
                                        parametersStringWithoutType += ", ";
                                    }
                                }

                                // Method
                                mergedStructWriter.WriteLine("public " + method.ReturnType.ToString() + " " + method.Identifier.ToString() + "(" + parametersString + ")");
                                mergedStructWriter.BeginScope();
                                {
                                    // For each individual struct, call the method
                                    mergedStructWriter.WriteLine("switch(" + typeEnumVarName + ")");
                                    mergedStructWriter.BeginScope();
                                    {
                                        foreach (IndividialStructData structData in structDatas)
                                        {
                                            mergedStructWriter.WriteLine("case " + typeEnumName + "." + structData.StructName + ":");
                                            mergedStructWriter.BeginScope();
                                            {
                                                string structVarName = "instance_" + structData.StructName;

                                                mergedStructWriter.WriteLine(structData.StructName + " " + structVarName + " = new " + structData.StructName + "(this);");
                                                mergedStructWriter.WriteLine(structVarName + "." + method.Identifier.ToString() + "(" + parametersStringWithoutType + ");");
                                                mergedStructWriter.WriteLine(structVarName + ".To" + mergedStructName + "(ref this);");
                                                mergedStructWriter.WriteLine("break;");
                                            }
                                            mergedStructWriter.EndScope();
                                        }
                                    }
                                    mergedStructWriter.EndScope();
                                }
                                mergedStructWriter.EndScope();
                            }
                        }
                    }
                    mergedStructWriter.EndScope();
                }

                // Close namespace
                if (!string.IsNullOrEmpty(interfaceNamespace))
                {
                    mergedStructWriter.EndScope();
                }

                context.AddSource(mergedStructName, SourceText.From(mergedStructWriter.FileContents, Encoding.UTF8));

                // For each individual struct, generate From/To converter methods
                foreach (IndividialStructData structData in structDatas)
                {
                    GeneratePartialIndividualStruct(context, allUsings, mergedStructName, structData, mergedFields);
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context) { }

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
                        mergedFieldData.FieldName = fieldData.TypeName + "_" + mergedFields.Count;
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

        private static List<IndividialStructData> BuildIndividualStructsData(GeneratorExecutionContext context, List<string> allUsings, InterfaceDeclarationSyntax polymorphicInterface)
        {
            List<IndividialStructData> structDatas = new List<IndividialStructData>();

            IEnumerable<StructDeclarationSyntax> individualStructs = FindAllStructsImplementingInterface(context, polymorphicInterface.Identifier.ToString());
            foreach (StructDeclarationSyntax individualStruct in individualStructs)
            {
                IndividialStructData structData = new IndividialStructData();
                structData.Namespace = GetNamespace(individualStruct);
                structData.StructName = individualStruct.Identifier.ToString();

                // Add usings
                foreach (var u in individualStruct.SyntaxTree.GetCompilationUnitRoot().Usings)
                {
                    TryAddUniqueUsing(allUsings, u.Name.ToString());
                }

                // Add fields
                IEnumerable<FieldDeclarationSyntax> fields = GetAllFieldsOfStruct(individualStruct);
                foreach (FieldDeclarationSyntax field in fields)
                {
                    structData.Fields.Add(new StructFieldData
                    {
                        TypeName = field.Declaration.Type.ToString(),
                        FieldName = field.Declaration.Variables.ToString(),
                    });
                }

                structDatas.Add(structData);
            }

            return structDatas;
        }

        private static void TryAddUniqueUsing(List<string> allUsings, string newUsing)
        {
            if(!allUsings.Contains(newUsing))
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
                    individualStructWriter.WriteLine("using " + u + ";");
                }
            }

            individualStructWriter.WriteLine("");

            // Open namespace
            if (!string.IsNullOrEmpty(structData.Namespace))
            {
                individualStructWriter.WriteLine("namespace " + structData.Namespace);
                individualStructWriter.BeginScope();
            }

            {
                // Generate struct declaration
                individualStructWriter.WriteLine("public partial struct " + structData.StructName);
                individualStructWriter.BeginScope();
                {
                    // From merged constructor
                    {
                        individualStructWriter.WriteLine("public " + structData.StructName + "(" + mergedStructName + " s)");
                        individualStructWriter.BeginScope();
                        {
                            foreach (StructFieldData field in structData.Fields)
                            {
                                individualStructWriter.WriteLine(field.FieldName + " = s." + field.MergedFieldName + ";");
                            }
                        }
                        individualStructWriter.EndScope();
                    }

                    individualStructWriter.WriteLine("");

                    // To merged
                    {
                        individualStructWriter.WriteLine("public " + mergedStructName + " To" + mergedStructName + "()");
                        individualStructWriter.BeginScope();
                        {
                            individualStructWriter.WriteLine("return new " + mergedStructName);
                            individualStructWriter.BeginScope();
                            {
                                individualStructWriter.WriteLine(typeEnumVarName + " = " + mergedStructName + "." + typeEnumName + "." + structData.StructName + ",");
                                foreach (MergedFieldData mergedField in mergedFields)
                                {
                                    if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
                                    {
                                        individualStructWriter.WriteLine(mergedField.FieldName + " = " + mergedField.FieldNameForStructName[structData.StructName] + ",");
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
                        individualStructWriter.WriteLine("public void To" + mergedStructName + "(ref " + mergedStructName + " s)");
                        individualStructWriter.BeginScope();
                        {
                            individualStructWriter.WriteLine("s." + typeEnumVarName + " = " + mergedStructName + "." + typeEnumName + "." + structData.StructName + ";");
                            foreach (MergedFieldData mergedField in mergedFields)
                            {
                                if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
                                {
                                    individualStructWriter.WriteLine("s." + mergedField.FieldName + " = " + mergedField.FieldNameForStructName[structData.StructName] + ";");
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

            context.AddSource(structData.StructName, SourceText.From(individualStructWriter.FileContents, Encoding.UTF8));
        }

        private static IEnumerable<InterfaceDeclarationSyntax> FindAllInterfacesWithAttribute(GeneratorExecutionContext context, string attributeName)
        {
            IEnumerable <InterfaceDeclarationSyntax> interfacesWithAttribute = context.Compilation.SyntaxTrees
                .SelectMany(st => st.GetRoot()
                        .DescendantNodes()
                        .Where(n => n is InterfaceDeclarationSyntax)
                        .Select(n => n as InterfaceDeclarationSyntax)
                        .Where(r => r.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.GetText().ToString() == attributeName)));

            return interfacesWithAttribute;
        }

        private static IEnumerable<StructDeclarationSyntax> FindAllStructsImplementingInterface(GeneratorExecutionContext context, string interfaceName)
        {
            IEnumerable<StructDeclarationSyntax> structsWithInterface = context.Compilation.SyntaxTrees
                .SelectMany(st => st.GetRoot()
                        .DescendantNodes()
                        .Where(n => n is StructDeclarationSyntax)
                        .Select(n => n as StructDeclarationSyntax)
                        .Where(r => r.BaseList.Types.Any(t => t.ToString() == interfaceName)));

            return structsWithInterface;
        }

        private static IEnumerable<MethodDeclarationSyntax> GetAllMethodsOfInterface(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            IEnumerable<MethodDeclarationSyntax> methods = interfaceDeclaration.Members
                .Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>();

            return methods;
        }

        private static IEnumerable<FieldDeclarationSyntax> GetAllFieldsOfStruct(StructDeclarationSyntax structDeclaration)
        {
            IEnumerable<FieldDeclarationSyntax> fields = structDeclaration.Members
                .Where(m => m.IsKind(SyntaxKind.FieldDeclaration)).OfType<FieldDeclarationSyntax>();

            return fields;
        }

        private static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            string nameSpace = string.Empty;
            SyntaxNode potentialNamespaceParent = syntax.Parent;

            while (potentialNamespaceParent != null && !(potentialNamespaceParent is NamespaceDeclarationSyntax))
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            if (potentialNamespaceParent != null && potentialNamespaceParent is NamespaceDeclarationSyntax namespaceParent)
            {
                nameSpace = namespaceParent.Name.ToString();
            }

            return nameSpace;
        }
    }
}
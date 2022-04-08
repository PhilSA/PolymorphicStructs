using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PolymorphicStructsSourceGenerators
{
    public static class SourceGenUtils
    {
        public static bool HasAttribute(BaseTypeDeclarationSyntax typeSyntax, string attributeName)
        {
            if (typeSyntax.AttributeLists != null)
            {
                foreach (AttributeListSyntax attributeList in typeSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        if (attribute.Name.ToString() == attributeName)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool ImplementsInterface(BaseTypeDeclarationSyntax typeSyntax, string interfaceName)
        {
            if (typeSyntax.BaseList != null)
            {
                foreach (BaseTypeSyntax type in typeSyntax.BaseList.Types)
                {
                    if (type.ToString() == interfaceName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool ImplementsAnyInterface(ITypeSymbol typeSymbol)
        {
            if(typeSymbol.AllInterfaces.Length > 0)
            {
                return true;
            }
            return false;
        }

        public static IEnumerable<MethodDeclarationSyntax> GetAllMethodsOfInterface(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            IEnumerable<MethodDeclarationSyntax> methods = interfaceDeclaration.Members
                .Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>();

            return methods;
        }

        public static IEnumerable<FieldDeclarationSyntax> GetAllFieldsOfStruct(StructDeclarationSyntax structDeclaration)
        {
            IEnumerable<FieldDeclarationSyntax> fields = structDeclaration.Members
                .Where(m => m.IsKind(SyntaxKind.FieldDeclaration)).OfType<FieldDeclarationSyntax>();

            return fields;
        }

        public static string GetNamespace(BaseTypeDeclarationSyntax syntax)
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

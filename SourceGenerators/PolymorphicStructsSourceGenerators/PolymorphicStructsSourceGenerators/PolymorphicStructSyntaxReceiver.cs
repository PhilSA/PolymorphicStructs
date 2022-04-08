using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace PolymorphicStructsSourceGenerators
{
    internal class PolymorphicStructSyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> PolymorphicInterfaces = new List<InterfaceDeclarationSyntax>();
        public List<StructDeclarationSyntax> AllStructs = new List<StructDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if(syntaxNode is InterfaceDeclarationSyntax interfaceNode)
            {
                if (SourceGenUtils.HasAttribute(interfaceNode, "PolymorphicStruct"))
                {
                    PolymorphicInterfaces.Add(interfaceNode);
                }
            }
            else if (syntaxNode is StructDeclarationSyntax structNode)
            {
                AllStructs.Add(structNode);
            }
        }
    }
}

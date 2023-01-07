﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PX.Generators.Common;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal class BqlFieldsCodeGenerator
    {
        public static readonly string Indentation = "\t";
        public static BqlFieldsCodeGenerator Instance { get; } = new();

        public (string? FileName, SourceText? source) Compile(BqlTableInfo bqlTable)
        {
            if (bqlTable.Fields?.Count is not > 0)
            {
                Debugger.Break();
                return default;
            }

            var compilation = CompilationUnit()
               .AddMembers(GetNamespaceOrClass()
                              .WithLeadingTrivia(GetComment()));

            compilation = compilation.NormalizeWhitespace(Indentation);

            return (
                $"{bqlTable.FullName}.bqlfields.g.cs",
                SourceText.From(compilation.ToFullString(), Encoding.UTF8)
            );

            SyntaxTrivia GetComment()
            {
                return Comment("// <auto-generated/>");
            }

            MemberDeclarationSyntax GetNamespaceOrClass()
            {
                if (string.IsNullOrEmpty(bqlTable.Namespace))
                    return GetClass();

                return NamespaceDeclaration(IdentifierName(bqlTable.Namespace!))
                   .AddMembers(GetClass());
            }

            ClassDeclarationSyntax GetClass()
            {
                return ClassDeclaration(bqlTable.Name)
                      .AddModifiers(Token(SyntaxKind.PartialKeyword))
                      .WithMembers(
                           List<MemberDeclarationSyntax>(
                               bqlTable.Fields
                                       .Select(f =>
                                        {
                                            if (f.ClassType == null)
                                                return null;
                                            return ClassDeclaration(f.ClassName)
                                                  .AddModifiers(
                                                       Token(SyntaxKind.PublicKeyword),
                                                       Token(SyntaxKind.AbstractKeyword))
                                                  .AddBaseListTypes(
                                                       SimpleBaseType(
                                                           ParseTypeName(
                                                               $"PX.Data.BQL.{f.ClassType}.Field<{f.ClassName}>")));
                                            // todo: add new keyword
                                        })
                                       .Where(f => f != null)!));
            }
        }
    }
}
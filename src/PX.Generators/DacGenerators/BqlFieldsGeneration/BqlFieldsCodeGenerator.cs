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


        public (string? FileName, string? Code) Generate_(BqlTableInfo bqlTable)
        {
            if (bqlTable.Fields?.Count is not > 0)
                return default;

            bool fieldAdded = false;
            var  cb         = new CodeBuilder(Indentation);
            using (string.IsNullOrEmpty(bqlTable.Namespace)
                       ? cb.EmptyIndentation
                       : cb.AppendLineWithBrackets($"namespace {bqlTable.Namespace}"))
            {
                using (cb.AppendLineWithBrackets($"partial class {bqlTable.Name}"))
                {
                    foreach (var bqlField in bqlTable.Fields)
                    {
                        if (BqlFieldClassType.TryMap(bqlField.Type, out var className) is false)
                            continue;

                        string addNew = bqlField.IsHidingBaseClass ? "new" : string.Empty;
                        cb.AppendLine(
                            $"public static {addNew} class : PX.Data.BQL.{className}.Field<{className}> {{ }}");
                        fieldAdded = true;
                    }
                }

                cb.Append("ha1");
                cb.AppendLine("ha2");
            }

            cb.Append("ha3");
            cb.AppendLine("ha4");

            if (fieldAdded is false)
                return default;

            return ($"{bqlTable.FullName}.g.cs", cb.ToString());
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Collections.Immutable;
using Bicep.Core.Navigation;
using Bicep.Core.Parsing;

namespace Bicep.Core.Syntax
{
    public class OutputDeclarationSyntax : SyntaxBase, INamedDeclarationSyntax
    {
        public OutputDeclarationSyntax(Token keyword, IdentifierSyntax name, SyntaxBase type, SyntaxBase assignment, SyntaxBase value)
        {
            AssertKeyword(keyword, nameof(keyword), LanguageConstants.OutputKeyword);
            AssertSyntaxType(name, nameof(name), typeof(IdentifierSyntax), typeof(IdentifierSyntax));
            AssertSyntaxType(type, nameof(type), typeof(TypeSyntax), typeof(SkippedTriviaSyntax));
            AssertSyntaxType(assignment, nameof(assignment), typeof(Token), typeof(SkippedTriviaSyntax));
            AssertTokenType(assignment as Token, nameof(assignment), TokenType.Assignment);

            this.LeadingNodes = ImmutableArray<SyntaxBase>.Empty;
            this.Keyword = keyword;
            this.Name = name;
            this.Type = type;
            this.Assignment = assignment;
            this.Value = value;
        }

        public OutputDeclarationSyntax(IEnumerable<SyntaxBase> leadingNodes, Token keyword, IdentifierSyntax name, SyntaxBase type, SyntaxBase assignment, SyntaxBase value)
        {
            AssertKeyword(keyword, nameof(keyword), LanguageConstants.OutputKeyword);
            AssertSyntaxType(name, nameof(name), typeof(IdentifierSyntax), typeof(IdentifierSyntax));
            AssertSyntaxType(type, nameof(type), typeof(TypeSyntax), typeof(SkippedTriviaSyntax));
            AssertSyntaxType(assignment, nameof(assignment), typeof(Token), typeof(SkippedTriviaSyntax));
            AssertTokenType(assignment as Token, nameof(assignment), TokenType.Assignment);

            this.LeadingNodes = leadingNodes.ToImmutableArray();
            this.Keyword = keyword;
            this.Name = name;
            this.Type = type;
            this.Assignment = assignment;
            this.Value = value;
        }

        public ImmutableArray<SyntaxBase> LeadingNodes { get; }

        public Token Keyword { get; }

        public IdentifierSyntax Name { get; }

        public SyntaxBase Type { get; }

        public SyntaxBase Assignment { get; }

        public SyntaxBase Value { get; }

        public IEnumerable<DecoratorSyntax> Decorators => this.LeadingNodes.OfType<DecoratorSyntax>();

        public override void Accept(ISyntaxVisitor visitor) => visitor.VisitOutputDeclarationSyntax(this);

        public override TextSpan Span => TextSpan.Between(Keyword, Value);

        public TypeSyntax? OutputType => this.Type as TypeSyntax;
    }
}

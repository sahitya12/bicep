// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Bicep.Core.Diagnostics;
using Bicep.Core.Navigation;
using Bicep.Core.Parsing;
using Bicep.Core.Resources;
using Bicep.Core.Semantics;
using Bicep.Core.TypeSystem;

namespace Bicep.Core.Syntax
{
    public class ResourceDeclarationSyntax : SyntaxBase, INamedDeclarationSyntax
    {
        public ResourceDeclarationSyntax(Token keyword, IdentifierSyntax name, SyntaxBase type, Token? existingKeyword, SyntaxBase assignment, SyntaxBase? ifCondition, SyntaxBase body)
        {
            AssertKeyword(keyword, nameof(keyword), LanguageConstants.ResourceKeyword);
            AssertSyntaxType(name, nameof(name), typeof(IdentifierSyntax));
            AssertSyntaxType(type, nameof(type), typeof(StringSyntax), typeof(SkippedTriviaSyntax));
            AssertKeyword(existingKeyword, nameof(existingKeyword), LanguageConstants.ExistingKeyword);
            AssertTokenType(keyword, nameof(keyword), TokenType.Identifier);
            AssertSyntaxType(assignment, nameof(assignment), typeof(Token), typeof(SkippedTriviaSyntax));
            AssertTokenType(assignment as Token, nameof(assignment), TokenType.Assignment);
            AssertSyntaxType(ifCondition, nameof(ifCondition), typeof(SkippedTriviaSyntax), typeof(IfConditionSyntax));
            AssertSyntaxType(body, nameof(body), typeof(SkippedTriviaSyntax), typeof(ObjectSyntax));

            this.Keyword = keyword;
            this.Name = name;
            this.Type = type;
            this.ExistingKeyword = existingKeyword;
            this.Assignment = assignment;
            this.IfCondition = ifCondition;
            this.Body = body;
        }

        public Token Keyword { get; }

        public IdentifierSyntax Name { get; }

        public SyntaxBase Type { get; }

        public Token? ExistingKeyword { get; }

        public SyntaxBase Assignment { get; }

        public SyntaxBase? IfCondition { get; }

        public SyntaxBase Body { get; }

        public override void Accept(ISyntaxVisitor visitor) => visitor.VisitResourceDeclarationSyntax(this);

        public override TextSpan Span => TextSpan.Between(Keyword, Body);

        public StringSyntax? TypeString => Type as StringSyntax;

        public bool IsExistingResource() => (ExistingKeyword is not null);

        public TypeSymbol GetDeclaredType(ResourceScopeType targetScope, IResourceTypeProvider resourceTypeProvider)
        {
            var stringSyntax = this.TypeString;

            if (stringSyntax != null && stringSyntax.IsInterpolated())
            {
                // TODO: in the future, we can relax this check to allow interpolation with compile-time constants.
                // right now, codegen will still generate a format string however, which will cause problems for the type.
                return ErrorType.Create(DiagnosticBuilder.ForPosition(this.Type).ResourceTypeInterpolationUnsupported());
            }

            var stringContent = stringSyntax?.TryGetLiteralValue();
            if (stringContent == null)
            {
                return ErrorType.Create(DiagnosticBuilder.ForPosition(this.Type).InvalidResourceType());
            }

            var typeReference = ResourceTypeReference.TryParse(stringContent);
            if (typeReference == null)
            {
                return ErrorType.Create(DiagnosticBuilder.ForPosition(this.Type).InvalidResourceType());
            }

            var resourceType = resourceTypeProvider.GetType(targetScope, typeReference);
            if (IsExistingResource())
            {
                resourceType = new ResourceType(resourceType.TypeReference, ConvertToReadOnly(resourceType.Body));
            }

            return resourceType;
        }

        private static ITypeReference ConvertToReadOnly(ITypeReference typeReference)
        {
            switch (typeReference.Type)
            {
                case ObjectType objectType:
                {
                    var updatedProperties = new List<TypeProperty>();
                    foreach (var (key, property) in objectType.Properties)
                    {
                        // TODO: fix this
                        if (key == "name" || key == "scope")
                        {
                            updatedProperties.Add(property);
                        }
                        else
                        {
                            updatedProperties.Add(new TypeProperty(property.Name, property.TypeReference, ConvertToReadOnly(property.Flags)));
                        }
                    }

                    return new NamedObjectType(
                        objectType.Name,
                        objectType.ValidationFlags,
                        updatedProperties,
                        objectType.AdditionalPropertiesType,
                        ConvertToReadOnly(objectType.AdditionalPropertiesFlags));
                }
                case DiscriminatedObjectType discriminatedObjectType:
                {
                    var unionMembers = discriminatedObjectType.UnionMembersByKey.Values.Select(x => ConvertToReadOnly(x));

                    return new DiscriminatedObjectType(
                        discriminatedObjectType.Name,
                        discriminatedObjectType.ValidationFlags,
                        discriminatedObjectType.DiscriminatorKey,
                        unionMembers);
                }
                default:
                {
                    return typeReference;
                }
            }
        }

        private static TypePropertyFlags ConvertToReadOnly(TypePropertyFlags typePropertyFlags)
            => (typePropertyFlags | TypePropertyFlags.ReadOnly) & ~TypePropertyFlags.Required;
    }
}

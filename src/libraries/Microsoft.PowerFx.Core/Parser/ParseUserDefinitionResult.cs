﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class ParseUserDefinitionResult
    {
        internal IEnumerable<UDF> UDFs { get; }

        internal IEnumerable<NamedFormula> NamedFormulas { get; }

        internal IEnumerable<DefinedType> DefinedTypes { get; }

        internal IEnumerable<TexlError> Errors { get; }

        internal IEnumerable<CommentToken> Comments { get; }

        // This is used to preserve the order of user definitions and all their source trivia (like comments), to be used by Pretty Print and other similar operations.
        internal IEnumerable<UserDefinitionSourceInfo> UserDefinitionSourceInfos { get; }

        internal bool HasErrors { get; }

        public ParseUserDefinitionResult(IEnumerable<NamedFormula> namedFormulas, IEnumerable<UDF> uDFs, IEnumerable<DefinedType> definedTypes, IEnumerable<TexlError> errors, IEnumerable<CommentToken> comments, IEnumerable<UserDefinitionSourceInfo> userDefinitionSourceInfos)
        {
            NamedFormulas = namedFormulas;
            UDFs = uDFs;
            DefinedTypes = definedTypes;
            Comments = comments;
            UserDefinitionSourceInfos = userDefinitionSourceInfos;

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasErrors = true;
            }
        }
    }
}

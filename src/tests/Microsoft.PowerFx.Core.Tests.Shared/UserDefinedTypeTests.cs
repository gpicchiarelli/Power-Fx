﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class UserDefinedTypeTests : PowerFxTest
    {
        private static readonly ReadOnlySymbolTable _primitiveTypes = ReadOnlySymbolTable.PrimitiveTypesTableInstance;

        [Theory]

        // Check record, table types with primitive types
        [InlineData("Point := Type({ x: Number, y: Number })", "![x:n,y:n]", true)]
        [InlineData("Points := Type([{ x: Number, y: Number }])", "*[x:n,y:n]", true)]
        [InlineData("Person := Type({ name: Text, dob: Date })", "![name:s,dob:D]", true)]
        [InlineData("People := Type([{ name: Text, isReady: Boolean }])", "*[name:s,isReady:b]", true)]
        [InlineData("Heights := Type([Number])", "*[Value:n]", true)]
        [InlineData("Palette := Type([Color])", "*[Value:c]", true)]

        // Type alias
        [InlineData("DTNZ := Type(DateTimeTZInd)", "Z", true)]
        [InlineData("MyVoid := Type(Void)", "-", true)]

        // Nested record types
        [InlineData("Nested := Type({a: {b: DateTime, c: {d: GUID, e: Hyperlink}}, x: Time})", "![a:![b:d, c:![d:g, e:h]], x:T]", true)]

        // Invalid types
        [InlineData("Pics := Type([Image])", "*[Value:i]", false)]
        [InlineData("A := Type(B)", "", false)]
        [InlineData("A := Type([])", "", false)]
        [InlineData("A := Type({})", "", false)]
        public void TestUserDefinedType(string typeDefinition, string expectedDefinedTypeString, bool isValid)
        {
            var checkResult = new DefinitionsCheckResult()
                                            .SetText(typeDefinition)
                                            .SetBindingInfo(_primitiveTypes);
            checkResult.ApplyResolveTypes();

            if (isValid)
            {
                Assert.NotEmpty(checkResult.ResolvedTypes);
                var resultType = checkResult.ResolvedTypes.First().Value._type;
                Assert.Equal(TestUtils.DT(expectedDefinedTypeString), resultType);
            }
            else
            {
                Assert.Empty(checkResult.ResolvedTypes);
                Assert.NotEmpty(checkResult.Errors);
            }
        }

        [Theory]
        [InlineData("X = 5; Point := Type({ x: Number, y: Number })", 1)]
        [InlineData("Point := Type({ x: Number, y: Number }); Points := Type([Point])", 2)]

        // Mix named formula with named type
        [InlineData("X = 5; Point := Type({ x: X, y: Number }); Points := Type([Point])", 0)]

        // Have invalid type expression
        [InlineData("WrongType := Type(5+5); WrongTypes := Type([WrongType]); People := Type([{ name: Text, age: Number }])", 1)]

        // Have incomplete expressions and parse errors
        [InlineData("Point := Type({a:); Points = Type([Point]); People := Type([{ name: Text, age })", 0)]
        [InlineData("Point := Type({a:; Points = Type([Point]); People := Type([{ name: Text, age: Number })", 1)]

        // Redeclare type
        [InlineData("Point := Type({ x: Number, y: Number }); Point := Type(Number);", 1)]

        // Redeclare typed name in record
        [InlineData("X:= Type({ f:Number, f:Number});", 0)]

        // Cyclic definition
        [InlineData("B := Type({ x: A }); A := Type(B);", 0)]
        [InlineData("B := Type(B);", 0)]

        // Complex resolutions
        [InlineData("C := Type({x: Boolean, y: Date, f: B});B := Type({ x: A }); A := Type(Number);", 3)]
        [InlineData("D := Type({nArray: [Number]}), C := Type({x: Boolean, y: Date, f: B});B := Type({ x: A }); A := Type([C]);", 1)]

        // With Invalid types
        [InlineData("A := Type(Blob); B := Type({x: Currency}); C := Type([DateTime]); D := Type(None)", 2)]

        // Have named formulas and udf in the script
        [InlineData("NAlias := Type(Number);X := 5; ADDX(n:Number): Number = n + X; SomeType := Type(Dynamic)", 2)]

        // Have RecordOf with/ without errors
        [InlineData("Numbers := Type([Number]);T1 := Type(RecordOf([Number])); Num := Type(RecordOf(Numbers)); T2 := Type(Num);", 3)]

        // Cannot do RecordOf on a Record type
        [InlineData("Point := Type({x: Number, y: Number});PointR := Type(RecordOf(Point));", 1)]

        // Cannot use RecordOf outside of type literal
        [InlineData("Point := Type({x: Number, y: Number});PointR = RecordOf(Point);", 1)]
        public void TestValidUDTCounts(string typeDefinition, int expectedDefinedTypesCount)
        {
            var checkResult = new DefinitionsCheckResult()
                                            .SetText(typeDefinition)
                                            .SetBindingInfo(_primitiveTypes);
            checkResult.ApplyResolveTypes();

            var resolvedTypes = checkResult.ResolvedTypes;

            Assert.Equal(expectedDefinedTypesCount, resolvedTypes.Count());
        }

        [Theory]

        //To test DefinitionsCheckResult.ApplyErrors method and error messages
        [InlineData("Point := Type({ x: Number, y: Number }); Point := Type(Number);", 1, "ErrNamedType_TypeAlreadyDefined")]
        [InlineData("X:= Type({ f:Number, f:Number});", 1, "ErrNamedType_InvalidTypeDeclaration")]
        [InlineData("B := Type({ x: A }); A := Type(B);", 2, "ErrNamedType_InvalidCycles")]
        [InlineData("B := Type(B);", 1, "ErrNamedType_InvalidCycles")]
        [InlineData("Currency := Type({x: Text}); Record := Type([DateTime]); D := Type(None);", 2, "ErrNamedType_InvalidTypeName")]
        [InlineData("A = 5;C :=; B := Type(Number);", 1, "ErrNamedType_MissingTypeExpression")]
        [InlineData("C := 5; D := [1,2,3];", 2, "ErrNamedType_MissingTypeExpression")]
        [InlineData("T := Type([{a: {b: Void}}]);", 1, "ErrNamedType_InvalidTypeDeclaration")]
        [InlineData("T := Type([Void]);", 1, "ErrNamedType_InvalidTypeDeclaration")]
        [InlineData("T := Type({a: Void});", 1, "ErrNamedType_InvalidTypeDeclaration")]
        public void TestUDTErrors(string typeDefinition, int expectedErrorCount, string expectedMessageKey)
        {
            var checkResult = new DefinitionsCheckResult()
                                            .SetText(typeDefinition)
                                            .SetBindingInfo(_primitiveTypes);
            var errors = checkResult.ApplyErrors();

            Assert.Equal(expectedErrorCount, errors.Count());
            Assert.All(errors, e => Assert.Contains(expectedMessageKey, e.MessageKey));
        }

        [Theory]

        // not in Type literal expression;
        [InlineData("Points := Type([{ x: Number, y: Number }]); F(): Points = RecordOf(Points); ", "ErrKnownTypeHelperFunction")]
        [InlineData("Points := Type([{ x: Number, y: Number }]); F(): Number = RecordOf(Points).x; ", "ErrKnownTypeHelperFunction")]

        // RecordOf record type
        [InlineData("Point := Type({ x: Number, y: Number }); PointR := Type(RecordOf(Point)); ", "ErrNamedType_InvalidTypeDeclaration")]

        // Inline definitions within RecordOf
        [InlineData("T1 := Type(RecordOf(Type([{A:Number}])));", "ErrTypeFunction_InvalidTypeExpression")]
        [InlineData("T1 := Type(RecordOf(RecordOf([{x:Number, y:Number}])));", "ErrTypeFunction_InvalidTypeExpression")]
        public void TestRecordOfErrors(string typeDefinition, string expectedMessageKey)
        {
            var checkResult = new DefinitionsCheckResult()
                                            .SetText(typeDefinition)
                                            .SetBindingInfo(_primitiveTypes);
            var errors = checkResult.ApplyErrors();
            Assert.Contains(errors, e => e.MessageKey.Contains(expectedMessageKey));
        }

        [Theory]
        [InlineData("f():T = {x: true, y: 1}; T := Type({x: Boolean});", "ErrUDF_ReturnTypeSchemaAdditionalFields")]
        [InlineData("f(x:T):Number = x.n; T := Type({n: Number}); g(): Number = f({n: 5, m: 5});", "ErrBadSchema_AdditionalField")]
        [InlineData("f():T = [{x: 5, y: 5}]; T := Type([{x: Number}]);", "ErrUDF_ReturnTypeSchemaAdditionalFields")]
        [InlineData("f(x:T):T = x; T := Type([{n: Number}]); g(): T = f([{n: 5, m: 5}]);", "ErrBadSchema_AdditionalField")]
        [InlineData("f(x:T):T = {n: \"Foo\"}; T := Type({n: GUID});", "ErrUDF_ReturnTypeSchemaIncompatible")]
        public void TestAggregateTypeErrors(string typeDefinition, string expectedMessageKey)
        {
            var checkResult = new DefinitionsCheckResult()
                                            .SetText(typeDefinition)
                                            .SetBindingInfo(_primitiveTypes);
            var errors = checkResult.ApplyErrors();
            Assert.Contains(errors, e => e.MessageKey.Contains(expectedMessageKey));
        }

        [Theory]
        [InlineData("T := Type({ x: 5+5, y: -5 });", 2)]
        [InlineData("T := Type(Type(Number));", 1)]
        [InlineData("T := Type({+});", 1)]
        [InlineData("T := Type({);", 1)]
        [InlineData("T := Type({x: true, y: \"Number\"});", 2)]
        [InlineData("T1 := Type({A: Number}); T2 := Type(T1.A);", 1)]
        [InlineData("T := Type((1, 2));", 1)]
        [InlineData("T1 := Type(UniChar(955)); T2 := Type([Table(Number)])", 2)]
        [InlineData("T := Type((1; 2));", 1)]
        [InlineData("T := Type(Self.T);", 1)]
        [InlineData("T := Type(Parent.T);", 1)]
        [InlineData("T := Type(Number As T1);", 1)]
        [InlineData("T := Type(Text); T1 := Type(Not T);", 1)]
        [InlineData("T1 := Type({V: Number}); T2 := Type(T1[@V]);", 1)]
        [InlineData("T := Type([{a: {b: {c: [{d: 10e+4}]}}}]);", 1)]
        public void TestUDTParseErrors(string typeDefinition, int expectedErrorCount)
        {
            var checkResult = new DefinitionsCheckResult()
                                            .SetText(typeDefinition);

            var parseResult = checkResult.ApplyParse();
            Assert.True(parseResult.HasErrors);

            var validatorErrors = parseResult.Errors.Where(e => e.MessageKey.Contains("ErrTypeFunction_InvalidTypeExpression"));
            Assert.Equal(expectedErrorCount, validatorErrors.Count());
        }
    }
}

﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// The backing implementation for UntypedObjectValue, for example Json, Xml,
    /// or the Ast or Value system from another language.
    /// </summary>
    public interface IUntypedObject
    {
        /// <summary>
        /// Use ExternalType if the type is incompatible with PowerFx.
        /// </summary>
        FormulaType Type { get; }

        int GetArrayLength();

        /// <summary>
        /// 0-based index.
        /// </summary>
        IUntypedObject this[int index] { get; }

        bool TryGetProperty(string value, out IUntypedObject result);

        string GetString();

        double GetDouble();

        decimal GetDecimal();

        bool GetBoolean();

        // For providers that do not imply a type for numbers (JSON), GetUntypedNumber is used to access the raw
        // underlying string representation, likely from the source representation (again in the case of JSON).
        // It is validated to be in a culture invariant standard format number, possibly with dot decimal separator and "E" exponent.
        // This method need not be implemetned if ExteranlType.UntypedNumber is not used.
        // GetDouble and GetDecimal can be called on an ExternalType.UntypedNumber.
        string GetUntypedNumber();

        /// <summary>
        /// If the untyped value is an object then this method returns true and the list of available property names in the <paramref name="propertyNames"/> parameter.
        /// Returns false otherwise, <paramref name="propertyNames"/> will be null in this case.
        /// </summary>
        bool TryGetPropertyNames(out IEnumerable<string> propertyNames);
    }

    [DebuggerDisplay("UntypedObjectValue({Impl})")]
    public class UntypedObjectValue : ValidFormulaValue
    {
        public IUntypedObject Impl { get; }

        internal UntypedObjectValue(IRContext irContext, IUntypedObject impl)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.UntypedObject);
            Impl = impl;
        }

        public override object ToObject()
        {
            throw new NotImplementedException();
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            // Not supported for the time being.
            throw new NotImplementedException("UntypedObjectValue cannot be serialized.");
        }
    }

    public abstract class UntypedObjectBase : IUntypedObject
    {
        public abstract IUntypedObject this[int index] { get; }

        public abstract FormulaType Type { get; }

        public abstract int GetArrayLength();

        public abstract bool GetBoolean();

        public abstract decimal GetDecimal();

        public abstract double GetDouble();

        public abstract string GetString();

        public abstract string GetUntypedNumber();

        public abstract bool TryGetProperty(string value, out IUntypedObject result);

        /// <summary>
        /// Get the property value of the untyped object.
        /// Hosts should override this method in case a different return value is needed. For example, a host may want to return a ErrorValue in case of a missing property.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="returnType"></param>        
        /// <returns></returns>
        public virtual FormulaValue GetProperty(string value, FormulaType returnType)
        {
            if (TryGetProperty(value, out var res))
            {
                if (res.Type == FormulaType.Blank)
                {
                    return new BlankValue(IRContext.NotInSource(returnType));
                }

                return new UntypedObjectValue(IRContext.NotInSource(returnType), res);
            }
            else
            {
                return new BlankValue(IRContext.NotInSource(returnType));
            }
        }

        public abstract bool TryGetPropertyNames(out IEnumerable<string> propertyNames);

        /// <summary>
        /// Set a property on the object.
        /// </summary>
        /// <param name="propertyName">Property identifier.</param>
        /// <param name="value">FormulaValue to be set.</param>
        public virtual void SetProperty(string propertyName, FormulaValue value)
        {
            // In case of unwanted behavior, throw an CustomFunctionErrorException exception.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set a property on the object.
        /// </summary>
        /// <param name="index">Property identifier. 0-based.</param>
        /// <param name="value">FormulaValue to be set.</param>
        public virtual void SetIndex(int index, FormulaValue value)
        {
            // In case of unwanted behavior, throw an CustomFunctionErrorException exception.
            throw new NotImplementedException();
        }
    }
}

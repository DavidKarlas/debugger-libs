// 
// PropertyValueReference.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Mono.Debugging.Evaluation;
using Mono.Debugger.Soft;
using Mono.Debugging.Client;

namespace Mono.Debugging.Soft
{
	public class PropertyValueReference: SoftValueReference
	{
		PropertyInfoMirror property;
		object obj;
		TypeMirror declaringType;
		Value[] indexerArgs;
		ObjectValueFlags flags;
		PropertyReferenceBatch batch;
		
		public PropertyValueReference (EvaluationContext ctx, PropertyInfoMirror property, object obj, TypeMirror declaringType, MethodMirror getter, Value[] indexerArgs, PropertyReferenceBatch batch = null): base (ctx)
		{
			this.property = property;
			this.obj = obj;
			this.declaringType = declaringType;
			this.indexerArgs = indexerArgs;
			this.batch = batch;

			var objectMirror = obj as ObjectMirror;
			if (objectMirror != null)
				EnsureContextHasDomain (objectMirror.Domain);

			flags = GetFlags (property, getter);
		}

		internal static ObjectValueFlags GetFlags (PropertyInfoMirror property, MethodMirror getter)
		{
			var flags = ObjectValueFlags.Property;

			if (property.GetSetMethod (true) == null)
				flags |= ObjectValueFlags.ReadOnly;

			if (getter.IsStatic)
				flags |= ObjectValueFlags.Global;

			if (getter.IsPublic)
				flags |= ObjectValueFlags.Public;
			else if (getter.IsPrivate)
				flags |= ObjectValueFlags.Private;
			else if (getter.IsFamily)
				flags |= ObjectValueFlags.Protected;
			else if (getter.IsFamilyAndAssembly)
				flags |= ObjectValueFlags.Internal;
			else if (getter.IsFamilyOrAssembly)
				flags |= ObjectValueFlags.InternalProtected;

			if (property.DeclaringType.IsValueType)
				flags |= ObjectValueFlags.ReadOnly; // Setting property values on structs is not supported by sdb

			return flags;
		}
		
		public override ObjectValueFlags Flags {
			get {
				return flags;
			}
		}

		public override string Name {
			get {
				return property.Name;
			}
		}

		public override object Type {
			get {
				return property.PropertyType;
			}
		}
		
		public override object DeclaringType {
			get {
				return property.DeclaringType;
			}
		}

		public override object Value {
			get {
				Context.AssertTargetInvokeAllowed ();
				return GetValueExplicitly ();
			}
			set {
				Context.AssertTargetInvokeAllowed ();
				var ctx = (SoftEvaluationContext) Context;
				var args = new Value [indexerArgs != null ? indexerArgs.Length + 1 : 1];
				if (indexerArgs != null)
					indexerArgs.CopyTo (args, 0);
				args [args.Length - 1] = (Value) value;
				var setter = property.GetSetMethod (true);
				if (setter == null)
					throw new EvaluatorException ("Property is read-only");
				ctx.RuntimeInvoke (setter, obj ?? declaringType, args);
			}
		}

		protected override object GetValueExplicitly ()
		{
			var ctx = (SoftEvaluationContext) Context;
			if (batch != null)
				return batch.RuntimeInvoke (property, indexerArgs);
			return ctx.RuntimeInvoke (property.GetGetMethod (true), obj ?? declaringType, indexerArgs);
		}

		protected override bool CanEvaluate (EvaluationOptions options)
		{
			return options.AllowTargetInvoke;
		}
	}
}

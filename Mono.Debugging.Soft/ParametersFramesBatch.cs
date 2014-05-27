// 
// ParametersFramesBatch.cs
//  
// Authors: David Karlaš <david.karlas@xamarin.com>
// 
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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

using System;

using Mono.Debugger.Soft;
using System.Collections.Generic;

namespace Mono.Debugging.Soft
{
	public class ParametersFramesBatch
	{
		readonly Dictionary<StackFrame,LocalVariable[]> variables = new Dictionary<StackFrame, LocalVariable[]> ();
		readonly Dictionary<StackFrame,Value[]> results = new Dictionary<StackFrame, Value[]> ();
		readonly SoftEvaluationContext ctx;

		public ParametersFramesBatch(SoftEvaluationContext ctx)
		{
			this.ctx = ctx;
		}

		public void AddFrame (StackFrame frame, LocalVariable[] parametersVariables)
		{
			variables.Add (frame, parametersVariables);
		}
		public Value[] GetValues (StackFrame frame)
		{
			if (results.Count != variables.Count) {
				results.Clear ();
				ctx.Session.VirtualMachine.StartBuffering ();
				foreach (var variable in variables)
					results.Add (variable.Key, frame.GetValues (variable.Value));
				ctx.Session.VirtualMachine.StopBuffering ();
			}
			return results [frame];
		}
	}
}

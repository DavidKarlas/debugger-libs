// 
// PropertyReferenceBatch.cs
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
using Mono.Debugger.Soft;
using System.Collections.Generic;
using System;
using System.Threading;



namespace Mono.Debugging.Soft
{
	public class PropertyReferenceBatch
	{
		readonly object resultsLocker = new object ();
		readonly object obj;
		readonly SoftEvaluationContext ctx;
		List<Tuple<PropertyInfoMirror,Value[]>> propertiesKeys = new List<Tuple<PropertyInfoMirror, Value[]>> ();
		Dictionary<Tuple<PropertyInfoMirror,Value[]>, Value> results;

		public PropertyReferenceBatch (SoftEvaluationContext ctx, object obj)
		{
			this.ctx = ctx;
			this.obj = obj;
		}

		public void Add (PropertyInfoMirror property, Value[] indexerArgs)
		{
			propertiesKeys.Add (new Tuple<PropertyInfoMirror,Value[]> (property, indexerArgs));
		}

		public void Invalidate ()
		{
			lock (resultsLocker) {
				results = null;
			}
		}

		public object RuntimeInvoke (PropertyInfoMirror property, Value[] indexerArgs)
		{
			lock (resultsLocker) {
				if (results == null) {
					results = new Dictionary<Tuple<PropertyInfoMirror, Value[]>, Value> ();
					ManualResetEvent allResultsRecieved = new ManualResetEvent (false);
					ctx.Session.VirtualMachine.StartBuffering ();
					foreach (var propertyKey in propertiesKeys) { 
						ctx.RuntimeInvoke (propertyKey.Item1.GetGetMethod (true), obj ?? propertyKey.Item1.DeclaringType, propertyKey.Item2, (result) => {
							results.Add (propertyKey, result);
							if (propertiesKeys.Count == results.Count)
								allResultsRecieved.Set ();
						});
					}
					ctx.Session.VirtualMachine.StopBuffering ();
					allResultsRecieved.WaitOne ();
				}
				return results [new Tuple<PropertyInfoMirror,Value[]> (property, indexerArgs)];
			}
		}
	}
}

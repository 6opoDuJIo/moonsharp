﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Grammar;
using MoonSharp.Interpreter.Tree.Expressions;

namespace MoonSharp.Interpreter.Tree.Statements
{
	class LocalAssignmentStatement : Statement
	{
		SymbolRef[] m_Names;
		Expression[] m_RValues;

		public LocalAssignmentStatement(LuaParser.Stat_localassignmentContext context, ScriptLoadingContext lcontext)
			: base(context, lcontext)
		{
			m_Names = context.namelist().NAME()
				.Select(n => n.GetText())
				.Select(n => lcontext.Scope.DefineLocal(n))
				.ToArray();

			var explist = context.explist();

			if (explist != null)
			{
				m_RValues = explist
				.exp()
				.Select(e => NodeFactory.CreateExpression(e, lcontext))
				.ToArray();
			}
			else
				m_RValues = new Expression[0];

		}


		public override ExecutionFlow Exec(RuntimeScope scope)
		{
			if (m_Names.Length == 1 && m_RValues.Length >= 1)
			{
				scope.Assign(m_Names[0], m_RValues[0].Eval(scope).ToSimplestValue());
				return ExecutionFlow.None;
			}
			else
			{
				return PairMultipleAssignment(scope, m_Names, m_RValues, (l, s, v) => 
				{
					scope.Assign(l, v);
				});
			}
		}

		public override void Compile(Execution.VM.Chunk bc)
		{
			if (m_Names.Length == 1 && m_RValues.Length == 1)
			{
				bc.Symbol(m_Names[0]);
				m_RValues[0].Compile(bc);
				bc.Store();
			}
			else
			{
				foreach (var var in m_Names)
					bc.Symbol(var);

				foreach (var exp in m_RValues)
					exp.Compile(bc);

				bc.Assign(m_Names.Length, m_RValues.Length);
			}
		}
	}
}
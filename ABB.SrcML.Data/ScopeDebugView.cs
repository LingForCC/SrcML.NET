﻿using System.Diagnostics;
using System.Linq;

namespace ABB.SrcML.Data {

    internal class ScopeDebugView {
        private IScope scope;

        public ScopeDebugView(IScope scope) {
            this.scope = scope;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IScope[] ChildScopes {
            get { return this.scope.ChildScopes.ToArray(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IMethodCall[] MethodCalls { get { return this.scope.MethodCalls.ToArray(); } }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IVariableDeclaration[] Variables { get { return this.scope.DeclaredVariables.ToArray(); } }

        public override string ToString() {
            return scope.ToString();
        }
    }
}
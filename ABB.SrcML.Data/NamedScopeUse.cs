﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {

    /// <summary>
    /// The NamedScopeUse class represents a use of a named scope. It can create a
    /// <see cref="NamedScope"/> based on itself by calling
    /// <see cref="INamedScopeUse.CreateScope()"/>.
    /// </summary>
    [Serializable]
    public class NamedScopeUse : AbstractScopeUse<INamedScope>, INamedScopeUse {

        /// <summary>
        /// The child of this scope
        /// </summary>
        public INamedScopeUse ChildScopeUse { get; set; }

        /// <summary>
        /// Creates a <see cref="NamedScope"/> object from this use (along with all of its
        /// descendants based on <see cref="ChildScopeUse"/>).
        /// </summary>
        /// <returns>A new named scope based on this use</returns>
        public virtual INamedScope CreateScope() {
            INamedScope scope = new NamedScope() {
                Name = this.Name,
                ProgrammingLanguage = this.ProgrammingLanguage,
            };
            scope.AddSourceLocation(this.Location);
            if(null != this.ChildScopeUse) {
                scope.AddChildScope(ChildScopeUse.CreateScope());
            }
            return scope;
        }

        /// <summary>
        /// Find named scopes that match this named scope use.
        /// </summary>
        /// <returns>An enumerable of named scopes with the same name as this use</returns>
        public override IEnumerable<INamedScope> FindMatches() {
            if(ChildScopeUse != null) {
                var globalScope = ParentScope.GetParentScopesAndSelf<INamespaceDefinition>().Where(p => p.IsGlobal).FirstOrDefault();

                if(null == globalScope) {
                    throw new ScopeDetachedException(this.ParentScope);
                }

                INamedScopeUse current = ChildScopeUse;

                IEnumerable<INamedScope> matches = null;
                while(current != null) {
                    if(matches == null) {
                        matches = globalScope.GetChildScopesWithId<INamedScope>(current.Name);
                    } else {
                        matches = GetChildScopesWithName(matches, current.Name);
                    }
                    current = current.ChildScopeUse;
                }
                return (matches == null ? Enumerable.Empty<INamedScope>() : matches);
            } else {
                return base.FindMatches();
            }
        }

        /// <summary>
        /// Constructs the full name for this named scope use by combining this scope with all of
        /// its <see cref="ChildScopeUse">children</see>
        /// </summary>
        /// <returns>The full name</returns>
        public string GetFullName() {
            StringBuilder sb = new StringBuilder();
            INamedScopeUse current = this;
            while(current != null) {
                sb.Append(current.Name);
                current = current.ChildScopeUse;

                if(null != current) {
                    sb.Append('.');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if this scope matches
        /// <paramref name="definition"/></summary>
        /// <param name="definition">The scope to check</param>
        /// <returns>True if this and definition have the same name</returns>
        public override bool Matches(INamedScope definition) {
            if(null == definition)
                return false;
            return definition.Name == this.Name;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() {
            return GetFullName();
        }

        private IEnumerable<INamedScope> GetChildScopesWithName(IEnumerable<INamedScope> scopes, string name) {
            var matches = from scope in scopes
                          from match in scope.GetChildScopesWithId<INamedScope>(name)
                          select match;
            return matches;
        }
    }
}
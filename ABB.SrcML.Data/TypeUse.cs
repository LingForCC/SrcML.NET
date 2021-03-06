﻿/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a use of a type. It is used in declarations and inheritance specifications.
    /// </summary>
    [Serializable]
    public class TypeUse : AbstractScopeUse<ITypeDefinition>, ITypeUse {
        private List<ITypeUse> internalTypeParameters;

        /// <summary>
        /// Create a new type use object.
        /// </summary>
        public TypeUse() {
            this.Name = String.Empty;
            this.internalTypeParameters = new List<ITypeUse>();
            this.TypeParameters = new ReadOnlyCollection<ITypeUse>(this.internalTypeParameters);
        }

        /// <summary>
        /// The calling object for this type (should be unused)
        /// </summary>
        public IResolvesToType CallingObject { get; set; }

        /// <summary>
        /// Returns true if <see cref="TypeParameters"/> has any elements
        /// </summary>
        public bool IsGeneric { get { return this.internalTypeParameters.Count > 0; } }

        public override IScope ParentScope {
            get {
                return base.ParentScope;
            }
            set {
                base.ParentScope = value;
                if(null != Prefix) {
                    Prefix.ParentScope = this.ParentScope;
                }
                foreach(var parameter in internalTypeParameters) {
                    parameter.ParentScope = this.ParentScope;
                }
            }
        }

        /// <summary>
        /// The prefix for this type use object
        /// </summary>
        public INamedScopeUse Prefix { get; set; }

        /// <summary>
        /// Parameters for the type use (indicates that this is a generic type use)
        /// </summary>
        public ReadOnlyCollection<ITypeUse> TypeParameters { get; private set; }

        /// <summary>
        /// Adds a generic type parameter to this type use
        /// </summary>
        /// <param name="typeParameter">The type parameter to add</param>
        public void AddTypeParameter(ITypeUse typeParameter) {
            this.internalTypeParameters.Add(typeParameter);
        }

        /// <summary>
        /// Adds all of the type parameters to this type use element
        /// </summary>
        /// <param name="typeParameters">An enumerable of type use elements to add</param>
        public void AddTypeParameters(IEnumerable<ITypeUse> typeParameters) {
            this.internalTypeParameters.AddRange(typeParameters);
        }

        /// <summary>
        /// Gets the first type that matches this use
        /// </summary>
        /// <returns>The matching type; null if there aren't any</returns>
        public ITypeDefinition FindFirstMatchingType() {
            return this.FindMatches().FirstOrDefault();
        }

        /// <summary>
        /// Finds all of the matches for this type
        /// </summary>
        /// <returns>All of the type definitions that match this type use</returns>
        public override IEnumerable<ITypeDefinition> FindMatches() {
            // if this is a built-in type, then just return that otherwise, go hunting for matching
            // types
            if(BuiltInTypeFactory.IsBuiltIn(this)) {
                yield return BuiltInTypeFactory.GetBuiltIn(this);
            } else if(null != Prefix) {
                var matches = from prefixMatch in Prefix.FindMatches()
                              from match in prefixMatch.GetChildScopesWithId<ITypeDefinition>(this.Name)
                              select match;
                foreach(var match in matches) {
                    yield return match;
                }
            } else {
                // First, just call AbstractUse.FindMatches() this will search everything in
                // ParentScope.GetParentScopesAndSelf<TypeDefinition>() for a matching type and
                // return it
                foreach(var match in base.FindMatches()) {
                    yield return match;
                }
            }
        }

        /// <summary>
        /// This is just a call to <see cref="FindMatches()"/>
        /// </summary>
        /// <returns>The matching type definitions for this use</returns>
        public IEnumerable<ITypeDefinition> FindMatchingTypes() {
            return this.FindMatches();
        }

        /// <summary>
        /// Tests if this type use is a match for the given
        /// <paramref name="definition"/></summary>
        /// <param name="definition">the definition to compare to</param>
        /// <returns>true if the definitions match; false otherwise</returns>
        public override bool Matches(ITypeDefinition definition) {
            return definition != null && definition.Name == this.Name;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            if(Prefix != null) {
                sb.Append(Prefix);
                sb.Append('.');
            }

            sb.Append(Name);

            if(IsGeneric) {
                sb.AppendFormat("<{0}>", String.Join(",", TypeParameters));
            }
            return sb.ToString();
        }
    }
}
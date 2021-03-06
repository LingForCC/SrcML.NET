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

namespace ABB.SrcML.Data {

    public interface IVariableDeclaration : IRootedObject {

        AccessModifier Accessibility { get; set; }

        SrcMLLocation Location { get; set; }

        string Name { get; set; }

        ITypeUse VariableType { get; set; }
    }
}
﻿/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *  Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    public abstract class AbstractQuery<TResult> {
        public int LockTimeout { get; private set; }
        public IDataRepository Data { get; private set; }
        protected TaskFactory Factory { get; private set; }

        private AbstractQuery() { }

        protected AbstractQuery(IDataRepository data, TaskFactory factory)
            : this(data, Timeout.Infinite, factory) { }

        protected AbstractQuery(IDataRepository data, int lockTimeout)
            : this(data, lockTimeout, Task.Factory) { }

        protected AbstractQuery(IDataRepository data, int lockTimeout, TaskFactory factory) {
            this.Data = data;
            this.LockTimeout = lockTimeout;
            this.Factory = factory;
        }

        public Task<TResult> ExecuteAsync(CancellationToken cancellationToken) {
            if(null == Data) {
                throw new InvalidOperationException("Query has no data repository");
            }
            return Factory.StartNew<TResult>(() => {
                IScope globalScope;
                cancellationToken.ThrowIfCancellationRequested();
                if(Data.TryLockGlobalScope(LockTimeout, out globalScope)) {
                    try {
                        cancellationToken.ThrowIfCancellationRequested();
                        return Execute(globalScope);
                    } finally {
                        Data.ReleaseGlobalScopeLock();
                    }
                }
                throw new TimeoutException();
            });
        }

        public Task<TResult> ExecuteAsync() {
            if(null == Data) {
                throw new InvalidOperationException("Query has no data repository");
            }
            return Factory.StartNew<TResult>(Execute);
        }

        public TResult Execute() {
            if(null == Data) {
                throw new InvalidOperationException("Query has no data repository");
            }
            IScope globalScope;
            if(Data.TryLockGlobalScope(LockTimeout, out globalScope)) {
                try {
                    return Execute(globalScope);
                } finally {
                    Data.ReleaseGlobalScopeLock();
                }
            }
            throw new TimeoutException();
        }

        public abstract TResult Execute(IScope globalScope);
    }

    public abstract class AbstractQueryBase<TTuple, TResult> {
        private AbstractQueryBase() { }

        internal AbstractQueryBase(IDataRepository data, int lockTimeout, TaskFactory factory) {
            this.Data = data;
            this.LockTimeout = lockTimeout;
            this.Factory = factory;
        }

        public int LockTimeout { get; private set; }
        public IDataRepository Data { get; private set; }
        protected TaskFactory Factory { get; private set; }

        protected Task<TResult> ExecuteAsync(TTuple parameterTuple, CancellationToken cancellationToken) {
            if(null == Data) {
                throw new InvalidOperationException("Query has no data repository");
            }
            Func<TResult> action = () => {
                IScope globalScope;
                cancellationToken.ThrowIfCancellationRequested();
                if(Data.TryLockGlobalScope(LockTimeout, out globalScope)) {
                    try {
                        cancellationToken.ThrowIfCancellationRequested();
                        return ExecuteImpl(globalScope, parameterTuple);
                    } finally {
                        Data.ReleaseGlobalScopeLock();
                    }
                }
                throw new TimeoutException();
            };

            return Factory.StartNew<TResult>(action, cancellationToken);
        }

        protected TResult Execute(TTuple parameterTuple) {
            if(null == Data) {
                throw new InvalidOperationException("Query has no data repository");
            }
            IScope globalScope;
            if(Data.TryLockGlobalScope(LockTimeout, out globalScope)) {
                try {
                    return ExecuteImpl(globalScope, parameterTuple);
                } finally {
                    Data.ReleaseGlobalScopeLock();
                }
            }
            throw new TimeoutException();
        }
        protected abstract TResult ExecuteImpl(IScope globalScope, TTuple parameterTuple);
    }

    public abstract class AbstractQuery<TParam, TResult> : AbstractQueryBase<Tuple<TParam>, TResult> {
        protected AbstractQuery(IDataRepository data, int lockTimeout)
            : base(data, lockTimeout, Task.Factory) { }

        protected AbstractQuery(IDataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        public Task<TResult> ExecuteAsync(TParam parameter) {
            return base.ExecuteAsync(Tuple.Create(parameter), new CancellationToken(false));
        }

        public Task<TResult> ExecuteAsync(TParam parameter, CancellationToken cancellationToken) {
            return base.ExecuteAsync(Tuple.Create(parameter), cancellationToken);
        }

        public TResult Execute(TParam parameter) {
            return base.Execute(Tuple.Create(parameter));
        }

        public abstract TResult Execute(IScope globalScope, TParam parameter);

        protected sealed override TResult ExecuteImpl(IScope globalScope, Tuple<TParam> parameterTuple) {
            return Execute(globalScope, parameterTuple.Item1);
        }
    }

    public abstract class AbstractQuery<TParam1, TParam2, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2>, TResult> {
        protected AbstractQuery(IDataRepository data, int lockTimeout)
            : base(data, lockTimeout, Task.Factory) { }

        protected AbstractQuery(IDataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2), cancellationToken);
        }
        
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2), new CancellationToken(false));
        }

        public TResult Execute(TParam1 parameter1, TParam2 parameter2) {
            return Execute(Tuple.Create(parameter1, parameter2));
        }

        public abstract TResult Execute(IScope globalScope, TParam1 parameter1, TParam2 parameter2);

        protected sealed override TResult ExecuteImpl(IScope globalScope, Tuple<TParam1, TParam2> parameter) {
            return Execute(globalScope, parameter.Item1, parameter.Item2);
        }
    }

    public abstract class AbstractQuery<TParam1, TParam2, TParam3, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2, TParam3>, TResult> {
        protected AbstractQuery(IDataRepository data, int lockTimeout)
            : base(data, lockTimeout, Task.Factory) { }

        protected AbstractQuery(IDataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3), cancellationToken);
        }

        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3), new CancellationToken(false));
        }

        public TResult Execute(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3) {
            return Execute(Tuple.Create(parameter1, parameter2, parameter3));
        }

        public abstract TResult Execute(IScope globalScope, TParam1 parameter1, TParam2 parameter2, TParam3 parameter3);

        protected sealed override TResult ExecuteImpl(IScope globalScope, Tuple<TParam1, TParam2, TParam3> parameter) {
            return Execute(globalScope, parameter.Item1, parameter.Item2, parameter.Item3);
        }
    }

    public abstract class AbstractQuery<TParam1, TParam2, TParam3, TParam4, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2, TParam3, TParam4>, TResult> {
        protected AbstractQuery(IDataRepository data, int lockTimeout)
            : base(data, lockTimeout, Task.Factory) { }

        protected AbstractQuery(IDataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4), cancellationToken);
        }

        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4), new CancellationToken(false));
        }

        public TResult Execute(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4) {
            return Execute(Tuple.Create(parameter1, parameter2, parameter3, parameter4));
        }

        public abstract TResult Execute(IScope globalScope, TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4);

        protected sealed override TResult ExecuteImpl(IScope globalScope, Tuple<TParam1, TParam2, TParam3, TParam4> parameter) {
            return Execute(globalScope, parameter.Item1, parameter.Item2, parameter.Item3, parameter.Item4);
        }
    }

    public abstract class AbstractQuery<TParam1, TParam2, TParam3, TParam4, TParam5, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2, TParam3, TParam4, TParam5>, TResult> {
        protected AbstractQuery(IDataRepository data, int lockTimeout)
            : base(data, lockTimeout, Task.Factory) { }

        protected AbstractQuery(IDataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4, parameter5), cancellationToken);
        }

        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4, parameter5), new CancellationToken(false));
        }

        public TResult Execute(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5) {
            return Execute(Tuple.Create(parameter1, parameter2, parameter3, parameter4, parameter5));
        }

        public abstract TResult Execute(IScope globalScope, TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5);

        protected sealed override TResult ExecuteImpl(IScope globalScope, Tuple<TParam1, TParam2, TParam3, TParam4, TParam5> parameter) {
            return Execute(globalScope, parameter.Item1, parameter.Item2, parameter.Item3, parameter.Item4, parameter.Item5);
        }
    }
}

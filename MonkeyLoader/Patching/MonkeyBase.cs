﻿using HarmonyLib;
using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MonkeyLoader.Patching
{
    /// <summary>
    /// Abstract base for regular <see cref="Monkey{TMonkey}"/>s and <see cref="EarlyMonkey{TMonkey}"/>s.
    /// </summary>
    public abstract partial class MonkeyBase : IMonkey
    {
        private static readonly Type _monkeyType = typeof(MonkeyBase);
        private readonly Lazy<IFeaturePatch[]> _featurePatches;
        private Mod _mod = null!;

        /// <inheritdoc/>
        public AssemblyName AssemblyName { get; }

        /// <inheritdoc/>
        public Config Config => Mod.Config;

        /// <summary>
        /// Gets whether this monkey's <see cref="Run">Run</see>() method failed when it was called.
        /// </summary>
        public bool Failed { get; protected set; }

        /// <inheritdoc/>
        public IEnumerable<IFeaturePatch> FeaturePatches => _featurePatches.Value.AsSafeEnumerable();

        /// <inheritdoc/>
        public Harmony Harmony => Mod.Harmony;

        /// <inheritdoc/>
        public MonkeyLogger Logger { get; private set; } = null!;

        /// <inheritdoc/>
        public Mod Mod
        {
            get => _mod;
            internal set
            {
                if (value == _mod)
                    return;

                _mod = value;
                Logger = new MonkeyLogger(_mod.Logger, Name);
            }
        }

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <summary>
        /// Gets whether this monkey's <see cref="Run">Run</see>() method has been called.
        /// </summary>
        public bool Ran { get; private protected set; } = false;

        /// <summary>
        /// Gets whether this monkey's <see cref="Shutdown">Shutdown</see>() failed when it was called.
        /// </summary>
        public bool ShutdownFailed { get; private set; } = false;

        /// <summary>
        /// Gets whether this monkey's <see cref="Shutdown">Shutdown</see>() method has been called.
        /// </summary>
        public bool ShutdownRan { get; private set; } = false;

        internal MonkeyBase()
        {
            var type = GetType();
            AssemblyName = new(type.Assembly.GetName().Name);

            _featurePatches = new Lazy<IFeaturePatch[]>(() =>
            {
                var featurePatches = GetFeaturePatches().ToArray();
                Array.Sort(featurePatches, FeaturePatch.DescendingComparer);

                return featurePatches;
            });
        }

        /// <inheritdoc/>
        public int CompareTo(IMonkey other) => Monkey.AscendingComparer.Compare(this, other);

        /// <summary>
        /// Runs this monkey to let it patch.<br/>
        /// Must only be called once.
        /// </summary>
        /// <inheritdoc/>
        public abstract bool Run();

        /// <summary>
        /// Lets this monkey cleanup and shutdown.<br/>
        /// Must only be called once.
        /// </summary>
        /// <inheritdoc/>
        public bool Shutdown()
        {
            if (ShutdownRan)
                throw new InvalidOperationException("A monkey's Shutdown() method must only be called once!");

            ShutdownRan = true;

            try
            {
                if (!OnShutdown())
                {
                    ShutdownFailed = true;
                    Logger.Warn(() => "OnShutdown failed!");
                }
            }
            catch (Exception ex)
            {
                ShutdownFailed = true;
                Logger.Error(() => ex.Format("OnShutdown threw an Exception:"));
            }

            return !ShutdownFailed;
        }

        internal static MonkeyBase GetInstance(Type type)
        {
            // Could do more specific inheriting from Monkey<> check
            if (!_monkeyType.IsAssignableFrom(type))
                throw new ArgumentException($"Given type [{type}] doesn't inherit from {_monkeyType.FullName}!", nameof(type));

            return Traverse.Create(type).Property<MonkeyBase>("Instance").Value;
        }

        /// <summary>
        /// Gets the impacts this (pre-)patcher has on certain features.
        /// </summary>
        protected abstract IEnumerable<IFeaturePatch> GetFeaturePatches();

        /// <summary>
        /// Lets this monkey cleanup and shutdown.
        /// </summary>
        /// <inheritdoc/>
        protected virtual bool OnShutdown() => true;

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if <see cref="Ran"/> is <c>true</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">If <see cref="Ran"/> is <c>true</c>.</exception>
        protected void ThrowIfRan()
        {
            if (Ran)
                throw new InvalidOperationException("A monkey's Run() method must only be called once!");
        }
    }

    /// <inheritdoc/>
    /// <typeparam name="TMonkey">The type of the actual patcher.</typeparam>
    public abstract class MonkeyBase<TMonkey> : MonkeyBase where TMonkey : MonkeyBase<TMonkey>, new()
    {
        /// <summary>
        /// Gets the <see cref="Configuration.Config"/> that this patcher can use to load <see cref="ConfigSection"/>s.
        /// </summary>
        public new static Config Config => Instance.Config;

        /// <summary>
        /// Gets the <see cref="HarmonyLib.Harmony">Harmony</see> instance to be used by this patcher.
        /// </summary>
        public new static Harmony Harmony => Instance.Harmony;

        /// <summary>
        /// Gets the instance of this patcher.
        /// </summary>
        public static MonkeyBase Instance { get; } = new TMonkey();

        /// <summary>
        /// Gets the <see cref="MonkeyLogger"/> that this patcher can use to log messages to game-specific channels.
        /// </summary>
        public new static MonkeyLogger Logger => Instance.Logger;

        /// <summary>
        /// Gets the mod that this patcher is a part of.
        /// </summary>
        public new static Mod Mod => Instance.Mod;

        /// <summary>
        /// Allows creating only a single <typeparamref name="TMonkey"/> instance.
        /// </summary>
        internal MonkeyBase() : base()
        {
            if (Instance is not null)
                throw new InvalidOperationException("Can't create more than one patcher instance!");
        }

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public static void Debug(Func<object> messageProducer) => Logger.Debug(messageProducer);

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Debug(params Func<object>[] messageProducers) => Logger.Debug(messageProducers);

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Debug(IEnumerable<Func<object>> messageProducers) => Logger.Debug(messageProducers);

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public static void Error(Func<object> messageProducer) => Logger.Error(messageProducer);

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Error(params Func<object>[] messageProducers) => Logger.Error(messageProducers);

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Error(IEnumerable<Func<object>> messageProducers) => Logger.Error(messageProducers);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public static void Fatal(Func<object> messageProducer) => Logger.Fatal(messageProducer);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Fatal(params Func<object>[] messageProducers) => Logger.Fatal(messageProducers);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Fatal(IEnumerable<Func<object>> messageProducers) => Logger.Fatal(messageProducers);

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public static void Info(Func<object> messageProducer) => Logger.Info(messageProducer);

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Info(params Func<object>[] messageProducers) => Logger.Info(messageProducers);

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Info(IEnumerable<Func<object>> messageProducers) => Logger.Info(messageProducers);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public static void Trace(Func<object> messageProducer) => Logger.Trace(messageProducer);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Trace(params Func<object>[] messageProducers) => Logger.Trace(messageProducers);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Trace(IEnumerable<Func<object>> messageProducers) => Logger.Trace(messageProducers);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public static void Warn(Func<object> messageProducer) => Logger.Warn(messageProducer);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Warn(params Func<object>[] messageProducers) => Logger.Warn(messageProducers);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public static void Warn(IEnumerable<Func<object>> messageProducers) => Logger.Warn(messageProducers);
    }
}
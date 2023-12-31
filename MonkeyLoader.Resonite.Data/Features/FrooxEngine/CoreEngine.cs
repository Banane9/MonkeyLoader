﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Resonite.Features.FrooxEngine
{
    /// <summary>
    /// Anything affecting the whole core Engine class.
    /// </summary>
    public class CoreEngine : FrooxEngine
    {
        /// <inheritdoc/>
        public override string Description { get; } = "Anything affecting the whole core Engine class.";

        /// <inheritdoc/>
        public override string Name { get; } = "Engine";
    }

    /// <summary>
    /// The Engine's initialization process.
    /// </summary>
    public class EngineInitialization : CoreEngine
    {
        /// <inheritdoc/>
        public override string Description { get; } = "The Engine's initialization process.";

        /// <inheritdoc/>
        public override string Name { get; } = "Engine Initialization";
    }
}
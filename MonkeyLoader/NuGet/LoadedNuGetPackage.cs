﻿using NuGet.Frameworks;
using NuGet.Packaging.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.NuGet
{
    /// <summary>
    /// Represents the information required for NuGet package dependency resolution.
    /// </summary>
    public interface ILoadedNuGetPackage
    {
        public bool AllDependenciesLoaded { get; }

        /// <summary>
        /// Gets the dependencies of the package.
        /// </summary>
        public IEnumerable<DependencyReference> Dependencies { get; }

        /// <summary>
        /// Gets the identity of the package.
        /// </summary>
        public PackageIdentity Identity { get; }

        /// <summary>
        /// Gets the framework targeted with this dependency.
        /// </summary>
        public NuGetFramework TargetFramework { get; }

        public bool TryResolveDependencies();
    }

    /// <summary>
    /// Represents a loaded pseudo-package,
    /// i.e. loaded game assemblies that can be referenced as NuGet packages by mods.
    /// </summary>
    public sealed class LoadedNuGetPackage : ILoadedNuGetPackage
    {
        private readonly DependencyReference[] _dependencies;
        private bool _allDependenciesLoaded = false;

        /// <inheritdoc/>
        public bool AllDependenciesLoaded
        {
            get
            {
                if (!_allDependenciesLoaded)
                    _allDependenciesLoaded = _dependencies.All(dep => dep.AllDependenciesLoaded);

                return _allDependenciesLoaded;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<DependencyReference> Dependencies => _dependencies.AsSafeEnumerable();

        /// <inheritdoc/>
        public PackageIdentity Identity { get; }

        /// <inheritdoc/>
        public NuGetFramework TargetFramework { get; }

        /// <summary>
        /// Creates a new loaded pseudo-package instance with the given parameters.
        /// </summary>
        /// <param name="identity">The identity of the loaded package.</param>
        /// <param name="targetFramework">The framework targeted by the package.</param>
        /// <param name="dependencies">The dependencies of the package.</param>
        public LoadedNuGetPackage(PackageIdentity identity, NuGetFramework targetFramework, params DependencyReference[] dependencies)
        {
            Identity = identity;
            TargetFramework = targetFramework;
            _dependencies = dependencies;
        }

        /// <summary>
        /// Creates a new loaded pseudo-package instance with the given parameters.
        /// </summary>
        /// <param name="identity">The identity of the loaded package.</param>
        /// <param name="targetFramework">The framework targeted by the package.</param>
        /// <param name="dependencies">The dependencies of the package.</param>
        public LoadedNuGetPackage(PackageIdentity identity, NuGetFramework targetFramework, IEnumerable<DependencyReference> dependencies)
            : this(identity, targetFramework, dependencies.ToArray())
        { }

        public bool TryResolveDependencies()
            => _dependencies.Select(dep => dep.TryResolve()).All();
    }
}
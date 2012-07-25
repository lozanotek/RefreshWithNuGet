﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Ninject;
using NuGet.Server.DataServices;

namespace NuGet.Server.Infrastructure
{
    public class ServerPackageRepository : LocalPackageRepository, IServerPackageRepository
    {
        private readonly IDictionary<IPackage, DerivedPackageData> _derivedDataLookup = new Dictionary<IPackage, DerivedPackageData>();

        public ServerPackageRepository(string path)
            : base(path)
        {
        }

        public ServerPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem)
            : base(pathResolver, fileSystem)
        {
        }

        [Inject]
        public IHashProvider HashProvider { get; set; }

        public IQueryable<Package> GetPackagesWithDerivedData()
        {
            return from package in base.GetPackages()
                   select GetMetadataPackage(package);
        }

        public override void AddPackage(IPackage package)
        {
            string fileName = PathResolver.GetPackageFileName(package);
            using (Stream stream = package.GetStream())
            {
                FileSystem.AddFile(fileName, stream);
            }
        }

        public void RemovePackage(string packageId, SemanticVersion version)
        {
            IPackage package = FindPackage(packageId, version);
            if (package != null)
            {
                RemovePackage(package);
            }
        }

        public override void RemovePackage(IPackage package)
        {
            string fileName = PathResolver.GetPackageFileName(package);
            FileSystem.DeleteFile(fileName);
        }

        protected override IPackage OpenPackage(string path)
        {
            IPackage package = base.OpenPackage(path);
            _derivedDataLookup[package] = CalculateDerivedData(package, path);
            return package;
        }

        public Package GetMetadataPackage(IPackage package)
        {
            return new Package(package, _derivedDataLookup[package]);
        }

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            var packages = GetPackages().Find(searchTerm)
                                        .FilterByPrerelease(allowPrereleaseVersions)
                                        .AsQueryable();

            // TODO: Enable this when we can make it faster
            //if (targetFrameworks.Any()) {
            //    // Get the list of framework names
            //    var frameworkNames = targetFrameworks.Select(frameworkName => VersionUtility.ParseFrameworkName(frameworkName));

            //    packages = packages.Where(package => frameworkNames.Any(frameworkName => IsCompatible(frameworkName, package)));
            //}

            return packages;
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            // TODO : Fix this when we can update Core.
            return (from p in GetPackages()
                    where p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)
                    orderby p.Id
                    select p);
        }

        public IEnumerable<IPackage> GetUpdates(IEnumerable<IPackage> packages, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> targetFramework)
        {
            return this.GetUpdatesCore(packages, includePrerelease, includeAllVersions, targetFramework);
        }

        private bool IsCompatible(FrameworkName frameworkName, IPackage package)
        {
            var packageData = _derivedDataLookup[package];

            return VersionUtility.IsCompatible(frameworkName, packageData.SupportedFrameworks);
        }

        private DerivedPackageData CalculateDerivedData(IPackage package, string path)
        {
            byte[] fileBytes;
            using (Stream stream = FileSystem.OpenFile(path))
            {
                fileBytes = stream.ReadAllBytes();
            }

            return new DerivedPackageData
            {
                PackageSize = fileBytes.Length,
                PackageHash = Convert.ToBase64String(HashProvider.CalculateHash(fileBytes)),
                LastUpdated = FileSystem.GetLastModified(path),
                Created = FileSystem.GetCreated(path),
                // TODO: Add support when we can make this faster
                // SupportedFrameworks = package.GetSupportedFrameworks(),
                Path = path,
                FullPath = FileSystem.GetFullPath(path)
            };
        }
    }
}

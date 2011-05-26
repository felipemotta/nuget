﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.VisualStudio.Test {
    using NuGet.Test;

    [TestClass]
    public class FallbackRepositoryTest {
        [TestMethod]
        public void CreatePackageManagerUsesPrimaryRepositoryAsDependencyProviderIfUseFallbackIsFalse() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockFileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });

            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, mockFileSystemProvider.Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var packageManager = packageManagerFactory.CreatePackageManager(mockRepository1, useFallbackForDependencies: false);

            // Assert
            Assert.AreEqual(mockRepository1, packageManager.SourceRepository);
        }

        [TestMethod]
        public void CreatePackageManagerUsesFallbackRepositoryyAsDependencyProviderIfUseFallbackIsTrue() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockFileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });

            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, mockFileSystemProvider.Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var packageManager = packageManagerFactory.CreatePackageManager(mockRepository1, useFallbackForDependencies: true);

            // Assert
            Assert.IsInstanceOfType(packageManager.SourceRepository, typeof(FallbackRepository));
            var fallbackRepo = (FallbackRepository)packageManager.SourceRepository;
            Assert.IsInstanceOfType(fallbackRepo.DependencyResolver, typeof(AggregateRepository));
            var dependencyProvider = (AggregateRepository)fallbackRepo.DependencyResolver;
            Assert.AreEqual(2, dependencyProvider.Repositories.Count());
            Assert.AreEqual(mockRepository1, dependencyProvider.Repositories.First());
            Assert.AreEqual(mockRepository2, dependencyProvider.Repositories.Last());

        }


        [TestMethod]
        public void GetDependenciesReturnsPackagesFromAggregateSources() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var repository = packageManagerFactory.CreateFallbackRepository(mockRepository1);

            // Assert
            var dependencyProvider = repository as IDependencyProvider;
            List<IPackage> dependencies = dependencyProvider.GetDependencies("A").ToList();
            List<IPackage> packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(1, packages.Count());
            Assert.AreEqual("A", dependencies[0].Id);
            Assert.AreEqual(new Version("1.0"), dependencies[0].Version);

            Assert.AreEqual(2, dependencies.Count);
            Assert.AreEqual("A", dependencies[0].Id);
            Assert.AreEqual(new Version("1.0"), dependencies[0].Version);

            Assert.AreEqual("A", dependencies[1].Id);
            Assert.AreEqual(new Version("1.2"), dependencies[1].Version);
        }

        [TestMethod]
        public void CreateFallbackRepositoryReturnsCurrentIfCurrentIsAggregateRepository() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var aggregateRepo = new AggregateRepository(new[] { mockRepository1, mockRepository2 });

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var aggregateSource = AggregatePackageSource.Instance;

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, aggregateSource });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var repository = packageManagerFactory.CreateFallbackRepository(aggregateRepo);

            // Assert
            Assert.AreEqual(aggregateRepo, repository);
        }

        [TestMethod]
        public void CreateFallbackRepositoryUsesResolvedSourceNameWhenEnsuringRepositoryIsNotAlreadyListedInAggregate() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository("http://redirected");
            var aggregateRepo = new AggregateRepository(new[] { mockRepository1, mockRepository2 });

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var aggregateSource = AggregatePackageSource.Instance;

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, aggregateSource });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            FallbackRepository repository = (FallbackRepository)packageManagerFactory.CreateFallbackRepository(mockRepository2);

            // Assert
            var dependencyProvider = (AggregateRepository)repository.DependencyResolver;
            Assert.AreEqual(2, dependencyProvider.Repositories.Count());
        }

        [TestMethod]
        public void CreateFallbackRepositoryDoesNotThrowWhenIteratingOverFailingRepositories() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository("http://redirected");

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("SourceBad");
            var aggregateSource = AggregatePackageSource.Instance;

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source3, source2, aggregateSource });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "SourceBad": throw new InvalidOperationException();
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            FallbackRepository repository = (FallbackRepository)packageManagerFactory.CreateFallbackRepository(mockRepository2);

            // Assert
            var dependencyProvider = (AggregateRepository)repository.DependencyResolver;
            Assert.AreEqual(2, dependencyProvider.Repositories.Count());
        }

        [TestMethod]
        public void CreateFallbackRepositoryIncludesRepositoryOnceInAggregateDependencyResolver() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository("Source1");
            var mockRepository2 = new MockPackageRepository("Source2");

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var repository = packageManagerFactory.CreateFallbackRepository(mockRepository1);

            // Assert
            var fallbackRepo = repository as FallbackRepository;
            var aggregateRepo = (AggregateRepository)fallbackRepo.DependencyResolver;
            Assert.AreEqual(2, aggregateRepo.Repositories.Count());
            Assert.AreEqual(mockRepository1, aggregateRepo.Repositories.First());
            Assert.AreEqual(mockRepository2, aggregateRepo.Repositories.Last());
        }
    }
}
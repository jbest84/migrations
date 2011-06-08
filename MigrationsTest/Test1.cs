﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Migrations;
using System.Reflection;

namespace MigrationsTest
{
    [TestClass]
    public class Test1
    {
        private MigrationService runner;
        private IVersionDataSource versionDataSource;

        [TestInitialize]
        public void SetUp()
        {
            versionDataSource = new StubVersionDataSource();
            versionDataSource.SetVersionNumber(0);
            this.runner = new MigrationService(versionDataSource);
        }

        [TestMethod]
        public void TestLoadMigrationsFromAssembly()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            this.runner.LoadMigrationsFromAssembly(asm); // Only assemblies with default empty ctor will get loaded
            List<IMigration> migrations = this.runner.Migrations;
            Assert.IsTrue(migrations.Count == 2); // Migration1 and Migration2 should be only ones loaded
            foreach (var m in migrations)
            {
                Type foo = m.GetType();
                Assert.IsTrue(foo.IsClass);
                Assert.IsNotNull(foo.GetInterface("IMigration"));
                Assert.IsNotNull(MigrationService.GetMigrationsAttributes(m));
            }
        }

        [TestMethod]
        public void TestLoadMigrationsFromAssemblyWithParams()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            this.runner.LoadMigrationsFromAssembly(asm, ""); // Load migrations with default ctor, and optionally if ctor takes a string
            List<IMigration> migrations = this.runner.Migrations;
            Assert.IsTrue(migrations.Count == 3); // Migration1, Migration2, and MigrationWithCTorParams should be loaded
            foreach (var m in migrations)
            {
                Type foo = m.GetType();
                Assert.IsTrue(foo.IsClass);
                Assert.IsNotNull(foo.GetInterface("IMigration"));
                Assert.IsNotNull(MigrationService.GetMigrationsAttributes(m));
            }
        }

        [TestMethod]
        public void TestGetMigrationAttributes()
        {
            Migration1 foo = new Migration1();
            MigrationAttribute attr = MigrationService.GetMigrationsAttributes(foo);
            Assert.AreEqual(attr.Version, 1.0);
        }

        [TestMethod]
        public void TestGetMigrationNoAttributes()
        {
            IMigration foo = new MigrationNoAttributes();
            MigrationAttribute attr = MigrationService.GetMigrationsAttributes(foo);
            Assert.IsNull(attr);
        }

        [TestMethod]
        public void TestGetMigrationWrongAttributes()
        {
            IMigration foo = new MigrationWrongAttributes();
            MigrationAttribute attr = MigrationService.GetMigrationsAttributes(foo);
            Assert.IsNull(attr);
        }

        [TestMethod]
        public void TestMigrationSort()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();

            List<IMigration> migrations = new List<IMigration>{
                migration2,
                migration1
            };

            migrations.Sort(MigrationService.MigrationSorter);
            Assert.AreSame(migrations[0], migration1);

            migrations = new List<IMigration>{
                migration1,
                migration2
            };

            migrations.Sort(MigrationService.MigrationSorter);
            Assert.AreSame(migrations[0], migration1);
        }

        [TestMethod]
        public void TestGetMigrationVersionNumber()
        {
            IMigration migration1 = new Migration1();
            Assert.AreEqual(1.0, MigrationService.GetMigrationVersionNumber(migration1));
        }

        [TestMethod]
        public void TestGetMigrationVersionNumberNoAttributes()
        {
            IMigration migration = new MigrationNoAttributes();
            Assert.AreEqual(-1, MigrationService.GetMigrationVersionNumber(migration));
        }

        [TestMethod]
        public void TestGetMigrationVersionNumberWrongAttributes()
        {
            IMigration migration = new MigrationWrongAttributes();
            Assert.AreEqual(-1, MigrationService.GetMigrationVersionNumber(migration));
        }

        [TestMethod]
        public void TestRunUpMigrations()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                this.runner.Migrations = migrations;
                this.runner.RunUpMigrations();
                // Assert that we ran two upgrade migrations, are at version 2
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == 2);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunUpMigrationsTo()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int UPGRADE_TO_VERSION = 2;
                this.runner.Migrations = migrations;
                this.runner.RunUpMigrations(UPGRADE_TO_VERSION);
                // Assert that we ran two upgrade migrations, are at version UGRADE_TO_VERSION
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == UPGRADE_TO_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunUpMigrationsToBogus()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int SCHEMA_VERSION = 2;
                this.versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                const int UPGRADE_TO_VERSION = 1;
                this.runner.Migrations = migrations;
                this.runner.RunUpMigrations(UPGRADE_TO_VERSION);

                // Assert that we did not run the bogus upgrade and that the schema version did not change
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunUpMigrationsBogus()
        {
            IMigration migration1 = new MigrationNoAttributes();
            IMigration migration2 = new MigrationWrongAttributes();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int SCHEMA_VERSION = 10;
                this.versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                this.runner.Migrations = migrations;
                this.runner.RunUpMigrations();
                // Assert that we didn't run the bogus migrations
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
        
        [TestMethod]
        public void TestRunUpToMigrationsBogus()
        {
            IMigration migration1 = new MigrationNoAttributes();
            IMigration migration2 = new MigrationWrongAttributes();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int SCHEMA_VERSION = 0;
                const int UPGRADE_TO_VERSION = 2;
                this.versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                this.runner.Migrations = migrations;
                this.runner.RunUpMigrations(UPGRADE_TO_VERSION);
                // Assert that we didn't run the bogus migrations
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrations()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                this.versionDataSource.SetVersionNumber(2);
                this.runner.Migrations = migrations;
                this.runner.RunDownMigrations();

                // Assert that we end up downgraded to version 0
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == 0);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrationsToBogus()
        {
            IMigration migration1 = new MigrationWrongAttributes();
            IMigration migration2 = new MigrationNoAttributes();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int SCHEMA_VERSION = 10;
                this.versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                const int DOWNGRADE_TO_VERSION = 1;
                this.runner.Migrations = migrations;
                this.runner.RunDownMigrations(DOWNGRADE_TO_VERSION);

                // Assert that we did not run the bogus downgrade and that the schema version did not change
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrationsToBogusVersion()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int SCHEMA_VERSION = 2;
                this.versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                const int DOWNGRADE_TO_VERSION = 5;
                this.runner.Migrations = migrations;
                this.runner.RunDownMigrations(DOWNGRADE_TO_VERSION);
                this.runner.RunDownMigrations(SCHEMA_VERSION);

                // Assert that we did not run the bogus downgrade and that the schema version did not change
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrationsTo()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int DOWNGRADE_TO_VERSION = 1;
                const int SCHEMA_VERSION = 2;

                this.versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                this.runner.Migrations = migrations;
                this.runner.RunDownMigrations(DOWNGRADE_TO_VERSION);
                // Assert that we ran two downgrade migrations, are at version DOWNGRADE_TO_VERSION
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == DOWNGRADE_TO_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
        [TestMethod]
        public void TestRunDownMigrationsBogus()
        {
            IMigration migration1 = new MigrationNoAttributes();
            IMigration migration2 = new MigrationWrongAttributes();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                const int SCHEMA_VERSION = 10;
                this.versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                
                this.runner.Migrations = migrations;
                this.runner.RunDownMigrations();

                // Assert that we didn't run the bogus migrations
                Assert.IsTrue(this.versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestIndexerGet()
        {
            var m1 = new Migration1();
            this.runner.Migrations.Add(m1);
            Assert.AreSame(m1, this.runner[0]);
        }

        [TestMethod]
        public void TestIndexSet()
        {
            var m1 = new Migration1();
            this.runner.Migrations.Add(m1);

            var m2 = new Migration2();
            this.runner[0] = m2;

            Assert.AreEqual(m2, this.runner.Migrations[0]);
        }
    }
}

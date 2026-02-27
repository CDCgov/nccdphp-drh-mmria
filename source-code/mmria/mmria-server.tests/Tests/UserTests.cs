#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria_server.tests.Helpers;

namespace mmria_server.tests.Tests;

/// <summary>
/// User management tests.
/// Tests user CRUD operations, role assignment, jurisdiction authorization,
/// and role hierarchy enforcement.
/// </summary>
[TestFixture]
public class UserTests
{
    private TestEnvironment _env = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _env = await TestEnvironment.BootstrapAsync("user_tests");
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        await _env.ResolveConfigurationAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _env.CleanupAsync();
    }

    [Test]
    public void Placeholder()
    {
        Assert.Pass("UserTests stub - ready for test implementation.");
    }

    [Test]
    [Category("UserManagement")]
    public async Task CheckUser_ExistingUser_ReturnsPopulatedUser()
    {
        // TODO: Create a test user via ManageUsersManager.SaveUserAsync,
        //       then call CheckUserAsync and verify the returned user has a name.
        Assert.Inconclusive("Stub - not yet implemented.");
    }

    [Test]
    [Category("UserManagement")]
    public async Task CheckUser_NonExistentUser_ReturnsEmptyUser()
    {
        // TODO: Call CheckUserAsync with a user_id that does not exist,
        //       verify the returned user object has null/empty name (duplicate check = available).
        Assert.Inconclusive("Stub - not yet implemented.");
    }

    [Test]
    [Category("UserManagement")]
    public async Task CheckUser_DuplicateUserName_DetectedBeforeCreate()
    {
        // TODO: Create a user, then call CheckUserAsync with the same user_id,
        //       verify the returned user has a populated name (duplicate detected).
        Assert.Inconclusive("Stub - not yet implemented.");
    }
}

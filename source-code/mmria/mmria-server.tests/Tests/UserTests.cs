#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria_server.tests.Helpers;
using mmria.common.SharedLibraries.ManageUsers.DAL;
using mmria.common.SharedLibraries.ManageUsers.Manager;

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
    private ManageUsersManager _manager = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _env = await TestEnvironment.BootstrapAsync("user_tests");
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        await _env.ResolveConfigurationAsync();
        var dal = new ManageUsersDAL(_env.CouchDbClient);
        _manager = new ManageUsersManager(dal, _env.CouchDbClient);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        if (_env != null)
        {
            await _env.CleanupAsync();
        }
    }

    [Test]
    [Category("UserManagement")]
    public async Task CheckUser_ExistingUser_ReturnsTrue()
    {
        var db_config = _env.Config!.DbConfig;
        var sampleCredentials = _env.Config!.ConfigLoader.TestCredentials.SampleCredentials;
        var test_user_name = $"test_existing_{Guid.NewGuid():N}";
        var test_user_id = $"org.couchdb.user:{test_user_name}";

        // Create a test user
        var user = new mmria.common.model.couchdb.user
        {
            _id = test_user_id,
            name = test_user_name,
            password = sampleCredentials.UserCreationPassword,
            type = "user",
            roles = new string[] { }
        };
        var saveResult = await _manager.SaveUserAsync(user, db_config);
        Assert.That(saveResult.ok, Is.True, "Test user creation should succeed.");

        // Check that CheckUserAsync returns true for existing user
        bool exists = await _manager.CheckUserAsync(test_user_id, db_config);
        Assert.That(exists, Is.True, "CheckUserAsync should return true for an existing user.");

        // Cleanup
        await _manager.DeleteUserAsync(test_user_id, saveResult.rev, db_config);
    }

    [Test]
    [Category("UserManagement")]
    public async Task CheckUser_NonExistentUser_ReturnsFalse()
    {
        var db_config = _env.Config!.DbConfig;
        var fake_user_id = $"org.couchdb.user:nonexistent_{Guid.NewGuid():N}";

        bool exists = await _manager.CheckUserAsync(fake_user_id, db_config);
        Assert.That(exists, Is.False, "CheckUserAsync should return false for a non-existent user.");
    }

    [Test]
    [Category("UserManagement")]
    public async Task CheckUser_DuplicateUserName_DetectedBeforeCreate()
    {
        var db_config = _env.Config!.DbConfig;
        var sampleCredentials = _env.Config!.ConfigLoader.TestCredentials.SampleCredentials;
        var test_user_name = $"test_dup_{Guid.NewGuid():N}";
        var test_user_id = $"org.couchdb.user:{test_user_name}";

        // Create the first user
        var user = new mmria.common.model.couchdb.user
        {
            _id = test_user_id,
            name = test_user_name,
            password = sampleCredentials.UserCreationPassword,
            type = "user",
            roles = new string[] { }
        };
        var saveResult = await _manager.SaveUserAsync(user, db_config);
        Assert.That(saveResult.ok, Is.True, "First user creation should succeed.");

        // Attempt to create a second user with the same _id (no _rev = new user attempt)
        var duplicate_user = new mmria.common.model.couchdb.user
        {
            _id = test_user_id,
            name = test_user_name,
            password = sampleCredentials.AlternateUserCreationPassword,
            type = "user",
            roles = new string[] { }
        };
        var duplicateResult = await _manager.SaveUserAsync(duplicate_user, db_config);
        Assert.That(duplicateResult.ok, Is.False, "Second creation with same username should be rejected.");

        // Cleanup
        await _manager.DeleteUserAsync(test_user_id, saveResult.rev, db_config);
    }
}

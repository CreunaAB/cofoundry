﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using Cofoundry.Domain.Data;
using Cofoundry.Domain.CQS;
using Cofoundry.Core.Validation;
using Cofoundry.Core.Mail;

namespace Cofoundry.Domain
{
    /// <summary>
    /// A generic user creation command for use with Cofoundry users and
    /// other non-Cofoundry users. Does not send any email notifications.
    /// </summary>
    public class AddUserCommandHandler 
        : ICommandHandler<AddUserCommand>
        , IAsyncCommandHandler<AddUserCommand>
        , IPermissionRestrictedCommandHandler<AddUserCommand>
    {
        #region constructor

        private readonly CofoundryDbContext _dbContext;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IPasswordCryptographyService _passwordCryptographyService;
        private readonly IPasswordGenerationService _passwordGenerationService;
        private readonly IMailService _mailService;
        private readonly UserCommandPermissionsHelper _userCommandPermissionsHelper;
        private readonly IPermissionValidationService _permissionValidationService;
        private readonly IUserAreaRepository _userAreaRepository;
        
        public AddUserCommandHandler(
            CofoundryDbContext dbContext,
            IQueryExecutor queryExecutor,
            IPasswordCryptographyService passwordCryptographyService,
            IPasswordGenerationService passwordGenerationService,
            IMailService mailService,
            UserCommandPermissionsHelper userCommandPermissionsHelper,
            IPermissionValidationService permissionValidationService,
            IUserAreaRepository userAreaRepository
            )
        {
            _dbContext = dbContext;
            _queryExecutor = queryExecutor;
            _passwordCryptographyService = passwordCryptographyService;
            _mailService = mailService;
            _userCommandPermissionsHelper = userCommandPermissionsHelper;
            _permissionValidationService = permissionValidationService;
            _userAreaRepository = userAreaRepository;
            _passwordGenerationService = passwordGenerationService;
        }

        #endregion

        #region execution

        public void Execute(AddUserCommand command, IExecutionContext executionContext)
        {
            var userArea = _userAreaRepository.GetByCode(command.UserAreaCode);
            var dbUserArea = QueryUserArea(userArea).SingleOrDefault();
            dbUserArea = AddUserAreaIfNotExists(userArea, dbUserArea);

            ValidateCommand(command, userArea);
            var isUnique = _queryExecutor.Execute(GetUniqueQuery(command, userArea), executionContext);
            ValidateIsUnique(isUnique, userArea);

            var newRole = _userCommandPermissionsHelper.GetAndValidateNewRole(command.RoleId, command.UserAreaCode, executionContext);

            var user = MapAndAddUser(command, executionContext, newRole, userArea, dbUserArea);
            _dbContext.SaveChanges();

            command.OutputUserId = user.UserId;
        }

        public async Task ExecuteAsync(AddUserCommand command, IExecutionContext executionContext)
        {
            var userArea = _userAreaRepository.GetByCode(command.UserAreaCode);
            var dbUserArea = await QueryUserArea(userArea).SingleOrDefaultAsync();
            dbUserArea = AddUserAreaIfNotExists(userArea, dbUserArea);

            ValidateCommand(command, userArea);
            var isUnique = await _queryExecutor.ExecuteAsync(GetUniqueQuery(command, userArea), executionContext);
            ValidateIsUnique(isUnique, userArea);

            var newRole = _userCommandPermissionsHelper.GetAndValidateNewRole(command.RoleId, command.UserAreaCode, executionContext);

            var user = MapAndAddUser(command, executionContext, newRole, userArea, dbUserArea);
            await _dbContext.SaveChangesAsync();

            command.OutputUserId = user.UserId;
        }

        #endregion

        #region helpers

        /// <summary>
        /// Perform some additional command validation that we can't do using data 
        /// annotations.
        /// </summary>
        private void ValidateCommand(AddUserCommand command, IUserAreaDefinition userArea)
        {
            // Password
            var isPasswordEmpty = string.IsNullOrWhiteSpace(command.Password);

            if (userArea.AllowPasswordLogin && isPasswordEmpty && !command.GeneratePassword)
            {
                throw new PropertyValidationException("Password field is required", "Password");
            }
            else if (!userArea.AllowPasswordLogin && !isPasswordEmpty)
            {
                throw new PropertyValidationException("Password field should be empty because the specified user area does not use passwords", "Password");
            }

            // Email
            if (userArea.UseEmailAsUsername && string.IsNullOrEmpty(command.Email))
            {
                throw new PropertyValidationException("Email field is required.", "Email");
            }

            // Username
            if (userArea.UseEmailAsUsername && !string.IsNullOrEmpty(command.Username))
            {
                throw new PropertyValidationException("Usename field should be empty becuase the specified user area uses the email as the username.", "Password");
            }
            else if (!userArea.UseEmailAsUsername && string.IsNullOrWhiteSpace(command.Username))
            {
                throw new PropertyValidationException("Username field is required", "Username");
            }
        }

        private IQueryable<UserArea> QueryUserArea(IUserAreaDefinition userArea)
        {
            return _dbContext
                .UserAreas
                .Where(a => a.UserAreaCode == userArea.UserAreaCode);
        }

        private UserArea AddUserAreaIfNotExists(IUserAreaDefinition userArea, UserArea dbUserArea)
        {
            if (dbUserArea == null)
            {
                dbUserArea = new UserArea();
                dbUserArea.UserAreaCode = userArea.UserAreaCode;
                dbUserArea.Name = userArea.Name;
                _dbContext.UserAreas.Add(dbUserArea);
            }

            return dbUserArea;
        }

        private User MapAndAddUser(AddUserCommand command, IExecutionContext executionContext, Role role, IUserAreaDefinition userArea, UserArea dbUserArea)
        {
            var user = new User();
            user.FirstName = command.FirstName.Trim();
            user.LastName = command.LastName.Trim();
            user.Email = command.Email;
            user.RequirePasswordChange = command.RequirePasswordChange;
            user.LastPasswordChangeDate = executionContext.ExecutionDate;
            user.CreateDate = executionContext.ExecutionDate;
            user.Role = role;
            user.UserArea = dbUserArea;

            if (userArea.AllowPasswordLogin)
            {
                var password = command.GeneratePassword ? _passwordGenerationService.Generate() : command.Password;

                var hashResult = _passwordCryptographyService.CreateHash(password);
                user.Password = hashResult.Hash;
                user.PasswordEncryptionVersion = (int)hashResult.EncryptionVersion;
            }

            if (userArea.UseEmailAsUsername)
            {
                user.Username = command.Email;
            }
            else
            {
                user.Username = command.Username.Trim();
            }

            _dbContext.Users.Add(user);

            return user;
        }

        private void ValidateIsUnique(bool isUnique, IUserAreaDefinition userArea)
        {
            if (!isUnique)
            {
                if (userArea.UseEmailAsUsername)
                {
                    throw new PropertyValidationException("This email is already registered", "Email");
                }
                else
                {
                    throw new PropertyValidationException("This username is already registered", "Username");
                }
            }
        }

        private IsUsernameUniqueQuery GetUniqueQuery(AddUserCommand command, IUserAreaDefinition userArea)
        {
            var query = new IsUsernameUniqueQuery();

            if (userArea.UseEmailAsUsername)
            {
                query.Username = command.Email;
            }
            else
            {
                query.Username = command.Username.Trim();
            }
            query.UserAreaCode = command.UserAreaCode;

            return query;
        }

        private IQueryable<Role> QueryRole(AddUserCommand command)
        {
            return _dbContext
                .Roles
                .FilterById(command.RoleId);
        }

        #endregion

        #region Permission

        public IEnumerable<IPermissionApplication> GetPermissions(AddUserCommand command)
        {
            if (command.UserAreaCode == CofoundryAdminUserArea.AreaCode)
            {
                yield return new CofoundryUserCreatePermission();
            }
            else
            {
                yield return new NonCofoundryUserCreatePermission();
            }
        }

        #endregion
    }
}

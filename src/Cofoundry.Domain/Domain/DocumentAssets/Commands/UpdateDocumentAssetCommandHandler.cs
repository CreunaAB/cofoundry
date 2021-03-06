﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cofoundry.Domain.Data;
using Cofoundry.Domain.CQS;
using System.Data.Entity;
using Cofoundry.Core;
using Cofoundry.Core.EntityFramework;

namespace Cofoundry.Domain
{
    public class UpdateDocumentAssetCommandHandler 
        : IAsyncCommandHandler<UpdateDocumentAssetCommand>
        , IPermissionRestrictedCommandHandler<UpdateDocumentAssetCommand>
    {
        #region constructor

        private readonly CofoundryDbContext _dbContext;
        private readonly EntityAuditHelper _entityAuditHelper;
        private readonly EntityTagHelper _entityTagHelper;
        private readonly DocumentAssetCommandHelper _documentAssetCommandHelper;
        private readonly ITransactionScopeFactory _transactionScopeFactory;

        public UpdateDocumentAssetCommandHandler(
            CofoundryDbContext dbContext,
            EntityAuditHelper entityAuditHelper,
            EntityTagHelper entityTagHelper,
            DocumentAssetCommandHelper documentAssetCommandHelper,
            ITransactionScopeFactory transactionScopeFactory
            )
        {
            _dbContext = dbContext;
            _entityAuditHelper = entityAuditHelper;
            _entityTagHelper = entityTagHelper;
            _documentAssetCommandHelper = documentAssetCommandHelper;
            _transactionScopeFactory = transactionScopeFactory;
        }

        #endregion

        #region Execute

        public async Task ExecuteAsync(UpdateDocumentAssetCommand command, IExecutionContext executionContext)
        {
            bool hasNewFile = command.File != null;

            var documentAsset = await _dbContext
                .DocumentAssets
                .Include(a => a.DocumentAssetTags)
                .Include(a => a.DocumentAssetTags.Select(pt => pt.Tag))
                .FilterById(command.DocumentAssetId)
                .SingleOrDefaultAsync();

            documentAsset.Title = command.Title;
            documentAsset.Description = command.Description ?? string.Empty;
            documentAsset.FileName = SlugFormatter.ToSlug(command.Title);
            _entityTagHelper.UpdateTags(documentAsset.DocumentAssetTags, command.Tags, executionContext);
            _entityAuditHelper.SetUpdated(documentAsset, executionContext);

            using (var scope = _transactionScopeFactory.Create())
            {
                if (hasNewFile)
                {
                    await _documentAssetCommandHelper.SaveFile(command.File, documentAsset);
                }

                await _dbContext.SaveChangesAsync();

                scope.Complete();
            }
        }

        #endregion

        #region Permission

        public IEnumerable<IPermissionApplication> GetPermissions(UpdateDocumentAssetCommand command)
        {
            yield return new DocumentAssetUpdatePermission();
        }

        #endregion
    }
}

﻿using AutoMapper;
using Conditions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Cofoundry.Domain.CQS;

namespace Cofoundry.Domain
{
    public class GetCustomEntityDefinitionMicroSummaryByDisplayModelTypeQueryHandler 
        : IQueryHandler<GetCustomEntityDefinitionMicroSummaryByDisplayModelTypeQuery, CustomEntityDefinitionMicroSummary>
        , IIgnorePermissionCheckHandler
    {
        #region constructor

        private readonly IQueryExecutor _queryExecutor;

        public GetCustomEntityDefinitionMicroSummaryByDisplayModelTypeQueryHandler(
            IQueryExecutor queryExecutor
            )
        {
            _queryExecutor = queryExecutor;
        }

        #endregion

        public CustomEntityDefinitionMicroSummary Execute(GetCustomEntityDefinitionMicroSummaryByDisplayModelTypeQuery query, IExecutionContext executionContext)
        {
            var dataModelType = query.DisplayModelType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICustomEntityDisplayModel<>))
                .Select(i => i.GetGenericArguments().Single())
                .SingleOrDefault();

            if (dataModelType == null)
            {
                throw new ArgumentException("query.DisplayModelType is not of type ICustomEntityDisplayModel<>");
            }

            var definition = _queryExecutor
                .GetAll<CustomEntityDefinitionSummary>(executionContext)
                .FirstOrDefault(d => d.DataModelType == dataModelType);

            var microSummary = Mapper.Map<CustomEntityDefinitionMicroSummary>(definition);

            return microSummary;
        }
    }
}

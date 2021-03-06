﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Cofoundry.Domain.Data;

namespace Cofoundry.Domain
{
    public class DocumentAssetAutoMapProfile : Profile
    {
        public DocumentAssetAutoMapProfile()
        {
            CreateMap<DocumentAsset, DocumentAssetRenderDetails>();

            CreateMap<DocumentAsset, DocumentAssetSummary>()
                .ForMember(d => d.AuditData, o => o.MapFrom(s => s))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s
                    .DocumentAssetTags
                    .Select(t => t.Tag.TagText)
                    .OrderBy(t => t))
                    )
                ;

            CreateMap<DocumentAsset, DocumentAssetDetails>()
                .ForMember(d => d.AuditData, o => o.MapFrom(s => s))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s
                    .DocumentAssetTags
                    .Select(t => t.Tag.TagText)
                    .OrderBy(t => t))
                    )
                ;

            CreateMap<DocumentAsset, UpdateDocumentAssetCommand>()
                .ForMember(d => d.Tags, o => o.MapFrom(s => s
                    .DocumentAssetTags
                    .Select(t => t.Tag.TagText)
                    .OrderBy(t => t))
                    )
                ;
        }
    }
}

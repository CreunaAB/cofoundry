﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using Cofoundry.Domain;

namespace Cofoundry.Web
{
    /// <summary>
    /// Data model representing a list of text items 
    /// </summary>
    public class TextListDataModel : IPageModuleDataModel
    {
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required, Display(Name = "Text list")]
        //[Searchable]
        public string TextList { get; set; }

        [Display(Name = "Numbered?")]
        public bool IsNumbered { get; set; }
    }
}